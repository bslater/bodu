#!/usr/bin/env python3
"""Merges the per-framework DocFX metadata into one framework-aware API reference.

``docfx metadata`` runs once per documented framework (docs/obj/api/metadata/<tfm>), because feeding
both frameworks' assemblies to one pass would collide identical types. This script folds those sets
into the single set ``docfx build`` renders (docs/api), so a reader sees one page per API:

* **Identity.** An API is identified by its DocFX UID. Pages, items, the children and derived-type
  lists, references, the table of contents, and the UID manifest are unioned by UID.
* **Precedence.** The newest framework supplies an API's content wherever it has the API; an older
  framework supplies only the APIs the newer ones lack, which are kept, not discarded. An API one
  framework adds is placed after its predecessor in that framework's order, so the merged order is
  deterministic.
* **Applicability.** Every item gets ``frameworks``: the frameworks that have it, oldest first. The
  templates render it as the page's "Applies to" section and the per-member availability notes.
* **Package.** Every type gets ``package`` (id, version, whether it is on nuget.org) from the
  assembly it is compiled into, as MSBuild resolved it; members inherit their type's. Every namespace
  gets ``packages``: the distinct packages of the types it contains, which may be several.
* **Divergence.** An API present in more than one framework is compared across them. Its
  *declaration* (kind, C# syntax, parameter, return, and type-parameter types, base types, Bodu
  interfaces, attributes) and its *documentation* (its XML documentation comment as the compiler
  wrote it, before DocFX expands <inheritdoc />, whose framework text legitimately differs between
  .NET versions) must agree, or the difference must be listed, with a reason, in
  bld/docs-checks/framework-divergence-allowlist.txt. An unlisted difference, or a listed one that no
  longer occurs, fails the merge.

The report of what was merged, and of every divergence, is written to
docs/obj/api/divergence-report.md and, under GitHub Actions, to the job summary.
"""

from __future__ import annotations

import copy
import os
import re
import shutil
import sys
import xml.etree.ElementTree as ET
from collections import Counter, defaultdict
from dataclasses import dataclass, field
from typing import Any, Iterable

import docfx_yaml as dy
import docs_common as dc

TYPE_KINDS = frozenset({"Class", "Struct", "Interface", "Enum", "Delegate"})
ALLOWLIST = os.path.join(dc.DOCS_CHECKS, "framework-divergence-allowlist.txt")

# Declaration fields compared across frameworks, by the label a divergence and its allowlist entry use.
SIGNATURE_FIELDS = ("type", "syntax", "parameters", "return", "typeParameters", "inheritance", "implements", "attributes")
DOCUMENTATION_FIELD = "documentation"


@dataclass(frozen=True, order=True)
class Divergence:
    """One way an API differs between the frameworks that have it."""

    uid: str
    field: str
    detail: str = field(compare=False)


@dataclass
class MergeResult:
    """The merged metadata, ready to write, and what the merge found."""

    pages: dict[str, dict[str, Any]]
    toc: dict[str, Any]
    manifest: dict[str, str]
    applicability: Counter[tuple[str, ...]]
    divergences: list[Divergence]


# ------------------------------------------------------------------------------------------- ordering


def union_order(sequences: Iterable[Iterable[str]]) -> list[str]:
    """Unions sequences of keys, keeping the first one's order.

    A key that only a later sequence has is inserted immediately after the key that precedes it in
    that sequence; a key with no predecessor there goes immediately before the key that follows it (or
    last, when nothing does). Each sequence's relative order therefore survives, and the result
    depends only on the inputs.
    """
    result: list[str] = []
    for sequence in sequences:
        keys = list(sequence)
        present = set(result)
        anchor: str | None = None
        for index, key in enumerate(keys):
            if key in present:
                anchor = key
                continue
            if anchor is not None:
                position = result.index(anchor) + 1
            else:
                successor = next((k for k in keys[index + 1 :] if k in present), None)
                position = result.index(successor) if successor is not None else len(result)
            result.insert(position, key)
            present.add(key)
            anchor = key
    return result


def union_list(values: list[list[str] | None]) -> list[str] | None:
    """Unions the per-framework versions of one UID list; ``None`` when no framework has it."""
    present = [v for v in values if v]
    return union_order(present) if present else None


# ------------------------------------------------------------------------------------ documentation


def normalized_xml(element: ET.Element) -> str:
    """Serializes the content of a documentation <member> with insignificant whitespace collapsed."""
    text = (element.text or "") + "".join(ET.tostring(child, encoding="unicode") for child in element)
    return re.sub(r"\s+", " ", text).strip()


def read_xml_documentation(path: str) -> dict[str, str]:
    """Reads a compiler-written documentation file as comment id → normalized content."""
    members: dict[str, str] = {}
    root = ET.parse(path).getroot()
    for member in root.iter("member"):
        name = member.get("name")
        if name:
            members[name] = normalized_xml(member)
    return members


# ----------------------------------------------------------------------------------------- divergence


def _types(entries: list[dict[str, Any]] | None, key: str = "type") -> list[Any]:
    return [entry.get(key) for entry in entries or []]


def _split_top_level(text: str, separator: str = ",") -> list[str]:
    """Splits ``text`` on ``separator`` where it is not nested inside <>, (), or []."""
    parts: list[str] = []
    depth = 0
    start = 0
    for index, char in enumerate(text):
        if char in "<([":
            depth += 1
        elif char in ">)]":
            depth -= 1
        elif char == separator and depth == 0:
            parts.append(text[start:index].strip())
            start = index + 1
    parts.append(text[start:].strip())
    return parts


_DECLARATION = re.compile(r"^(?P<head>[^:]*?\b(?:class|struct|interface|record(?: class| struct)?)\b[^:]*?) : (?P<bases>.*?)(?P<constraints> where .*)?$", re.S)


def normalized_declaration(content: str | None) -> str | None:
    """Returns a type declaration with its base list in a canonical order.

    DocFX lists every interface a type implements, including those it inherits through other
    interfaces, in the order the compiler enumerates them. That order changes when a framework
    interface gains a base between .NET versions (.NET 10's INumberBase<T> brings in the UTF-8
    interfaces earlier than .NET 8's), without the type implementing anything different. Comparing
    the base list as a set keeps that from reading as a changed declaration.
    """
    if not content:
        return content
    match = _DECLARATION.match(content)
    if not match:
        return content
    bases = sorted(_split_top_level(match.group("bases")))
    return f"{match.group('head')} : {', '.join(bases)}{match.group('constraints') or ''}"


def signature_of(item: dict[str, Any]) -> dict[str, Any]:
    """Extracts the declaration fields compared across frameworks."""
    syntax = item.get("syntax") or {}
    return {
        "type": item.get("type"),
        "syntax": normalized_declaration(syntax.get("content")) if item.get("type") in TYPE_KINDS else syntax.get("content"),
        "parameters": _types(syntax.get("parameters")),
        "return": (syntax.get("return") or {}).get("type"),
        "typeParameters": _types(syntax.get("typeParameters"), "id"),
        "inheritance": item.get("inheritance") or [],
        # Interfaces inherited from framework base types legitimately differ between .NET versions, so
        # only the Bodu interfaces a type implements are part of its declaration here.
        "implements": sorted(uid for uid in item.get("implements") or [] if uid.startswith("Bodu.")),
        "attributes": [
            {"type": a.get("type"), "ctor": a.get("ctor"), "arguments": a.get("arguments"), "namedArguments": a.get("namedArguments")}
            for a in item.get("attributes") or []
        ],
    }


def compare_item(uid: str, versions: dict[str, dict[str, Any]], documentation: dict[str, dict[str, str]]) -> list[Divergence]:
    """Compares one API across the frameworks in ``versions`` (framework → item)."""
    tfms = list(versions)
    divergences: list[Divergence] = []

    signatures = {tfm: signature_of(item) for tfm, item in versions.items()}
    for name in SIGNATURE_FIELDS:
        values = {tfm: signatures[tfm][name] for tfm in tfms}
        if len({repr(v) for v in values.values()}) > 1:
            detail = "; ".join(f"{tfm}: {values[tfm]!r:.300}" for tfm in tfms)
            divergences.append(Divergence(uid, name, detail))

    comment_id = next(iter(versions.values())).get("commentId")
    assembly = (next(iter(versions.values())).get("assemblies") or [None])[0]
    if comment_id and assembly:
        texts = {tfm: documentation.get(f"{tfm}/{assembly}", {}).get(comment_id) for tfm in tfms}
        if len(set(texts.values())) > 1:
            detail = "; ".join(f"{tfm}: {texts[tfm]!r:.160}" for tfm in tfms)
            divergences.append(Divergence(uid, DOCUMENTATION_FIELD, detail))

    return divergences


def read_allowlist(path: str = ALLOWLIST) -> dict[tuple[str, str], str]:
    """Reads the divergence allowlist as (uid, field) → reason; every entry must give a reason."""
    entries: dict[tuple[str, str], str] = {}
    if not os.path.exists(path):
        return entries
    with open(path, encoding="utf-8") as stream:
        for number, line in enumerate(stream, 1):
            body, hash_, reason = line.partition("#")
            parts = body.split()
            if not parts:
                continue
            if len(parts) != 2 or not hash_ or not reason.strip():
                raise dc.PipelineError(f"{os.path.relpath(path, dc.ROOT)}:{number}: expected '<uid> <field>  # reason'")
            if parts[1] not in SIGNATURE_FIELDS + (DOCUMENTATION_FIELD,):
                raise dc.PipelineError(f"{os.path.relpath(path, dc.ROOT)}:{number}: '{parts[1]}' is not a compared field ({', '.join(SIGNATURE_FIELDS + (DOCUMENTATION_FIELD,))})")
            entries[(parts[0], parts[1])] = reason.strip()
    return entries


# ---------------------------------------------------------------------------------------------- merge


def merge_items(versions: dict[str, dict[str, Any]], newest_first: list[str], oldest_first: list[str]) -> tuple[dict[str, Any], list[str]]:
    """Merges one UID's per-framework items: the newest supplies content, the lists are unioned."""
    order = [tfm for tfm in newest_first if tfm in versions]
    item = copy.deepcopy(versions[order[0]])
    for key in ("children", "derivedClasses", "extensionMethods"):
        merged = union_list([versions[tfm].get(key) for tfm in order])
        if merged is not None:
            item[key] = merged
    frameworks = [tfm for tfm in oldest_first if tfm in versions]
    item["frameworks"] = frameworks
    return item, frameworks


def merge_pages(docs: dict[str, dict[str, Any]], newest_first: list[str], oldest_first: list[str]) -> tuple[dict[str, Any], dict[str, dict[str, dict[str, Any]]]]:
    """Merges one page's per-framework documents; also returns each UID's per-framework items."""
    order = [tfm for tfm in newest_first if tfm in docs]
    per_uid: dict[str, dict[str, dict[str, Any]]] = defaultdict(dict)
    for tfm in order:
        for item in docs[tfm].get("items") or []:
            per_uid[item["uid"]][tfm] = item

    uids = union_order([[item["uid"] for item in docs[tfm].get("items") or []] for tfm in order])
    items = [merge_items(per_uid[uid], newest_first, oldest_first)[0] for uid in uids]

    references: dict[str, dict[str, Any]] = {}
    for tfm in order:
        for reference in docs[tfm].get("references") or []:
            references.setdefault(reference["uid"], reference)
    reference_order = union_order([[r["uid"] for r in docs[tfm].get("references") or []] for tfm in order])

    page = copy.deepcopy({k: v for k, v in docs[order[0]].items() if k not in ("items", "references")})
    page["items"] = items
    if reference_order:
        page["references"] = [copy.deepcopy(references[uid]) for uid in reference_order]
    return page, per_uid


def merge_toc(tocs: list[list[dict[str, Any]]]) -> list[dict[str, Any]]:
    """Unions nested TOC item lists by UID; the first (newest) list supplies each node's fields."""
    nodes: dict[str, list[dict[str, Any]]] = defaultdict(list)
    for toc in tocs:
        for node in toc:
            nodes[node["uid"]].append(node)
    merged: list[dict[str, Any]] = []
    for uid in union_order([[node["uid"] for node in toc] for toc in tocs]):
        versions = nodes[uid]
        node = {k: copy.deepcopy(v) for k, v in versions[0].items() if k != "items"}
        children = [v["items"] for v in versions if v.get("items")]
        if children:
            node["items"] = merge_toc(children)
        merged.append(node)
    return merged


def package_of(item: dict[str, Any], packages: dict[str, dict[str, Any]]) -> dict[str, Any]:
    """Returns the package the item's assembly ships in."""
    assemblies = item.get("assemblies") or []
    found = {packages[a]["id"]: packages[a] for a in assemblies if a in packages}
    if len(found) != 1:
        raise dc.PipelineError(f"{item['uid']}: assemblies {assemblies} resolve to {len(found)} documented packages; expected exactly one")
    return dict(next(iter(found.values())))


def merge(
    frameworks: list[str],
    pages: dict[str, dict[str, dict[str, Any]]],
    tocs: dict[str, dict[str, Any]],
    manifests: dict[str, dict[str, str]],
    packages: dict[str, dict[str, Any]],
    documentation: dict[str, dict[str, str]],
) -> MergeResult:
    """Merges per-framework metadata.

    ``frameworks`` is newest first. ``pages`` maps a framework to its pages (file name → document);
    ``tocs`` and ``manifests`` map it to its table of contents and UID manifest; ``packages`` maps an
    assembly name to its package; ``documentation`` maps "<tfm>/<assembly>" to its XML documentation.
    """
    newest_first = list(frameworks)
    oldest_first = list(reversed(frameworks))

    merged_pages: dict[str, dict[str, Any]] = {}
    applicability: Counter[tuple[str, ...]] = Counter()
    divergences: list[Divergence] = []
    item_frameworks: dict[str, list[str]] = {}
    type_packages: dict[str, dict[str, Any]] = {}

    for name in sorted(set().union(*(pages[tfm] for tfm in frameworks))):
        docs = {tfm: pages[tfm][name] for tfm in frameworks if name in pages[tfm]}
        page, per_uid = merge_pages(docs, newest_first, oldest_first)
        for item in page["items"]:
            uid = item["uid"]
            item_frameworks[uid] = item["frameworks"]
            applicability[tuple(item["frameworks"])] += 1
            if item.get("type") in TYPE_KINDS:
                item["package"] = package_of(item, packages)
                type_packages[uid] = item["package"]
            if len(per_uid[uid]) > 1:
                divergences += compare_item(uid, {tfm: per_uid[uid][tfm] for tfm in oldest_first if tfm in per_uid[uid]}, documentation)
        merged_pages[name] = page

    # Second pass: namespaces list the packages of the types they contain, which live on other pages,
    # and references to documented APIs carry their applicability so a page listing them can show it.
    for page in merged_pages.values():
        for item in page["items"]:
            if item.get("type") == "Namespace":
                distinct = {(p["id"], p["version"]): p for uid in item.get("children") or [] if (p := type_packages.get(uid))}
                item["packages"] = [dict(distinct[key]) for key in sorted(distinct)]
        for reference in page.get("references") or []:
            if reference["uid"] in item_frameworks:
                reference["frameworks"] = list(item_frameworks[reference["uid"]])

    toc_versions = [tocs[tfm] for tfm in newest_first if tfm in tocs]
    toc = {k: v for k, v in toc_versions[0].items() if k != "items"} if toc_versions else {}
    toc["items"] = merge_toc([t.get("items") or [] for t in toc_versions])

    manifest: dict[str, str] = {}
    for tfm in newest_first:
        for uid, file in (manifests.get(tfm) or {}).items():
            manifest.setdefault(uid, file)

    return MergeResult(merged_pages, toc, dict(sorted(manifest.items())), applicability, sorted(divergences))


# --------------------------------------------------------------------------------------------- report


def evaluate_divergences(divergences: list[Divergence], allowlist: dict[tuple[str, str], str]) -> tuple[list[Divergence], list[Divergence], list[tuple[str, str]]]:
    """Splits divergences into (unexpected, allowed) and returns the allowlist entries that no longer occur."""
    seen = {(d.uid, d.field) for d in divergences}
    unexpected = [d for d in divergences if (d.uid, d.field) not in allowlist]
    allowed = [d for d in divergences if (d.uid, d.field) in allowlist]
    stale = sorted(key for key in allowlist if key not in seen)
    return unexpected, allowed, stale


def report(result: MergeResult, frameworks: list[dc.Framework], unexpected: list[Divergence], allowed: list[Divergence], stale: list[tuple[str, str]], allowlist: dict[tuple[str, str], str]) -> str:
    """Renders the merge report as Markdown."""
    labels = {fw.tfm: fw.label for fw in frameworks}
    lines = ["## API framework applicability", "", "| Applies to | APIs |", "| --- | ---: |"]
    for key, count in sorted(result.applicability.items(), key=lambda kv: (-len(kv[0]), kv[0])):
        lines.append(f"| {', '.join(labels[t] for t in key)} | {count} |")
    lines += ["", f"Pages: {len(result.pages)}. Signature divergences: {sum(d.field != DOCUMENTATION_FIELD for d in unexpected + allowed)}. "
              f"Documentation divergences: {sum(d.field == DOCUMENTATION_FIELD for d in unexpected + allowed)}.", ""]

    only = {key: count for key, count in result.applicability.items() if len(key) < len(frameworks)}
    if only:
        lines += ["### APIs not in every framework", ""]
        for page in result.pages.values():
            for item in page["items"]:
                if len(item["frameworks"]) < len(frameworks):
                    lines.append(f"- `{item['uid']}`: {', '.join(labels[t] for t in item['frameworks'])} only")
        lines.append("")

    if unexpected:
        lines += ["### Unexpected divergences (fail)", "", "Add each to bld/docs-checks/framework-divergence-allowlist.txt with a reason once it is understood, or remove it.", ""]
        lines += [f"- `{d.uid}` **{d.field}**: {d.detail}" for d in unexpected]
        lines.append("")
    if allowed:
        lines += ["### Allowed divergences", ""]
        lines += [f"- `{d.uid}` **{d.field}**: {allowlist[(d.uid, d.field)]}" for d in allowed]
        lines.append("")
    if stale:
        lines += ["### Stale allowlist entries (fail)", ""]
        lines += [f"- `{uid}` {name}: no longer diverges; remove the entry" for uid, name in stale]
        lines.append("")
    return "\n".join(lines)


# ------------------------------------------------------------------------------------------------ I/O


def load_framework(tfm: str) -> tuple[dict[str, dict[str, Any]], dict[str, Any], dict[str, str]]:
    """Loads one framework's pages, table of contents, and UID manifest."""
    folder = dc.metadata_dir(tfm)
    if not os.path.isdir(folder):
        raise dc.PipelineError(f"{os.path.relpath(folder, dc.ROOT)} does not exist; run the `metadata` stage of bld/docs/build-api-docs.sh first")
    pages: dict[str, dict[str, Any]] = {}
    toc: dict[str, Any] = {}
    for name in sorted(os.listdir(folder)):
        path = os.path.join(folder, name)
        if name == "toc.yml":
            toc = dy.load_file(path)
        elif name.endswith(".yml"):
            header = dy.read_header(path)
            if header != dy.MANAGED_REFERENCE_HEADER:
                raise dc.PipelineError(f"{os.path.relpath(path, dc.ROOT)} is not ManagedReference YAML ({header!r})")
            pages[name] = dy.load_file(path)
    manifest_path = os.path.join(folder, ".manifest")
    manifest = dc.read_json(manifest_path) if os.path.exists(manifest_path) else {}
    return pages, toc, manifest


def load_documentation(tfms: list[str], assemblies: Iterable[str]) -> dict[str, dict[str, str]]:
    """Loads the staged XML documentation of every documented assembly for every framework."""
    documentation: dict[str, dict[str, str]] = {}
    for tfm in tfms:
        for assembly in assemblies:
            path = os.path.join(dc.input_dir(tfm), f"{assembly}.xml")
            if not os.path.exists(path):
                raise dc.PipelineError(f"{os.path.relpath(path, dc.ROOT)} does not exist; run the `assemblies` stage of bld/docs/build-api-docs.sh first")
            documentation[f"{tfm}/{assembly}"] = read_xml_documentation(path)
    return documentation


def write(result: MergeResult, output: str) -> None:
    """Replaces ``output`` with the merged metadata."""
    shutil.rmtree(output, ignore_errors=True)
    os.makedirs(output)
    for name, page in result.pages.items():
        dy.dump_file(os.path.join(output, name), page, dy.MANAGED_REFERENCE_HEADER)
    dy.dump_file(os.path.join(output, "toc.yml"), result.toc, dy.TOC_HEADER)
    dc.write_json(os.path.join(output, ".manifest"), result.manifest)


def main() -> int:
    build = dc.read_json(dc.BUILD_JSON)
    frameworks = dc.frameworks_newest_first(build["frameworks"])
    tfms = [fw.tfm for fw in frameworks]
    packages = dc.read_json(dc.PACKAGES_JSON)

    pages: dict[str, dict[str, dict[str, Any]]] = {}
    tocs: dict[str, dict[str, Any]] = {}
    manifests: dict[str, dict[str, str]] = {}
    for tfm in tfms:
        pages[tfm], tocs[tfm], manifests[tfm] = load_framework(tfm)
        print(f"{tfm}: read {len(pages[tfm])} pages")

    documentation = load_documentation(tfms, packages)
    result = merge(tfms, pages, tocs, manifests, packages, documentation)

    allowlist = read_allowlist()
    unexpected, allowed, stale = evaluate_divergences(result.divergences, allowlist)
    text = report(result, frameworks, unexpected, allowed, stale, allowlist)
    os.makedirs(dc.OBJ, exist_ok=True)
    with open(dc.DIVERGENCE_REPORT, "w", encoding="utf-8", newline="\n") as stream:
        stream.write(text + "\n")
    summary = os.environ.get("GITHUB_STEP_SUMMARY")
    if summary:
        with open(summary, "a", encoding="utf-8") as stream:
            stream.write(text + "\n")

    problems = [f"{d.uid} differs in {d.field} between frameworks: {d.detail}" for d in unexpected]
    problems += [f"bld/docs-checks/framework-divergence-allowlist.txt lists '{uid} {name}', which no longer diverges; remove it" for uid, name in stale]
    if problems:
        print(text)
        return dc.fail(problems, "Framework metadata merge")

    write(result, dc.API)
    counts = ", ".join(f"{' + '.join(key)}: {count}" for key, count in sorted(result.applicability.items(), key=lambda kv: (-len(kv[0]), kv[0])))
    print(f"Merged {len(result.pages)} pages into {os.path.relpath(dc.API, dc.ROOT)} ({counts}; {len(allowed)} allowed divergence(s)).")
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except dc.PipelineError as error:
        sys.exit(dc.fail(str(error).splitlines(), "merge_framework_metadata"))
