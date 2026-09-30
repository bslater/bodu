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
  links        every link and asset reference in the rendered site leads to a file the site has, and
               every #anchor to an element on that page, so a member deep link (an anchor on its
               type's page) cannot dangle; DocFX checks neither the links in an apidoc overwrite file
               nor the hrefs in XML documentation
  xmldoc       every href in an XML documentation comment anywhere in the codebase that leads into
               the site - relative, as the API pages render it, or an absolute URL of the published
               site - names a page and anchor the site has, including comments no page renders
  doclines     no line of documented XML documentation, outside a code block, begins with what
               Markdown reads as the start of a block (-, +, * or 1. and a space, a run of - or =,
               #, >): DocFX renders the documentation as Markdown, so the line would become a list,
               heading or quote; the XML-doc formatter never wraps one there, but an author can
  overlay      every template partial copied from DocFX names the DocFX version the pipeline pins,
               so a DocFX upgrade cannot silently run on partials forked from another version
"""

from __future__ import annotations

import glob
import html
import json
import os
import posixpath
import re
import sys
import urllib.parse
from collections import defaultdict
from typing import Any, Iterator

import docfx_yaml as dy
import docs_common as dc

TYPE_KINDS = frozenset({"Class", "Struct", "Interface", "Enum", "Delegate"})
SITE = os.path.join(dc.DOCS, "_site")
SITE_API = os.path.join(SITE, "api")
OVERLAY = os.path.join(dc.DOCS, "templates", "bodu")
GENERATED_SOURCE = re.compile(r"/obj/|\.Designer\.cs(?:$|#)|\.g\.cs(?:$|#)")

# A link or asset reference in a rendered page, and an anchor a link can land on.
LINK_ATTRIBUTE = re.compile(r'\s(?:href|src)="([^"]*)"')
ANCHOR_ATTRIBUTE = re.compile(r'\s(?:id|name)="([^"]*)"')
# A scheme (https:, mailto:, javascript:, ...) or a protocol-relative link leaves the site.
LEAVES_SITE = re.compile(r"^(?:[A-Za-z][A-Za-z0-9+.-]*:|//)")
# An XML documentation comment line, and an href within it.
XML_DOC_LINE = re.compile(r"^\s*///(.*)$")
XML_DOC_HREF = re.compile(r'\bhref="([^"]*)"')
# XML documentation renders on the API pages, so a relative href in it resolves from api/.
XML_DOC_BASE = "api/index.html"
# Directories a walk of the codebase never needs to enter: build output and tooling state.
UNSCANNED_DIRECTORIES = frozenset({".git", ".vs", "bin", "obj", "node_modules", "_site"})
# Folders of a documented package that hold no documented source.
UNDOCUMENTED_DIRECTORIES = UNSCANNED_DIRECTORIES | {"test", "tests", "bench", "benchmarks", "samples"}
# Text that Markdown reads as the start of a block when it begins a line: a bullet, a setext underline or thematic
# break, an ATX heading, a block quote, or an ordered list starting at one (the only kind that can interrupt a
# paragraph). It mirrors DocWrapper.IsMarkdownBlockMarker in Bodu.CodeStyle.
MARKDOWN_BLOCK_START = re.compile(r"^(?:[-+*](?:\s|$)|[-=*]+\s*$|#{1,6}(?:\s|$)|>|&gt;|1[.)](?:\s|$))")
# The start of a <code> block or CDATA section that does not end on the same line, and the end of either.
CODE_BLOCK_OPEN = re.compile(r"<code\b[^>]*>(?!.*</code>)|<!\[CDATA\[(?!.*\]\]>)")
CODE_BLOCK_CLOSE = re.compile(r"</code>|\]\]>")
# A tag closing a line, and the tags that sit inside prose rather than bounding a block of it.
TRAILING_TAG = re.compile(r"</?([A-Za-z]+)\b[^<>]*>$")
INLINE_XML_TAGS = frozenset({"see", "paramref", "typeparamref", "c", "a"})
# Beyond this many broken links the rest are counted rather than listed.
LINK_REPORT_LIMIT = 50


class RenderedSite:
    """The files of a built site and, read when first asked for, the anchors each page defines."""

    def __init__(self, root: str) -> None:
        self.root = root
        self.files: set[str] = set()
        for directory, _, names in os.walk(root):
            for name in names:
                self.files.add(os.path.relpath(os.path.join(directory, name), root).replace(os.sep, "/"))
        self._anchors: dict[str, frozenset[str]] = {}

    def pages(self) -> list[str]:
        return sorted(path for path in self.files if path.endswith(".html"))

    def read(self, page: str) -> str:
        with open(os.path.join(self.root, page), encoding="utf-8", errors="replace") as stream:
            return stream.read()

    def anchors(self, page: str) -> frozenset[str]:
        if page not in self._anchors:
            self._anchors[page] = frozenset(html.unescape(anchor) for anchor in ANCHOR_ATTRIBUTE.findall(self.read(page)))
        return self._anchors[page]

    def problem(self, target: str, fragment: str) -> str | None:
        """Returns why a link to ``target`` (a site-relative path) and ``fragment`` does not resolve, or None."""
        if target.startswith("/"):
            return "root-absolute, but the site is served under a path prefix (/bodu/, /bodu/dev/, ...)"
        if target == ".." or target.startswith("../"):
            return "leads outside the site"
        page = next((c for c in (target, posixpath.normpath(posixpath.join(target, "index.html"))) if c in self.files), None)
        if page is None:
            return "no such file in the site"
        if fragment and page.endswith(".html") and fragment not in self.anchors(page):
            return f"{page} has no anchor #{fragment}"
        return None


def resolve_link(page: str, href: str) -> tuple[str, str] | None:
    """Resolves ``href``, found on ``page`` (a site-relative path), to a site-relative target and fragment.

    Returns None for a link that leaves the site. A root-absolute path is returned as it is, for
    RenderedSite.problem to report. The query (the framework selector's ?view=) is dropped.
    """
    href = html.unescape(href).strip()
    if not href or LEAVES_SITE.match(href):
        return None
    path, _, fragment = href.partition("#")
    path = urllib.parse.unquote(path.split("?", 1)[0])
    fragment = urllib.parse.unquote(fragment)
    if path.startswith("/"):
        return path, fragment
    target = page if not path else posixpath.normpath(posixpath.join(posixpath.dirname(page), path))
    return target, fragment


def published_site_url() -> str:
    """The URL GitHub Pages serves the site from, derived from the repository."""
    owner, _, name = os.environ.get("GITHUB_REPOSITORY", "bslater/bodu").partition("/")
    return f"https://{owner.lower()}.github.io/{name}/"


def xml_doc_target(href: str, site_url: str) -> tuple[str, str] | None:
    """Resolves an href from XML documentation to a target in the site, or None when it leads elsewhere.

    A relative href resolves from the API pages, where the documentation renders. An absolute URL of
    the published site resolves within it, less the version slot (dev/, 1.0/, ...) it names, since
    every slot is a build of the same site.
    """
    href = html.unescape(href).strip()
    if href.lower().startswith(site_url.lower()):
        rest = re.sub(r"^(?:dev|[0-9]+\.[0-9]+)/", "", href[len(site_url):])
        return resolve_link("index.html", rest or "index.html")
    return resolve_link(XML_DOC_BASE, href)


def rendered_link_problems(rendered: RenderedSite) -> list[str]:
    problems = []
    for page in rendered.pages():
        for href in LINK_ATTRIBUTE.findall(rendered.read(page)):
            resolved = resolve_link(page, href)
            reason = resolved and rendered.problem(*resolved)
            if reason:
                problems.append(f"{page}: {href} ({reason})")
    return problems


def source_files(root: str, extension: str, skipped: frozenset[str] = UNSCANNED_DIRECTORIES) -> Iterator[str]:
    """Yields every file under ``root`` with ``extension``, in a stable order, skipping the ``skipped`` folders."""
    for directory, directories, names in os.walk(root):
        directories[:] = sorted(d for d in directories if d not in skipped)
        for name in sorted(names):
            if name.endswith(extension):
                yield os.path.join(directory, name)


def xml_doc_link_problems(rendered: RenderedSite, source_root: str, site_url: str) -> list[str]:
    problems = []
    for path in source_files(source_root, ".cs"):
        with open(path, encoding="utf-8", errors="replace") as stream:
            for number, line in enumerate(stream, 1):
                comment = XML_DOC_LINE.match(line)
                for href in XML_DOC_HREF.findall(comment.group(1)) if comment else ():
                    resolved = xml_doc_target(href, site_url)
                    reason = resolved and rendered.problem(*resolved)
                    if reason:
                        problems.append(f"{os.path.relpath(path, source_root)}:{number}: {href} ({reason})")
    return problems


def documented_source_roots() -> list[str]:
    """The package folder of every documented project: its src, and any shared source compiled into it."""
    return sorted({os.path.dirname(os.path.dirname(entry["project"])) for entry in dc.read_json(dc.PROJECTS_JSON)})


def continues_prose(previous: str) -> bool:
    """Whether a documentation line following ``previous`` continues its paragraph, rather than starting a block.

    A line after a blank line, or after a tag that bounds a block (``<para>``, ``</summary>``, ``<item>``, ...),
    starts a block, so a marker there is the author's; after prose or an inline tag it continues the paragraph.
    """
    if not previous:
        return False
    tag = TRAILING_TAG.search(previous)
    return tag is None or tag.group(1) in INLINE_XML_TAGS


def xml_doc_line_start_problems(roots: list[str], relative_to: str) -> list[str]:
    """Reports every XML documentation line under ``roots`` that continues a paragraph with a Markdown block marker.

    Code blocks and CDATA are skipped. A marker that opens a block (after ``<para>`` or a blank line) is the
    author's and is not reported; one that continues a paragraph turns the rest of it into a list, heading or quote.
    """
    problems = []
    for root in roots:
        for path in source_files(root, ".cs", UNDOCUMENTED_DIRECTORIES):
            in_code = False
            previous = ""
            with open(path, encoding="utf-8", errors="replace") as stream:
                for number, line in enumerate(stream, 1):
                    comment = XML_DOC_LINE.match(line)
                    if not comment:
                        in_code, previous = False, ""
                        continue
                    text = comment.group(1).strip()
                    if in_code:
                        in_code = not CODE_BLOCK_CLOSE.search(text)
                        previous = ""
                        continue
                    if continues_prose(previous) and MARKDOWN_BLOCK_START.match(text):
                        problems.append(
                            f"{os.path.relpath(path, relative_to)}:{number}: '{text}' continues a paragraph but opens a "
                            "Markdown list, heading or quote on the API site; keep the marker at the end of the previous line")
                    in_code = bool(CODE_BLOCK_OPEN.search(text))
                    previous = text
    return problems


def capped(problems: list[str], limit: int = LINK_REPORT_LIMIT) -> list[str]:
    if len(problems) <= limit:
        return problems
    return problems[:limit] + [f"... and {len(problems) - limit} more broken link(s)"]


class Site:
    """The merged metadata, the pipeline's resolved inputs, and the rendered site, loaded once."""

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
        self.rendered = RenderedSite(SITE)

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


def check_links(site: Site) -> list[str]:
    return capped(rendered_link_problems(site.rendered))


def check_xmldoc(site: Site) -> list[str]:
    return capped(xml_doc_link_problems(site.rendered, dc.ROOT, published_site_url()))


def check_doclines(site: Site) -> list[str]:
    return capped(xml_doc_line_start_problems(documented_source_roots(), dc.ROOT))


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
    "links": check_links,
    "xmldoc": check_xmldoc,
    "doclines": check_doclines,
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
