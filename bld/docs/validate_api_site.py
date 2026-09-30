#!/usr/bin/env python3
"""Checks the merged API metadata and the rendered site before the site is published.

Run after ``docfx build`` (bld/docs/build-api-docs.sh validate). Each check guards a promise the API
reference makes to its reader, and a broken promise fails the build rather than publishing:

  frameworks   every API applies to at least one documented framework and to no unknown one, and
               no API is documented twice
  navigation   every API page is reachable from the API table of contents, including an API that
               exists in only one framework
  packages     every type names exactly the package and version MSBuild resolved for its assembly,
               every documented package contributes types, and nothing outside the documented set
               (test infrastructure, excluded projects) appears
  pages        every rendered type page has an "Applies to" table that matches its frameworks and a
               Package fact with the exact id and version, linked to nuget.org only when published
  sources      every "View source" link names this repository at the documented commit, and none
               leads to generated code
  landing      every package's landing page (bld/docs-checks/package-docs-map.txt) cross-references
               the package's API
  overlay      every template partial copied from DocFX names the DocFX version the pipeline pins,
               so a DocFX upgrade cannot silently run on partials forked from another version
"""

from __future__ import annotations

import glob
import html
import json
import os
import re
import sys
from collections import defaultdict
from typing import Any

import docfx_yaml as dy
import docs_common as dc

TYPE_KINDS = frozenset({"Class", "Struct", "Interface", "Enum", "Delegate"})
SITE_API = os.path.join(dc.DOCS, "_site", "api")
OVERLAY = os.path.join(dc.DOCS, "templates", "bodu")
GENERATED_SOURCE = re.compile(r"/obj/|\.Designer\.cs(?:$|#)|\.g\.cs(?:$|#)")


class Site:
    """The merged metadata and the pipeline's resolved inputs, loaded once."""

    def __init__(self) -> None:
        self.packages: dict[str, dict[str, Any]] = dc.read_json(dc.PACKAGES_JSON)
        self.frameworks = [dc.parse_framework(f["tfm"]) for f in dc.read_json(dc.FRAMEWORKS_JSON)["_boduFrameworks"]]
        self.build: dict[str, Any] = dc.read_json(dc.BUILD_JSON)
        self.pages: dict[str, dict[str, Any]] = {}
        for path in sorted(glob.glob(os.path.join(dc.API, "*.yml"))):
            name = os.path.basename(path)
            if name != "toc.yml":
                self.pages[name] = dy.load_file(path)
        self.toc = dy.load_file(os.path.join(dc.API, "toc.yml"))

    def items(self) -> list[tuple[str, dict[str, Any]]]:
        return [(name, item) for name, page in self.pages.items() for item in page.get("items") or []]


def check_frameworks(site: Site) -> list[str]:
    known = {fw.tfm for fw in site.frameworks}
    problems: list[str] = []
    seen: dict[str, str] = {}
    for name, item in site.items():
        uid = item["uid"]
        if uid in seen:
            problems.append(f"{uid} is documented twice ({seen[uid]} and {name})")
        seen[uid] = name
        frameworks = item.get("frameworks")
        if not frameworks:
            problems.append(f"{uid} applies to no framework")
        elif set(frameworks) - known:
            problems.append(f"{uid} applies to unknown framework(s) {sorted(set(frameworks) - known)}")
    return problems


def toc_uids(nodes: list[dict[str, Any]]) -> set[str]:
    found: set[str] = set()
    for node in nodes:
        if "uid" in node:
            found.add(node["uid"])
        found |= toc_uids(node.get("items") or [])
    return found


def check_navigation(site: Site) -> list[str]:
    reachable = toc_uids(site.toc.get("items") or [])
    problems = []
    for name, page in site.pages.items():
        uid = page["items"][0]["uid"]
        if uid not in reachable:
            problems.append(f"{name} ({uid}) is not reachable from the API table of contents")
    return problems


def check_packages(site: Site) -> list[str]:
    problems: list[str] = []
    contributing: dict[str, int] = defaultdict(int)
    for _, item in site.items():
        for assembly in item.get("assemblies") or []:
            if assembly not in site.packages:
                problems.append(f"{item['uid']} comes from {assembly}, which is not a documented assembly")
        if item.get("type") not in TYPE_KINDS:
            continue
        package = item.get("package")
        expected = site.packages.get((item.get("assemblies") or [""])[0])
        if not package:
            problems.append(f"{item['uid']} names no package")
        elif package != expected:
            problems.append(f"{item['uid']} names package {package}, but its assembly resolves to {expected}")
        else:
            contributing[package["id"]] += 1
    for assembly, package in site.packages.items():
        if contributing[package["id"]] == 0:
            problems.append(f"package {package['id']} ({assembly}) contributes no documented type")
    return problems


def page_html(name: str) -> str | None:
    path = os.path.join(SITE_API, os.path.splitext(name)[0] + ".html")
    if not os.path.exists(path):
        return None
    with open(path, encoding="utf-8") as stream:
        return stream.read()


def expected_applies_to(frameworks: list[str], labels: dict[str, dc.Framework]) -> list[tuple[str, str]]:
    rows: dict[str, list[str]] = {}
    for tfm in frameworks:
        fw = labels[tfm]
        rows.setdefault(fw.product, []).append(fw.version)
    return [(product, ", ".join(versions)) for product, versions in rows.items()]


def check_pages(site: Site) -> list[str]:
    labels = {fw.tfm: fw for fw in site.frameworks}
    problems: list[str] = []
    for name, page in site.pages.items():
        item = page["items"][0]
        if item.get("type") not in TYPE_KINDS:
            continue
        uid = item["uid"]
        text = page_html(name)
        if text is None:
            problems.append(f"{uid}: no rendered page for {name}")
            continue

        table = re.search(r'<h2 id="appliesto".*?<tbody>(.*?)</tbody>', text, re.S)
        if not table:
            problems.append(f"{uid}: the rendered page has no Applies to section")
        else:
            rows = [tuple(html.unescape(cell).strip() for cell in row) for row in re.findall(r"<tr><td>(.*?)</td><td>(.*?)</td></tr>", table.group(1))]
            expected = expected_applies_to(item["frameworks"], labels)
            if rows != expected:
                problems.append(f"{uid}: Applies to shows {rows}, but the API applies to {expected}")

        package = item["package"]
        fact = re.search(r'<dl class="bodu-package"><dt>Package</dt><dd>(.*?)</dd></dl>', text, re.S)
        if not fact:
            problems.append(f"{uid}: the rendered page has no Package fact")
            continue
        body = fact.group(1)
        identity = re.search(r'class="bodu-package-id"(?: href="([^"]*)")?>([^<]*)<.*?class="bodu-package-version">([^<]*)<', body, re.S)
        if not identity or (html.unescape(identity.group(2)), html.unescape(identity.group(3))) != (package["id"], package["version"]):
            problems.append(f"{uid}: the Package fact does not read {package['id']} {package['version']}")
        elif bool(identity.group(1)) != bool(package["published"]):
            state = "published" if package["published"] else "not published"
            problems.append(f"{uid}: {package['id']} is {state} on nuget.org, but its Package fact {'lacks' if package['published'] else 'has'} a link")
    return problems


def check_sources(site: Site) -> list[str]:
    repository = os.environ.get("GITHUB_REPOSITORY", "bslater/bodu")
    prefix = f"https://github.com/{repository}/blob/{site.build['commit']}/"
    problems: list[str] = []
    sourced: dict[str, int] = defaultdict(int)
    for _, item in site.items():
        href = ((item.get("source") or {}).get("href")) or ""
        if not href:
            continue
        if not href.startswith(prefix):
            problems.append(f"{item['uid']}: source link {href} does not start with {prefix}")
        if GENERATED_SOURCE.search(href):
            problems.append(f"{item['uid']}: source link {href} leads to generated code")
        if item.get("type") in TYPE_KINDS:
            sourced[item["package"]["id"]] += 1
    for package in {p["id"] for p in site.packages.values()}:
        if sourced[package] == 0:
            problems.append(f"package {package} has no type with a source link; is its PDB missing Source Link?")
    return problems


def check_landing(site: Site) -> list[str]:
    by_package: dict[str, set[str]] = defaultdict(set)
    for _, item in site.items():
        if item.get("type") in TYPE_KINDS:
            by_package[item["package"]["id"]].add(item["uid"])
        elif item.get("type") == "Namespace":
            for package in item.get("packages") or []:
                by_package[package["id"]].add(item["uid"])

    landing_pages: dict[str, str] = {}
    with open(os.path.join(dc.DOCS_CHECKS, "package-docs-map.txt"), encoding="utf-8") as stream:
        for raw in stream:
            parts = raw.split("#", 1)[0].split()
            if len(parts) >= 2 and parts[1] != "-":
                landing_pages[parts[0]] = parts[1]

    problems: list[str] = []
    for package in sorted({p["id"] for p in site.packages.values()}):
        landing = landing_pages.get(package)
        if landing is None:
            continue  # bld/check-docs.py packages owns "every package has a landing page"
        candidates = [os.path.join(dc.DOCS, "docs", landing, "index.md"), os.path.join(dc.DOCS, "docs", f"{landing}.md")]
        path = next((c for c in candidates if os.path.exists(c)), None)
        if path is None:
            continue
        with open(path, encoding="utf-8") as stream:
            referenced = set(re.findall(r"xref:([A-Za-z0-9_.`{}#,()*@-]+?)(?:[?>)\]\s\"'])", stream.read()))
        if not referenced & by_package[package]:
            problems.append(f"{os.path.relpath(path, dc.ROOT)} is {package}'s landing page but cross-references none of its API")
    return problems


def check_overlay() -> list[str]:
    tools = dc.read_json(os.path.join(dc.DOCS, ".config", "dotnet-tools.json"))
    pinned = tools["tools"]["docfx"]["version"]
    problems = []
    for path in sorted(glob.glob(os.path.join(OVERLAY, "**", "*.tmpl*"), recursive=True)):
        with open(path, encoding="utf-8") as stream:
            match = re.search(r"Based on DocFX (\S+?) ", stream.readline())
        if match and match.group(1) != pinned:
            problems.append(f"{os.path.relpath(path, dc.ROOT)} is a copy of the DocFX {match.group(1)} partial, but DocFX {pinned} is pinned; re-copy it from {pinned} and re-apply the Bodu changes")
    return problems


CHECKS = {
    "frameworks": check_frameworks,
    "navigation": check_navigation,
    "packages": check_packages,
    "pages": check_pages,
    "sources": check_sources,
    "landing": check_landing,
}


def main() -> int:
    site = Site()
    problems: list[str] = []
    for name, check in CHECKS.items():
        found = check(site)
        print(f"{name}: {'ok' if not found else f'{len(found)} problem(s)'}")
        problems += found
    found = check_overlay()
    print(f"overlay: {'ok' if not found else f'{len(found)} problem(s)'}")
    problems += found
    if problems:
        return dc.fail(problems, "API site validation")
    types = sum(1 for _, item in site.items() if item.get("type") in TYPE_KINDS)
    print(f"Validated {len(site.pages)} API pages ({types} types, {len(site.packages)} packages).")
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except dc.PipelineError as error:
        sys.exit(dc.fail(str(error).splitlines(), "validate_api_site"))
