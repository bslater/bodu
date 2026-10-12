#!/usr/bin/env python3
"""Generate and validate the solution views (bodu.*.slnx) from bodu.slnx.

bodu.slnx is the master inventory of every project in the repository. The views at the
repository root select subsets of it - by purpose (libraries, tests, benchmarks, samples,
tooling) and by domain (core, security, globalization, financial, text, formats) - and this
script writes them from the classification in tools/solutions/solutions.json. A view is a
selection manifest and nothing else: a <Solution> of <Project Path="..." /> entries, sorted,
with no folders, configurations, build flags or solution items, so a project builds the same
way whichever solution it is built through.

Each view holds the projects its kinds and domains select plus every project they reference,
directly or transitively. Visual Studio builds only the projects in the opened solution, so a
view missing a referenced project would fail to build in the IDE.

Usage (from anywhere; paths are resolved from the repository root):

  python3 tools/solutions/generate-solutions.py           write the views
  python3 tools/solutions/generate-solutions.py --check   write nothing; exit 1 if a view is
                                                          stale or any rule below is broken
  python3 tools/solutions/generate-solutions.py --list    print each project's kind, domain
                                                          and views

Rules checked on every run (all problems are reported, then the exit code is 1):

  inventory    every project bodu.slnx lists exists, once; every *.csproj in the tree is in
               bodu.slnx or matches a notInMaster entry; no notInMaster entry is stale
  classify     every master project matches exactly one kind (an exception, else the first
               rule), and the kind agrees with the project file (tests set IsTestProject and
               are named *.Test.csproj; libraries are packable and are not tests); every
               package bld/release-manifest.txt publishes is a library
  domains      every project has one domain, from an exception or a name prefix, unless an
               exception sets it to null
  references   every ProjectReference resolves to a master project, or matches an
               externalReferences entry; references outside the master are not followed
  views        the libraries view holds only libraries; every master project is in at least
               one view; with --check, every committed view equals the generated one
  contract     every .slnx at the root other than the master is a declared view and holds
               only <Solution> and <Project Path="..." />
  legacy       the master's Debug build exclusions are exactly legacyMasterBuildExclusions
  policy       no MSBuild file reads $(SolutionDir), $(SolutionPath), $(SolutionName),
               $(SolutionFileName) or $(SolutionExt), and no Directory.Solution.props or
               Directory.Solution.targets exists

Exit codes: 0 clean, 1 a rule is broken or a view is stale, 2 the manifest cannot be read.
"""

from __future__ import annotations

import argparse
import json
import os
import re
import sys
import xml.etree.ElementTree as ET
from dataclasses import dataclass, field

MANIFEST_RELATIVE_PATH = "tools/solutions/solutions.json"
RELEASE_MANIFEST_RELATIVE_PATH = "bld/release-manifest.txt"
REGENERATE_COMMAND = "python3 tools/solutions/generate-solutions.py"

# Directories never scanned for project files: build output, VCS metadata and tool caches.
SKIPPED_DIRECTORIES = frozenset({".git", ".vs", "bin", "obj", "node_modules", "TestResults", "artifacts"})

# The project properties the classification reads.
READ_PROPERTIES = ("IsTestProject", "IsPackable", "OutputType", "PackAsTool")

SOLUTION_PROPERTY_PATTERN = re.compile(r"\$\((SolutionDir|SolutionPath|SolutionName|SolutionFileName|SolutionExt)\)")
VIEW_FILE_PATTERN = re.compile(r"^bodu\.[a-z][a-z0-9-]*\.slnx$")
MSBUILD_FILE_SUFFIXES = (".csproj", ".props", ".targets")


class ManifestError(Exception):
    """Raised when tools/solutions/solutions.json cannot be read or is malformed."""


@dataclass
class SolutionFile:
    """The parts of a .slnx file the checks read."""

    projects: list[str] = field(default_factory=list)
    debug_excluded: list[str] = field(default_factory=list)


@dataclass
class ProjectInfo:
    """A project file's classification inputs and its references."""

    path: str
    properties: dict[str, str] = field(default_factory=dict)
    references: list[str] = field(default_factory=list)
    unresolved: list[str] = field(default_factory=list)

    @property
    def name(self) -> str:
        return os.path.splitext(os.path.basename(self.path))[0]

    def flag(self, name: str) -> bool | None:
        value = self.properties.get(name)
        if value is None:
            return None
        return value.strip().lower() == "true"


@dataclass
class Classification:
    """A project's kind and domain, with the manifest entry each came from."""

    kind: str
    domain: str | None
    source: str


# ---------------------------------------------------------------------------------------------------------------
# Paths and globs
# ---------------------------------------------------------------------------------------------------------------


def normalize(path: str) -> str:
    """Returns a repository-relative path with forward slashes and no '.' or '..' segments."""
    return os.path.normpath(path.replace("\\", "/")).replace("\\", "/")


def sort_key(path: str) -> tuple[tuple[str, ...], str]:
    """Orders paths segment by segment, case-insensitively, ties broken ordinally, so a folder sorts
    before its dotted siblings (Bodu.Collections/ before Bodu.Collections.Concurrent/) and the output
    is the same on every platform."""
    return (tuple(segment.lower() for segment in path.split("/")), path)


def glob_to_regex(pattern: str) -> re.Pattern[str]:
    """Compiles a glob in which '**/' matches any number of directories, '*' stays inside one
    path segment, and '?' matches one character other than '/'."""
    out = []
    i = 0
    while i < len(pattern):
        if pattern.startswith("**/", i):
            out.append("(?:[^/]+/)*")
            i += 3
        elif pattern.startswith("**", i):
            out.append(".*")
            i += 2
        elif pattern[i] == "*":
            out.append("[^/]*")
            i += 1
        elif pattern[i] == "?":
            out.append("[^/]")
            i += 1
        else:
            out.append(re.escape(pattern[i]))
            i += 1
    return re.compile("^" + "".join(out) + "$")


def glob_match(pattern: str, path: str) -> bool:
    """Returns whether a repository-relative path matches a glob."""
    return glob_to_regex(pattern).match(path) is not None


# ---------------------------------------------------------------------------------------------------------------
# Reading
# ---------------------------------------------------------------------------------------------------------------


def local_name(tag: str) -> str:
    return tag.rsplit("}", 1)[-1]


def read_solution(text: str) -> SolutionFile:
    """Reads the project paths of a .slnx and the ones its Debug configuration excludes."""
    root = ET.fromstring(text)
    solution = SolutionFile()
    for element in root.iter():
        if local_name(element.tag) != "Project":
            continue
        path = element.get("Path")
        if path is None:
            continue
        path = normalize(path)
        solution.projects.append(path)
        for child in element:
            if local_name(child.tag) != "Build":
                continue
            if child.get("Project", "").lower() == "false" and child.get("Solution", "").startswith("Debug|"):
                solution.debug_excluded.append(path)
    return solution


def view_contract_problems(text: str) -> list[str]:
    """Returns the ways a view breaks the selection-only contract: a <Solution> root holding only
    <Project> elements with a Path attribute and no children, comments or other content."""
    try:
        root = ET.fromstring(text)
    except ET.ParseError as error:
        return [f"is not well-formed XML ({error})"]

    problems = []
    if local_name(root.tag) != "Solution":
        problems.append(f"has root <{local_name(root.tag)}>, not <Solution>")
    if root.attrib:
        problems.append("has attributes on <Solution>")
    if (root.text or "").strip():
        problems.append("has text inside <Solution>")
    for element in root:
        name = local_name(element.tag) if isinstance(element.tag, str) else "comment or processing instruction"
        if name != "Project":
            problems.append(f"has a <{name}> element; a view holds only <Project Path=\"...\" />")
            continue
        if set(element.attrib) != {"Path"}:
            extra = sorted(set(element.attrib) - {"Path"})
            problems.append(f"has a <Project> with attributes other than Path: {', '.join(extra) or 'no Path'}")
        if len(element) or (element.text or "").strip():
            problems.append(f"has content inside <Project Path=\"{element.get('Path')}\">")
        if (element.tail or "").strip():
            problems.append("has text between elements")
    if "<!--" in text:
        problems.append("has a comment; a view holds only <Project Path=\"...\" />")
    return problems


def read_project(repo_root: str, path: str) -> ProjectInfo:
    """Reads a project file's classification properties and resolves its ProjectReference items.

    An Include that names another item type, such as @(PluginFixture), is expanded from the items of
    that type declared in the same file. An Include that still holds an MSBuild property or item
    expression after that cannot be resolved without evaluation and is reported as unresolved."""
    info = ProjectInfo(path=path)
    root = ET.parse(os.path.join(repo_root, path)).getroot()
    directory = os.path.dirname(path)

    items: dict[str, list[str]] = {}
    for element in root.iter():
        if not isinstance(element.tag, str):
            continue
        name = local_name(element.tag)
        if name in READ_PROPERTIES and element.text is not None:
            info.properties[name] = element.text.strip()
        include = element.get("Include")
        if include is not None:
            items.setdefault(name, []).extend(part.strip() for part in include.split(";") if part.strip())

    for include in items.get("ProjectReference", []):
        expanded = [include]
        match = re.fullmatch(r"@\(([A-Za-z_][A-Za-z0-9_.-]*)\)", include)
        if match:
            expanded = items.get(match.group(1), [])
        for target in expanded:
            if "$(" in target or "@(" in target or "%(" in target:
                info.unresolved.append(target)
                continue
            info.references.append(normalize(os.path.join(directory, target)))
    return info


def discover_project_files(repo_root: str) -> list[str]:
    """Returns every *.csproj under the repository root, outside build output and tool caches."""
    found = []
    for directory, subdirectories, files in os.walk(repo_root):
        subdirectories[:] = sorted(d for d in subdirectories if d not in SKIPPED_DIRECTORIES)
        for file_name in files:
            if file_name.endswith(".csproj"):
                found.append(normalize(os.path.relpath(os.path.join(directory, file_name), repo_root)))
    return sorted(found, key=sort_key)


def discover_msbuild_files(repo_root: str) -> list[str]:
    """Returns every project, .props and .targets file under the repository root."""
    found = []
    for directory, subdirectories, files in os.walk(repo_root):
        subdirectories[:] = sorted(d for d in subdirectories if d not in SKIPPED_DIRECTORIES)
        for file_name in files:
            if file_name.endswith(MSBUILD_FILE_SUFFIXES):
                found.append(normalize(os.path.relpath(os.path.join(directory, file_name), repo_root)))
    return sorted(found, key=sort_key)


def read_release_packages(text: str) -> list[str]:
    """Returns the package ids bld/release-manifest.txt publishes, in file order."""
    packages = []
    for line in text.splitlines():
        line = line.strip()
        if not line or line.startswith("#"):
            continue
        packages.append(line.split()[0])
    return packages


def load_manifest(text: str) -> dict:
    """Parses solutions.json and checks the structure the rest of the script relies on."""
    try:
        manifest = json.loads(text)
    except json.JSONDecodeError as error:
        raise ManifestError(f"{MANIFEST_RELATIVE_PATH} is not valid JSON: {error}") from error

    for key in ("master", "kinds", "domains", "domainViewKinds", "views", "exceptions", "rules", "notInMaster", "legacyMasterBuildExclusions"):
        if key not in manifest:
            raise ManifestError(f"{MANIFEST_RELATIVE_PATH} has no '{key}'.")

    kinds = set(manifest["kinds"])
    domains = set(manifest["domains"])
    problems = []

    def check_kind(kind: object, where: str) -> None:
        if kind not in kinds:
            problems.append(f"{where} names kind '{kind}', which 'kinds' does not declare.")

    def check_domain(domain: object, where: str) -> None:
        if domain is not None and domain not in domains:
            problems.append(f"{where} names domain '{domain}', which 'domains' does not declare.")

    for kind in manifest["domainViewKinds"]:
        check_kind(kind, "domainViewKinds")
    for name, domain in manifest["domains"].items():
        if not domain.get("prefixes"):
            problems.append(f"domain '{name}' has no prefixes.")

    view_files = set()
    for view in manifest["views"]:
        file_name = view.get("file", "")
        if not VIEW_FILE_PATTERN.match(file_name):
            problems.append(f"view '{file_name}' is not named bodu.<name>.slnx.")
        if file_name in view_files or file_name == manifest["master"]:
            problems.append(f"view '{file_name}' is declared twice or names the master.")
        view_files.add(file_name)
        if not view.get("kinds") and not view.get("domains"):
            problems.append(f"view '{file_name}' selects neither kinds nor domains.")
        for kind in view.get("kinds", []):
            check_kind(kind, f"view '{file_name}'")
        for domain in view.get("domains", []):
            check_domain(domain, f"view '{file_name}'")

    for domain in sorted(domains):
        if not any(domain in view.get("domains", []) for view in manifest["views"]):
            problems.append(f"domain '{domain}' has no view.")

    for entry in manifest["exceptions"]:
        where = f"exception '{entry.get('match')}'"
        if not entry.get("match") or not entry.get("reason"):
            problems.append(f"{where} needs both 'match' and 'reason'.")
        check_kind(entry.get("kind"), where)
        if "domain" in entry:
            check_domain(entry["domain"], where)
    for entry in manifest["rules"]:
        if not entry.get("match"):
            problems.append("a rule has no 'match'.")
        check_kind(entry.get("kind"), f"rule '{entry.get('match')}'")
    for entry in manifest["notInMaster"]:
        if not entry.get("match") or not entry.get("reason"):
            problems.append(f"notInMaster entry '{entry.get('match')}' needs both 'match' and 'reason'.")
    for entry in manifest.get("externalReferences", []):
        if not entry.get("from") or not entry.get("to") or not entry.get("reason"):
            problems.append(f"externalReferences entry from '{entry.get('from')}' needs 'from', 'to' and 'reason'.")

    if problems:
        raise ManifestError("\n".join(f"{MANIFEST_RELATIVE_PATH}: {problem}" for problem in problems))
    return manifest


# ---------------------------------------------------------------------------------------------------------------
# Classification and selection
# ---------------------------------------------------------------------------------------------------------------


def domain_for_name(name: str, domains: dict) -> list[str]:
    """Returns the domains whose longest matching prefix claims a project name; more than one is ambiguous."""
    best_length = -1
    owners: list[str] = []
    for domain, definition in domains.items():
        for prefix in definition["prefixes"]:
            if name == prefix or name.startswith(prefix + "."):
                if len(prefix) > best_length:
                    best_length, owners = len(prefix), [domain]
                elif len(prefix) == best_length and domain not in owners:
                    owners.append(domain)
    return owners


def classify(project: ProjectInfo, manifest: dict) -> tuple[Classification | None, list[str]]:
    """Assigns a project its kind and domain. Exceptions are tried first and must not overlap; the
    first matching rule applies otherwise. Returns the classification, or None, with any problems."""
    problems = []
    exceptions = [entry for entry in manifest["exceptions"] if glob_match(entry["match"], project.path)]
    if len(exceptions) > 1:
        names = ", ".join(entry["match"] for entry in exceptions)
        return None, [f"{project.path}: matches more than one exception ({names})."]

    if exceptions:
        entry = exceptions[0]
        kind, source = entry["kind"], f"exception '{entry['match']}'"
    else:
        rule = next((entry for entry in manifest["rules"] if glob_match(entry["match"], project.path)), None)
        if rule is None:
            return None, [f"{project.path}: matches no rule or exception in {MANIFEST_RELATIVE_PATH}; classify it there."]
        entry = rule
        kind, source = rule["kind"], f"rule '{rule['match']}'"

    if "domain" in entry:
        domain = entry["domain"]
    else:
        owners = domain_for_name(project.name, manifest["domains"])
        if len(owners) != 1:
            reason = "no domain prefix matches it" if not owners else f"domains {', '.join(sorted(owners))} all claim it"
            problems.append(f"{project.path}: {reason}; give it a domain in {MANIFEST_RELATIVE_PATH}.")
            domain = None
        else:
            domain = owners[0]

    problems.extend(kind_problems(project, kind, source))
    return Classification(kind=kind, domain=domain, source=source), problems


def kind_problems(project: ProjectInfo, kind: str, source: str) -> list[str]:
    """Checks a kind against what the project file declares, so that a path rule cannot classify a
    project its own file contradicts."""
    problems = []
    is_test_file = project.path.endswith(".Test.csproj")
    if kind in ("test", "tooling-test", "sample-test"):
        if project.flag("IsTestProject") is not True:
            problems.append(f"{project.path}: classified '{kind}' by {source}, but does not set IsTestProject to true.")
        if not is_test_file:
            problems.append(f"{project.path}: classified '{kind}' by {source}, but is not named *.Test.csproj.")
    elif is_test_file:
        problems.append(f"{project.path}: is named *.Test.csproj, but {source} classifies it '{kind}', not a test kind.")
    if kind == "library":
        if project.flag("IsTestProject") is True:
            problems.append(f"{project.path}: classified 'library' by {source}, but sets IsTestProject to true.")
        if project.flag("IsPackable") is False:
            problems.append(f"{project.path}: classified 'library' by {source}, but sets IsPackable to false.")
        if project.properties.get("OutputType", "").lower() in ("exe", "winexe"):
            problems.append(f"{project.path}: classified 'library' by {source}, but is an executable.")
    return problems


def reference_closure(selected: set[str], projects: dict[str, ProjectInfo]) -> set[str]:
    """Returns the selected projects with every master project they reference, directly or
    transitively. A reference outside the master (an allowed external reference) is not followed."""
    members = set()
    pending = [path for path in selected if path in projects]
    while pending:
        path = pending.pop()
        if path in members:
            continue
        members.add(path)
        pending.extend(reference for reference in projects[path].references if reference in projects and reference not in members)
    return members


def select_view(view: dict, manifest: dict, classes: dict[str, Classification]) -> set[str]:
    """Returns the projects a view's kinds and domains select, before references are added."""
    domains = view.get("domains")
    kinds = set(view.get("kinds") or (manifest["domainViewKinds"] if domains else []))
    selected = set()
    for path, classification in classes.items():
        if classification.kind not in kinds:
            continue
        if domains and classification.domain not in domains:
            continue
        selected.add(path)
    return selected


def render_view(paths: set[str]) -> str:
    """Renders a view: one <Project Path="..." /> per project, sorted, LF line endings, no BOM."""
    lines = ["<Solution>"]
    lines.extend(f'  <Project Path="{path}" />' for path in sorted(paths, key=sort_key))
    lines.append("</Solution>")
    return "\n".join(lines) + "\n"


# ---------------------------------------------------------------------------------------------------------------
# The run
# ---------------------------------------------------------------------------------------------------------------


@dataclass
class Result:
    """Everything one run computes: the views to write and the problems found."""

    views: dict[str, str] = field(default_factory=dict)
    members: dict[str, set[str]] = field(default_factory=dict)
    classes: dict[str, Classification] = field(default_factory=dict)
    problems: list[str] = field(default_factory=list)


def read_text(repo_root: str, path: str) -> str:
    with open(os.path.join(repo_root, path), encoding="utf-8-sig") as stream:
        return stream.read()


def analyze(repo_root: str, manifest: dict) -> Result:
    """Reads the master and every project, classifies them, computes the views and checks every rule
    except the comparison with the committed views."""
    result = Result()
    problems = result.problems
    master_path = manifest["master"]

    master = read_solution(read_text(repo_root, master_path))
    master_set = set()
    for path in master.projects:
        if path in master_set:
            problems.append(f"{master_path}: lists {path} more than once.")
        master_set.add(path)
        if not os.path.isfile(os.path.join(repo_root, path)):
            problems.append(f"{master_path}: lists {path}, which does not exist.")

    # Inventory: every project file is in the master or deliberately left out of it.
    used_exclusions = set()
    for path in discover_project_files(repo_root):
        if path in master_set:
            continue
        matches = [entry["match"] for entry in manifest["notInMaster"] if glob_match(entry["match"], path)]
        if matches:
            used_exclusions.update(matches)
        else:
            problems.append(f"{path}: is not in {master_path}; add it there, or to notInMaster in {MANIFEST_RELATIVE_PATH} with the reason.")
    for entry in manifest["notInMaster"]:
        if entry["match"] in used_exclusions:
            continue
        if any(glob_match(entry["match"], path) for path in master_set):
            problems.append(f"{MANIFEST_RELATIVE_PATH}: notInMaster entry '{entry['match']}' matches a project {master_path} lists.")
        else:
            problems.append(f"{MANIFEST_RELATIVE_PATH}: notInMaster entry '{entry['match']}' matches no project file.")

    # The master's legacy Debug exclusions are recorded exactly, so none is added silently.
    recorded = set(normalize(path) for path in manifest["legacyMasterBuildExclusions"]["projects"])
    actual = set(master.debug_excluded)
    for path in sorted(actual - recorded, key=sort_key):
        problems.append(f"{master_path}: excludes {path} from its Debug configuration; the views never carry such an exclusion, and the master may not gain one.")
    for path in sorted(recorded - actual, key=sort_key):
        problems.append(f"{MANIFEST_RELATIVE_PATH}: legacyMasterBuildExclusions lists {path}, which {master_path} no longer excludes; remove it from the list.")

    # Read and classify every existing master project.
    projects: dict[str, ProjectInfo] = {}
    for path in sorted(master_set, key=sort_key):
        if not os.path.isfile(os.path.join(repo_root, path)):
            continue
        try:
            projects[path] = read_project(repo_root, path)
        except ET.ParseError as error:
            problems.append(f"{path}: is not well-formed XML ({error}).")

    used_exceptions = set()
    external = manifest.get("externalReferences", [])
    used_external = set()
    for path, project in projects.items():
        classification, found = classify(project, manifest)
        problems.extend(found)
        if classification is not None:
            result.classes[path] = classification
            if classification.source.startswith("exception"):
                used_exceptions.add(classification.source)
        for target in project.unresolved:
            problems.append(f"{path}: cannot resolve the ProjectReference '{target}' without evaluating MSBuild.")
        for reference in project.references:
            if reference in master_set:
                continue
            allowed = [index for index, entry in enumerate(external) if glob_match(entry["from"], path) and glob_match(entry["to"], reference)]
            if allowed:
                used_external.update(allowed)
            else:
                problems.append(f"{path}: references {reference}, which {master_path} does not list; add it there, or to externalReferences in {MANIFEST_RELATIVE_PATH} with the reason.")
    for entry in manifest["exceptions"]:
        if f"exception '{entry['match']}'" not in used_exceptions:
            problems.append(f"{MANIFEST_RELATIVE_PATH}: exception '{entry['match']}' matches no project in {master_path}.")
    for index, entry in enumerate(external):
        if index not in used_external:
            problems.append(f"{MANIFEST_RELATIVE_PATH}: externalReferences entry from '{entry['from']}' to '{entry['to']}' matches no reference.")

    # Every package the release manifest publishes is a library, and so reaches the libraries view.
    release_path = os.path.join(repo_root, RELEASE_MANIFEST_RELATIVE_PATH)
    if os.path.isfile(release_path):
        by_name: dict[str, list[str]] = {}
        for path in projects:
            by_name.setdefault(projects[path].name, []).append(path)
        for package in read_release_packages(read_text(repo_root, RELEASE_MANIFEST_RELATIVE_PATH)):
            paths = by_name.get(package, [])
            if len(paths) != 1:
                problems.append(f"{RELEASE_MANIFEST_RELATIVE_PATH}: publishes {package}, which matches {len(paths)} projects in {master_path}, not one.")
            elif paths[0] in result.classes and result.classes[paths[0]].kind != "library":
                problems.append(f"{paths[0]}: is published by {RELEASE_MANIFEST_RELATIVE_PATH} but classified '{result.classes[paths[0]].kind}', not 'library'.")

    # Views.
    in_any_view = set()
    for view in manifest["views"]:
        file_name = view["file"]
        selected = select_view(view, manifest, result.classes)
        if not selected:
            problems.append(f"{file_name}: selects no projects.")
        members = reference_closure(selected, projects)
        result.members[file_name] = members
        result.views[file_name] = render_view(members)
        in_any_view.update(members)
        if view.get("kinds") == ["library"]:
            for path in sorted(members, key=sort_key):
                if path in result.classes and result.classes[path].kind != "library":
                    problems.append(f"{file_name}: holds {path} ('{result.classes[path].kind}'), which a library references; the libraries view holds only libraries.")
    for path in sorted(master_set - in_any_view, key=sort_key):
        if path in projects:
            problems.append(f"{path}: is in no view; give its kind or domain a view in {MANIFEST_RELATIVE_PATH}.")

    # Every .slnx at the root is the master or a declared view.
    declared = {view["file"] for view in manifest["views"]}
    for file_name in sorted(os.listdir(repo_root)):
        if file_name.endswith(".slnx") and file_name != master_path and file_name not in declared:
            problems.append(f"{file_name}: is not a view {MANIFEST_RELATIVE_PATH} declares; declare it there or delete it.")

    # Build policy must not depend on the solution a project is built through.
    for path in discover_msbuild_files(repo_root):
        file_name = os.path.basename(path)
        if file_name in ("Directory.Solution.props", "Directory.Solution.targets"):
            problems.append(f"{path}: solution-level build customization is not allowed; put shared policy in Directory.Build.props/.targets or bld/.")
            continue
        try:
            text = read_text(repo_root, path)
        except (OSError, UnicodeDecodeError):
            continue
        for number, line in enumerate(text.splitlines(), start=1):
            match = SOLUTION_PROPERTY_PATTERN.search(line)
            if match:
                problems.append(f"{path}:{number}: reads $({match.group(1)}), so the project would build differently in different solutions.")

    return result


def check_committed(repo_root: str, result: Result) -> list[str]:
    """Compares the generated views with the committed ones and checks each against the contract."""
    problems = []
    for file_name, expected in result.views.items():
        full_path = os.path.join(repo_root, file_name)
        if not os.path.isfile(full_path):
            problems.append(f"{file_name}: is missing; run {REGENERATE_COMMAND}.")
            continue
        with open(full_path, "rb") as stream:
            raw = stream.read()
        try:
            actual = raw.decode("utf-8")
        except UnicodeDecodeError:
            problems.append(f"{file_name}: is not UTF-8; run {REGENERATE_COMMAND}.")
            continue
        for problem in view_contract_problems(actual):
            problems.append(f"{file_name}: {problem}.")
        if actual == expected:
            continue
        try:
            committed = set(read_solution(actual).projects)
        except ET.ParseError:
            committed = set()
        members = result.members[file_name]
        details = [f"missing {path}" for path in sorted(members - committed, key=sort_key)]
        details += [f"unexpected {path}" for path in sorted(committed - members, key=sort_key)]
        summary = "; ".join(details) if details else "same projects, different text (order, slashes, encoding or line endings)"
        problems.append(f"{file_name}: is out of date ({summary}); run {REGENERATE_COMMAND}.")
    return problems


def write_views(repo_root: str, result: Result) -> list[str]:
    """Writes every view whose text changed and returns the names written."""
    written = []
    for file_name, text in result.views.items():
        full_path = os.path.join(repo_root, file_name)
        if os.path.isfile(full_path):
            with open(full_path, "rb") as stream:
                if stream.read() == text.encode("utf-8"):
                    continue
        with open(full_path, "w", encoding="utf-8", newline="\n") as stream:
            stream.write(text)
        written.append(file_name)
    return written


def list_projects(result: Result, manifest: dict) -> str:
    """Formats each master project's kind, domain and views as a table."""
    rows = []
    for path in sorted(result.classes, key=sort_key):
        classification = result.classes[path]
        views = [view["file"][len("bodu."):-len(".slnx")] for view in manifest["views"] if path in result.members.get(view["file"], set())]
        rows.append((path, classification.kind, classification.domain or "-", ",".join(views)))
    width_path = max((len(row[0]) for row in rows), default=0)
    width_kind = max((len(row[1]) for row in rows), default=0)
    width_domain = max((len(row[2]) for row in rows), default=0)
    return "\n".join(f"{p:<{width_path}}  {k:<{width_kind}}  {d:<{width_domain}}  {v}" for p, k, d, v in rows)


def report(problems: list[str]) -> None:
    annotate = os.environ.get("GITHUB_ACTIONS") == "true"
    for problem in problems:
        if annotate:
            location, _, message = problem.partition(": ")
            file_part = location.split(":", 1)[0]
            print(f"::error file={file_part}::{message or problem}")
        print(f"error: {problem}", file=sys.stderr)


def default_repo_root() -> str:
    return os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Generate and validate the bodu.*.slnx solution views.")
    mode = parser.add_mutually_exclusive_group()
    mode.add_argument("--check", action="store_true", help="write nothing; fail if a view is stale or a rule is broken")
    mode.add_argument("--list", action="store_true", help="print each project's kind, domain and views")
    parser.add_argument("--repo-root", default=default_repo_root(), help=argparse.SUPPRESS)
    arguments = parser.parse_args(argv)
    repo_root = os.path.abspath(arguments.repo_root)

    try:
        manifest = load_manifest(read_text(repo_root, MANIFEST_RELATIVE_PATH))
        result = analyze(repo_root, manifest)
    except (ManifestError, OSError, ET.ParseError) as error:
        print(f"error: {error}", file=sys.stderr)
        return 2

    if arguments.list:
        print(list_projects(result, manifest))
        report(result.problems)
        return 1 if result.problems else 0

    if arguments.check:
        problems = result.problems + check_committed(repo_root, result)
        report(problems)
        if problems:
            print(f"{len(problems)} problem(s) in the solution views.", file=sys.stderr)
            return 1
        print(f"{len(result.views)} solution views are current; {len(result.classes)} projects classified.")
        return 0

    if result.problems:
        report(result.problems)
        print(f"{len(result.problems)} problem(s); no views written.", file=sys.stderr)
        return 1
    written = write_views(repo_root, result)
    for file_name in written:
        print(f"wrote {file_name}")
    print(f"{len(result.views)} solution views, {len(written)} changed; {len(result.classes)} projects classified.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
