"""Tests for tools/solutions/generate-solutions.py."""

from __future__ import annotations

import contextlib
import importlib.util
import io
import json
import os
import sys
import tempfile
import unittest

# The script's file name has a hyphen, so it is loaded by path. It is registered in sys.modules
# before it runs because its dataclasses look their module up there.
SCRIPT = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "generate-solutions.py")
_spec = importlib.util.spec_from_file_location("generate_solutions", SCRIPT)
gs = importlib.util.module_from_spec(_spec)
sys.modules[_spec.name] = gs
_spec.loader.exec_module(gs)


def project_xml(properties: dict[str, str] | None = None, references: list[str] | None = None, extra: str = "") -> str:
    """Returns a minimal SDK project file with the given properties and ProjectReference includes."""
    props = "".join(f"<{name}>{value}</{name}>" for name, value in (properties or {}).items())
    refs = "".join(f'<ProjectReference Include="{reference}" />' for reference in (references or []))
    return f'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>{props}</PropertyGroup><ItemGroup>{refs}</ItemGroup>{extra}</Project>'


TEST_PROPERTIES = {"IsTestProject": "true", "IsPackable": "false"}


def base_manifest() -> dict:
    """Returns a small, valid manifest: two domains, a libraries view, a tests view and two domain views."""
    return {
        "master": "bodu.slnx",
        "kinds": {kind: kind for kind in ("library", "test", "test-support", "benchmark", "docs")},
        "domains": {
            "core": {"description": "Core", "prefixes": ["Bodu.Core"]},
            "text": {"description": "Text", "prefixes": ["Bodu.Text"]},
        },
        "domainViewKinds": ["library", "test", "test-support"],
        "views": [
            {"file": "bodu.libraries.slnx", "purpose": "Libraries.", "kinds": ["library"]},
            {"file": "bodu.tests.slnx", "purpose": "Tests.", "kinds": ["test"]},
            {"file": "bodu.benchmarks.slnx", "purpose": "Benchmarks.", "kinds": ["benchmark"]},
            {"file": "bodu.core.slnx", "purpose": "Core.", "domains": ["core"]},
            {"file": "bodu.text.slnx", "purpose": "Text.", "domains": ["text"]},
        ],
        "exceptions": [
            {"match": "Bodu.Test/test/Bodu.Test.csproj", "kind": "test", "domain": None, "reason": "Shared harness."},
        ],
        "rules": [
            {"match": "**/bench/**/*.csproj", "kind": "benchmark"},
            {"match": "**/test/*.Test.csproj", "kind": "test"},
            {"match": "**/src/*.csproj", "kind": "library"},
        ],
        "notInMaster": [],
        "externalReferences": [],
        "legacyMasterBuildExclusions": {"reason": "Legacy.", "projects": []},
    }


BASE_PROJECTS = {
    "Bodu.Core/src/Bodu.Core.csproj": project_xml(),
    "Bodu.Core/test/Bodu.Core.Test.csproj": project_xml(TEST_PROPERTIES, ["..\\src\\Bodu.Core.csproj", "..\\..\\Bodu.Test\\test\\Bodu.Test.csproj"]),
    "Bodu.Core/bench/Bodu.Core.Benchmarks.csproj": project_xml({"OutputType": "Exe"}, ["../src/Bodu.Core.csproj"]),
    "Bodu.Test/test/Bodu.Test.csproj": project_xml(TEST_PROPERTIES),
    "Bodu.Text.Ini/src/Bodu.Text.Ini.csproj": project_xml(references=["..\\..\\Bodu.Core\\src\\Bodu.Core.csproj"]),
    "Bodu.Text.Ini/test/Bodu.Text.Ini.Test.csproj": project_xml(TEST_PROPERTIES, ["..\\src\\Bodu.Text.Ini.csproj", "..\\..\\Bodu.Test\\test\\Bodu.Test.csproj"]),
}


class SyntheticRepository:
    """A throwaway repository tree: a master solution, a manifest and project files."""

    def __init__(self, projects: dict[str, str] | None = None, manifest: dict | None = None, master: str | None = None) -> None:
        self._directory = tempfile.TemporaryDirectory()
        self.root = self._directory.name
        self.projects = dict(BASE_PROJECTS if projects is None else projects)
        self.manifest = manifest or base_manifest()
        for path, text in self.projects.items():
            self.write(path, text)
        self.write("bodu.slnx", master if master is not None else self.master_text(self.projects))
        self.write(gs.MANIFEST_RELATIVE_PATH, json.dumps(self.manifest))

    @staticmethod
    def master_text(projects: dict[str, str]) -> str:
        entries = "".join(f'<Folder Name="/X/"><Project Path="{path.replace("/", chr(92))}" /></Folder>' for path in projects)
        return f"<Solution>{entries}</Solution>"

    def write(self, path: str, text: str) -> None:
        full_path = os.path.join(self.root, path)
        os.makedirs(os.path.dirname(full_path) or self.root, exist_ok=True)
        with open(full_path, "w", encoding="utf-8", newline="\n") as stream:
            stream.write(text)

    def read(self, path: str) -> str:
        with open(os.path.join(self.root, path), encoding="utf-8") as stream:
            return stream.read()

    def analyze(self) -> gs.Result:
        return gs.analyze(self.root, gs.load_manifest(json.dumps(self.manifest)))

    def main(self, *arguments: str) -> tuple[int, str]:
        errors = io.StringIO()
        with contextlib.redirect_stdout(io.StringIO()), contextlib.redirect_stderr(errors):
            code = gs.main([*arguments, "--repo-root", self.root])
        return code, errors.getvalue()

    def close(self) -> None:
        self._directory.cleanup()


class RepositoryTestCase(unittest.TestCase):
    """Creates a synthetic repository per test and removes it afterwards."""

    def repository(self, **kwargs) -> SyntheticRepository:
        repository = SyntheticRepository(**kwargs)
        self.addCleanup(repository.close)
        return repository

    def assertProblem(self, problems: list[str], fragment: str) -> None:
        self.assertTrue(any(fragment in problem for problem in problems), f"no problem contains {fragment!r}: {problems}")


class GlobTests(unittest.TestCase):
    """The glob dialect the manifest's match patterns use."""

    def test_glob_match_when_double_star_slash_spans_no_directories_should_match(self) -> None:
        self.assertTrue(gs.glob_match("**/bench/**/*.csproj", "Bodu.Text/bench/Bodu.Text.Benchmarks.csproj"))

    def test_glob_match_when_double_star_slash_spans_several_directories_should_match(self) -> None:
        self.assertTrue(gs.glob_match("**/bench/**/*.csproj", "Bodu.Core/bench/A.Benchmarks/A.Benchmarks.csproj"))

    def test_glob_match_when_single_star_would_cross_a_directory_should_not_match(self) -> None:
        self.assertFalse(gs.glob_match("**/src/*.csproj", "Bodu.Core/src/nested/Bodu.Core.csproj"))
        self.assertFalse(gs.glob_match("samples/*.csproj", "samples/Core/A.csproj"))

    def test_glob_match_when_pattern_has_regex_metacharacters_should_match_them_literally(self) -> None:
        self.assertTrue(gs.glob_match("Bodu.Core/src/Bodu.Core.csproj", "Bodu.Core/src/Bodu.Core.csproj"))
        self.assertFalse(gs.glob_match("Bodu.Core/src/Bodu.Core.csproj", "BoduXCore/src/Bodu.Core.csproj"))


class PathTests(unittest.TestCase):
    """Path normalisation and the deterministic order of view entries."""

    def test_normalize_when_path_has_backslashes_and_dot_segments_should_return_forward_slash_canonical_path(self) -> None:
        self.assertEqual("Bodu.Core/src/Bodu.Core.csproj", gs.normalize("Bodu.Core\\test\\..\\src\\.\\Bodu.Core.csproj"))

    def test_sort_key_when_folder_has_dotted_sibling_should_order_folder_first(self) -> None:
        paths = ["Bodu.Collections.Concurrent/src/A.csproj", "Bodu.Collections/src/B.csproj", "bodu.alpha/src/C.csproj"]
        self.assertEqual(
            ["bodu.alpha/src/C.csproj", "Bodu.Collections/src/B.csproj", "Bodu.Collections.Concurrent/src/A.csproj"],
            sorted(paths, key=gs.sort_key),
        )


class ReadSolutionTests(unittest.TestCase):
    """Reading project paths and Debug exclusions from a .slnx."""

    def test_read_solution_when_master_has_folders_and_backslashes_should_return_normalized_paths(self) -> None:
        solution = gs.read_solution('<Solution><Folder Name="/A/"><Project Path="A\\src\\A.csproj" /></Folder><Project Path="B/src/B.csproj" /></Solution>')
        self.assertEqual(["A/src/A.csproj", "B/src/B.csproj"], solution.projects)

    def test_read_solution_when_project_is_excluded_from_debug_should_report_it(self) -> None:
        solution = gs.read_solution('<Solution><Project Path="A/bench/A.csproj"><Build Solution="Debug|*" Project="false" /></Project><Project Path="B/src/B.csproj" /></Solution>')
        self.assertEqual(["A/bench/A.csproj"], solution.debug_excluded)


class ViewContractTests(unittest.TestCase):
    """The selection-only contract every view must meet."""

    def test_view_contract_problems_when_view_is_rendered_should_report_nothing(self) -> None:
        self.assertEqual([], gs.view_contract_problems(gs.render_view({"B/src/B.csproj", "A/src/A.csproj"})))

    def test_view_contract_problems_when_view_has_a_build_element_should_report_it(self) -> None:
        problems = gs.view_contract_problems('<Solution><Project Path="A/A.csproj"><Build Project="false" /></Project></Solution>')
        self.assertTrue(any("content inside" in problem for problem in problems), problems)

    def test_view_contract_problems_when_view_has_folders_files_or_configurations_should_report_each(self) -> None:
        problems = gs.view_contract_problems(
            '<Solution><Configurations><Platform Name="x64" /></Configurations><Folder Name="/A/"><File Path="x.props" /></Folder></Solution>'
        )
        self.assertTrue(any("<Configurations>" in problem for problem in problems), problems)
        self.assertTrue(any("<Folder>" in problem for problem in problems), problems)

    def test_view_contract_problems_when_project_has_extra_attributes_should_report_them(self) -> None:
        problems = gs.view_contract_problems('<Solution><Project Path="A/A.csproj" Type="Classic C#" /></Solution>')
        self.assertTrue(any("Type" in problem for problem in problems), problems)

    def test_view_contract_problems_when_view_has_a_comment_should_report_it(self) -> None:
        problems = gs.view_contract_problems('<Solution><!-- note --><Project Path="A/A.csproj" /></Solution>')
        self.assertTrue(any("comment" in problem for problem in problems), problems)

    def test_view_contract_problems_when_root_is_not_solution_should_report_it(self) -> None:
        self.assertTrue(any("not <Solution>" in problem for problem in gs.view_contract_problems("<Project />")))

    def test_render_view_when_given_paths_in_any_order_should_return_sorted_lf_text(self) -> None:
        text = gs.render_view({"b/src/B.csproj", "A/src/A.csproj"})
        self.assertEqual('<Solution>\n  <Project Path="A/src/A.csproj" />\n  <Project Path="b/src/B.csproj" />\n</Solution>\n', text)


class ReadProjectTests(RepositoryTestCase):
    """Resolving a project's references and reading its properties."""

    def test_read_project_when_reference_is_an_item_expansion_should_resolve_the_items(self) -> None:
        extra = '<ItemGroup><Fixture Include="Fixtures\\One\\One.csproj" /><Fixture Include="Fixtures\\Two\\Two.csproj" /><ProjectReference Include="@(Fixture)" /></ItemGroup>'
        repository = self.repository(projects={"P/test/P.Test.csproj": project_xml(TEST_PROPERTIES, extra=extra)})
        info = gs.read_project(repository.root, "P/test/P.Test.csproj")
        self.assertEqual(["P/test/Fixtures/One/One.csproj", "P/test/Fixtures/Two/Two.csproj"], info.references)
        self.assertEqual([], info.unresolved)

    def test_read_project_when_reference_uses_a_property_should_report_it_unresolved(self) -> None:
        repository = self.repository(projects={"P/src/P.csproj": project_xml(references=["$(RepoRoot)Q/src/Q.csproj"])})
        info = gs.read_project(repository.root, "P/src/P.csproj")
        self.assertEqual(["$(RepoRoot)Q/src/Q.csproj"], info.unresolved)

    def test_read_project_when_properties_are_set_should_read_them(self) -> None:
        repository = self.repository(projects={"P/test/P.Test.csproj": project_xml(TEST_PROPERTIES)})
        info = gs.read_project(repository.root, "P/test/P.Test.csproj")
        self.assertIs(True, info.flag("IsTestProject"))
        self.assertIs(False, info.flag("IsPackable"))
        self.assertIsNone(info.flag("PackAsTool"))


class ClassifyTests(unittest.TestCase):
    """Kinds and domains assigned from exceptions, rules and name prefixes."""

    @staticmethod
    def project(path: str, properties: dict[str, str] | None = None) -> gs.ProjectInfo:
        return gs.ProjectInfo(path=path, properties=dict(properties or {}))

    def test_classify_when_an_exception_matches_should_take_its_kind_and_domain_over_the_rules(self) -> None:
        classification, problems = gs.classify(self.project("Bodu.Test/test/Bodu.Test.csproj", TEST_PROPERTIES), base_manifest())
        self.assertEqual([], problems)
        self.assertEqual(("test", None), (classification.kind, classification.domain))

    def test_classify_when_no_exception_matches_should_take_the_first_matching_rule(self) -> None:
        classification, problems = gs.classify(self.project("Bodu.Core/bench/X/Bodu.Core.X.csproj"), base_manifest())
        self.assertEqual([], problems)
        self.assertEqual(("benchmark", "core"), (classification.kind, classification.domain))

    def test_classify_when_nothing_matches_should_report_the_project(self) -> None:
        classification, problems = gs.classify(self.project("Bodu.Core/aot/Bodu.Core.Aot.csproj"), base_manifest())
        self.assertIsNone(classification)
        self.assertTrue(problems and "matches no rule or exception" in problems[0], problems)

    def test_classify_when_two_exceptions_match_should_report_the_overlap(self) -> None:
        manifest = base_manifest()
        manifest["exceptions"].append({"match": "Bodu.Test/**/*.csproj", "kind": "test-support", "reason": "Overlap."})
        classification, problems = gs.classify(self.project("Bodu.Test/test/Bodu.Test.csproj", TEST_PROPERTIES), manifest)
        self.assertIsNone(classification)
        self.assertTrue(problems and "more than one exception" in problems[0], problems)

    def test_classify_when_no_domain_prefix_matches_should_report_the_project(self) -> None:
        _, problems = gs.classify(self.project("Other/src/Other.csproj"), base_manifest())
        self.assertTrue(any("no domain prefix" in problem for problem in problems), problems)

    def test_classify_when_prefixes_nest_should_take_the_longest(self) -> None:
        manifest = base_manifest()
        manifest["domains"]["text"]["prefixes"].append("Bodu.Core.Text")
        classification, problems = gs.classify(self.project("Bodu.Core.Text/src/Bodu.Core.Text.csproj"), manifest)
        self.assertEqual([], problems)
        self.assertEqual("text", classification.domain)

    def test_classify_when_prefix_is_only_a_string_prefix_should_not_claim_the_name(self) -> None:
        self.assertEqual([], gs.domain_for_name("Bodu.CoreX", base_manifest()["domains"]))

    def test_classify_when_test_does_not_set_is_test_project_should_report_it(self) -> None:
        _, problems = gs.classify(self.project("Bodu.Core/test/Bodu.Core.Test.csproj"), base_manifest())
        self.assertTrue(any("IsTestProject" in problem for problem in problems), problems)

    def test_classify_when_test_named_project_is_not_a_test_kind_should_report_it(self) -> None:
        manifest = base_manifest()
        manifest["exceptions"].append({"match": "X/src/X.Test.csproj", "kind": "test-support", "reason": "Support."})
        _, problems = gs.classify(self.project("X/src/X.Test.csproj", TEST_PROPERTIES), manifest)
        self.assertTrue(any("is named *.Test.csproj" in problem for problem in problems), problems)

    def test_classify_when_library_is_not_packable_or_is_an_executable_should_report_both(self) -> None:
        _, problems = gs.classify(self.project("Bodu.Core/src/Bodu.Core.csproj", {"IsPackable": "false", "OutputType": "Exe"}), base_manifest())
        self.assertTrue(any("IsPackable" in problem for problem in problems), problems)
        self.assertTrue(any("executable" in problem for problem in problems), problems)


class ReferenceClosureTests(unittest.TestCase):
    """The reference closure each view is built from."""

    def test_reference_closure_when_references_chain_should_include_every_link(self) -> None:
        projects = {
            "a": gs.ProjectInfo("a", references=["b"]),
            "b": gs.ProjectInfo("b", references=["c"]),
            "c": gs.ProjectInfo("c"),
            "d": gs.ProjectInfo("d"),
        }
        self.assertEqual({"a", "b", "c"}, gs.reference_closure({"a"}, projects))

    def test_reference_closure_when_a_reference_is_outside_the_master_should_not_follow_it(self) -> None:
        projects = {"a": gs.ProjectInfo("a", references=["external"])}
        self.assertEqual({"a"}, gs.reference_closure({"a"}, projects))


class AnalyzeTests(RepositoryTestCase):
    """End-to-end runs over a synthetic repository."""

    def test_analyze_when_repository_is_consistent_should_report_nothing_and_close_each_view_over_references(self) -> None:
        result = self.repository().analyze()
        self.assertEqual([], result.problems)
        self.assertEqual(
            {"Bodu.Core/src/Bodu.Core.csproj", "Bodu.Core/test/Bodu.Core.Test.csproj", "Bodu.Test/test/Bodu.Test.csproj"},
            result.members["bodu.core.slnx"],
        )
        self.assertEqual({"Bodu.Core/src/Bodu.Core.csproj", "Bodu.Text.Ini/src/Bodu.Text.Ini.csproj"}, result.members["bodu.libraries.slnx"])
        self.assertIn("Bodu.Core/src/Bodu.Core.csproj", result.members["bodu.text.slnx"])

    def test_analyze_when_a_project_file_is_not_in_the_master_should_report_it(self) -> None:
        repository = self.repository()
        repository.write("Bodu.Core/bench/Extra/Extra.csproj", project_xml({"OutputType": "Exe"}))
        self.assertProblem(repository.analyze().problems, "Bodu.Core/bench/Extra/Extra.csproj: is not in bodu.slnx")

    def test_analyze_when_a_not_in_master_entry_matches_nothing_should_report_it(self) -> None:
        manifest = base_manifest()
        manifest["notInMaster"].append({"match": "tools/Gone/Gone.csproj", "reason": "Gone."})
        self.assertProblem(self.repository(manifest=manifest).analyze().problems, "notInMaster entry 'tools/Gone/Gone.csproj' matches no project file")

    def test_analyze_when_master_lists_a_missing_project_should_report_it(self) -> None:
        projects = dict(BASE_PROJECTS)
        master = SyntheticRepository.master_text({**projects, "Gone/src/Gone.csproj": ""})
        self.assertProblem(self.repository(projects=projects, master=master).analyze().problems, "Gone/src/Gone.csproj, which does not exist")

    def test_analyze_when_master_gains_a_debug_exclusion_should_report_it(self) -> None:
        master = SyntheticRepository.master_text(BASE_PROJECTS).replace(
            'Path="Bodu.Core\\bench\\Bodu.Core.Benchmarks.csproj" />',
            'Path="Bodu.Core\\bench\\Bodu.Core.Benchmarks.csproj"><Build Solution="Debug|*" Project="false" /></Project>',
        )
        self.assertProblem(self.repository(master=master).analyze().problems, "may not gain one")

    def test_analyze_when_a_recorded_debug_exclusion_is_gone_should_report_the_stale_entry(self) -> None:
        manifest = base_manifest()
        manifest["legacyMasterBuildExclusions"]["projects"].append("Bodu.Core/bench/Bodu.Core.Benchmarks.csproj")
        self.assertProblem(self.repository(manifest=manifest).analyze().problems, "no longer excludes")

    def test_analyze_when_a_reference_leaves_the_master_should_report_it_unless_allowed(self) -> None:
        projects = dict(BASE_PROJECTS)
        projects["Bodu.Core/src/Bodu.Core.csproj"] = project_xml(references=["..\\..\\External\\External.csproj"])
        manifest = base_manifest()
        manifest["notInMaster"].append({"match": "External/External.csproj", "reason": "Separate solution."})
        repository = self.repository(projects=projects, manifest=manifest)
        repository.write("External/External.csproj", project_xml())
        self.assertProblem(repository.analyze().problems, "references External/External.csproj, which bodu.slnx does not list")

        manifest["externalReferences"].append({"from": "Bodu.Core/src/*.csproj", "to": "External/**", "reason": "Analyzer from source."})
        result = repository.analyze()
        self.assertEqual([], result.problems)
        self.assertNotIn("External/External.csproj", result.members["bodu.core.slnx"])

    def test_analyze_when_a_library_references_a_test_support_project_should_report_the_libraries_view(self) -> None:
        projects = dict(BASE_PROJECTS)
        projects["Bodu.Text.Ini/src/Bodu.Text.Ini.csproj"] = project_xml(references=["..\\..\\Bodu.Test\\test\\Bodu.Test.csproj"])
        self.assertProblem(self.repository(projects=projects).analyze().problems, "the libraries view holds only libraries")

    def test_analyze_when_a_kind_has_no_view_should_report_the_unplaced_project(self) -> None:
        manifest = base_manifest()
        manifest["views"] = [view for view in manifest["views"] if view["file"] != "bodu.benchmarks.slnx"]
        self.assertProblem(self.repository(manifest=manifest).analyze().problems, "Bodu.Core/bench/Bodu.Core.Benchmarks.csproj: is in no view")

    def test_analyze_when_an_undeclared_slnx_is_at_the_root_should_report_it(self) -> None:
        repository = self.repository()
        repository.write("bodu.stray.slnx", "<Solution />")
        self.assertProblem(repository.analyze().problems, "bodu.stray.slnx: is not a view")

    def test_analyze_when_msbuild_reads_a_solution_property_should_report_the_line(self) -> None:
        repository = self.repository()
        repository.write("bld/Policy.props", "<Project>\n<PropertyGroup Condition=\"'$(SolutionName)' == 'x'\" />\n</Project>")
        self.assertProblem(repository.analyze().problems, "bld/Policy.props:2: reads $(SolutionName)")

    def test_analyze_when_directory_solution_props_exists_should_report_it(self) -> None:
        repository = self.repository()
        repository.write("Directory.Solution.props", "<Project />")
        self.assertProblem(repository.analyze().problems, "solution-level build customization")

    def test_analyze_when_an_exception_matches_nothing_should_report_the_stale_entry(self) -> None:
        manifest = base_manifest()
        manifest["exceptions"].append({"match": "Gone/src/Gone.csproj", "kind": "library", "reason": "Gone."})
        self.assertProblem(self.repository(manifest=manifest).analyze().problems, "exception 'Gone/src/Gone.csproj' matches no project")

    def test_analyze_when_release_manifest_publishes_a_non_library_should_report_it(self) -> None:
        repository = self.repository()
        repository.write(gs.RELEASE_MANIFEST_RELATIVE_PATH, "# Header\nBodu.Core 0.5.0\nBodu.Test 0.5.0\n")
        self.assertProblem(repository.analyze().problems, "Bodu.Test/test/Bodu.Test.csproj: is published")


class ManifestTests(unittest.TestCase):
    """The structural checks load_manifest applies before any project is read."""

    def test_load_manifest_when_a_view_names_an_undeclared_kind_should_raise(self) -> None:
        manifest = base_manifest()
        manifest["views"][0]["kinds"] = ["librarie"]
        with self.assertRaises(gs.ManifestError):
            gs.load_manifest(json.dumps(manifest))

    def test_load_manifest_when_a_domain_has_no_view_should_raise(self) -> None:
        manifest = base_manifest()
        manifest["views"] = [view for view in manifest["views"] if view["file"] != "bodu.text.slnx"]
        with self.assertRaisesRegex(gs.ManifestError, "domain 'text' has no view"):
            gs.load_manifest(json.dumps(manifest))

    def test_load_manifest_when_an_exception_has_no_reason_should_raise(self) -> None:
        manifest = base_manifest()
        del manifest["exceptions"][0]["reason"]
        with self.assertRaises(gs.ManifestError):
            gs.load_manifest(json.dumps(manifest))

    def test_load_manifest_when_a_view_is_misnamed_should_raise(self) -> None:
        manifest = base_manifest()
        manifest["views"][0]["file"] = "libraries.sln"
        with self.assertRaises(gs.ManifestError):
            gs.load_manifest(json.dumps(manifest))


class MainTests(RepositoryTestCase):
    """The command line: writing, checking and the exit codes."""

    def test_main_when_views_are_written_should_then_pass_check(self) -> None:
        repository = self.repository()
        self.assertEqual(1, repository.main("--check")[0])
        self.assertEqual(0, repository.main()[0])
        self.assertEqual((0, ""), repository.main("--check"))

    def test_main_when_a_committed_view_drifts_should_fail_check_naming_the_difference(self) -> None:
        repository = self.repository()
        repository.main()
        repository.write("bodu.core.slnx", repository.read("bodu.core.slnx").replace('  <Project Path="Bodu.Test/test/Bodu.Test.csproj" />\n', ""))
        code, errors = repository.main("--check")
        self.assertEqual(1, code)
        self.assertIn("bodu.core.slnx: is out of date (missing Bodu.Test/test/Bodu.Test.csproj)", errors)

    def test_main_when_a_committed_view_breaks_the_contract_should_fail_check(self) -> None:
        repository = self.repository()
        repository.main()
        repository.write("bodu.core.slnx", repository.read("bodu.core.slnx").replace("<Solution>", "<Solution>\n  <Folder Name=\"/X/\" />"))
        code, errors = repository.main("--check")
        self.assertEqual(1, code)
        self.assertIn("has a <Folder> element", errors)

    def test_main_when_check_finds_problems_should_write_nothing(self) -> None:
        repository = self.repository()
        repository.write("Bodu.Core/bench/Extra/Extra.csproj", project_xml({"OutputType": "Exe"}))
        self.assertEqual(1, repository.main()[0])
        self.assertFalse(os.path.exists(os.path.join(repository.root, "bodu.core.slnx")))

    def test_main_when_manifest_is_not_json_should_exit_two(self) -> None:
        repository = self.repository()
        repository.write(gs.MANIFEST_RELATIVE_PATH, "{")
        self.assertEqual(2, repository.main("--check")[0])


if __name__ == "__main__":
    unittest.main()
