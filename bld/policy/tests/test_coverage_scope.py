"""Tests for bld/coverage-scope.sh, the Coverage workflow's scope decision.

The script is run end to end: each test lays out a temporary tree holding the test projects the decision can
select, runs the script from that tree with the changed paths on standard input, as the workflow's "Decide
collection scope" step pipes `git diff --name-only` into it, and reads the three output lines the step appends to
$GITHUB_OUTPUT.
"""

from __future__ import annotations

import os
import subprocess
import tempfile
import unittest

SCRIPT = os.path.join(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))), "coverage-scope.sh")

# The test projects the temporary tree holds. Bodu.CodeStyle's is laid out like the real one, under the separate
# solution's folder, so the decision has a project to leave out; Bodu.Untested has sources but no test project.
NUMERICS_TEST = "Bodu.Numerics/test/Bodu.Numerics.Test.csproj"
TOML_TEST = "Bodu.Text.Toml/test/Bodu.Text.Toml.Test.csproj"
CORE_TEST = "Bodu.Core/test/Bodu.Core.Test.csproj"
CODESTYLE_TEST = "Bodu.CodeStyle/Bodu.CodeStyle.XmlDocumentation.Core/test/Bodu.CodeStyle.XmlDocumentation.Core.Test.csproj"

TREE = (
    "Bodu.Numerics/src/Fraction.cs",
    NUMERICS_TEST,
    "Bodu.Text.Toml/src/TomlSerializer.cs",
    TOML_TEST,
    "Bodu.Core/src/ThrowHelper.cs",
    CORE_TEST,
    "Bodu.CodeStyle/Bodu.CodeStyle.XmlDocumentation.Core/src/Rule.cs",
    CODESTYLE_TEST,
    "Bodu.Untested/src/Program.cs",
)

DEFERRAL = "Diff touches shared build configuration or Bodu.Core; deferring to the scheduled full run."
NOTHING_TO_COLLECT = "No production source changed; nothing to collect."

# A pipe buffer holds 64 KiB on Linux; this many paths of this shape run to about 180 KiB, so a writer that
# `grep -q` leaves mid-list is still writing when grep exits (#1337).
LONG_LIST_LENGTH = 6000


class ScopeRun:
    """The outcome of one run of the script: its exit code, what it wrote to each stream, and the outputs it decided."""

    def __init__(self, returncode: int, stdout: str, stderr: str) -> None:
        self.returncode = returncode
        self.stdout = stdout
        self.stderr = stderr

    @property
    def outputs(self) -> dict[str, str]:
        """The `name=value` lines on standard output, in order."""
        return dict(line.split("=", 1) for line in self.stdout.splitlines() if "=" in line)


def run_scope(changed: list[str], *, event: str | None = None, shards: str | None = None, shard_count: str | None = "8") -> ScopeRun:
    """Lays out the tree, runs the script from it over the changed paths, and returns what it decided.

    `event` and `shards` are passed as the --event and --shards options when given; `shard_count` is the SHARD_COUNT
    environment variable the workflow sets, or none when None.
    """
    with tempfile.TemporaryDirectory() as root:
        for path in TREE:
            full = os.path.join(root, path)
            os.makedirs(os.path.dirname(full), exist_ok=True)
            with open(full, "w", encoding="utf-8", newline="\n") as handle:
                handle.write("<!-- placeholder -->\n" if path.endswith(".csproj") else "// placeholder\n")

        env = {key: value for key, value in os.environ.items() if key != "SHARD_COUNT"}
        if shard_count is not None:
            env["SHARD_COUNT"] = shard_count

        args = ["bash", SCRIPT]
        if event is not None:
            args += ["--event", event]
        if shards is not None:
            args += ["--shards", shards]

        result = subprocess.run(
            args, cwd=root, env=env, input="".join(path + "\n" for path in changed), capture_output=True, text=True
        )
        return ScopeRun(result.returncode, result.stdout, result.stderr)


class ScopeTestCase(unittest.TestCase):
    """The assertions every decision shares: the exit code, the three output lines, and the sentence on standard error."""

    def assert_decision(self, run: ScopeRun, *, mode: str, projects: str, shards: str) -> None:
        """Asserts a successful run whose standard output is exactly the three lines the workflow reads, in order."""
        self.assertEqual(0, run.returncode, run.stderr)
        self.assertEqual(
            [f"mode={mode}", f"projects={projects}", f"shards={shards}"], run.stdout.splitlines(), run.stderr
        )

    def assert_deferred(self, run: ScopeRun) -> None:
        self.assert_decision(run, mode="skip", projects="", shards="")
        self.assertIn(DEFERRAL, run.stderr)

    def assert_nothing_to_collect(self, run: ScopeRun) -> None:
        self.assert_decision(run, mode="skip", projects="", shards="1")
        self.assertIn(NOTHING_TO_COLLECT, run.stderr)
        self.assertNotIn(DEFERRAL, run.stderr)

    def assert_collects(self, run: ScopeRun, *projects: str) -> None:
        self.assert_decision(run, mode="diff", projects=" ".join(projects), shards="1")
        self.assertIn("Collecting: " + " ".join(projects), run.stderr)
        self.assertNotIn(DEFERRAL, run.stderr)


class DeferralTests(ScopeTestCase):
    """The changes that fan out to effectively every project, and so defer to the scheduled full run."""

    def test_decide_when_directory_build_props_changed_should_defer(self) -> None:
        self.assert_deferred(run_scope(["Directory.Build.props"]))

    def test_decide_when_directory_build_targets_changed_should_defer(self) -> None:
        self.assert_deferred(run_scope(["Directory.Build.targets"]))

    def test_decide_when_directory_packages_props_changed_should_defer(self) -> None:
        self.assert_deferred(run_scope(["Directory.Packages.props"]))

    def test_decide_when_coverage_runsettings_changed_should_defer(self) -> None:
        self.assert_deferred(run_scope(["coverage.runsettings"]))

    def test_decide_when_bodu_core_source_changed_should_defer(self) -> None:
        self.assert_deferred(run_scope(["Bodu.Core/src/ThrowHelper.cs"]))

    def test_decide_when_bodu_core_source_changed_beside_another_project_should_defer(self) -> None:
        self.assert_deferred(run_scope(["Bodu.Numerics/src/Fraction.cs", "Bodu.Core/src/ThrowHelper.cs"]))

    def test_decide_when_deferring_path_leads_a_list_longer_than_a_pipe_buffer_should_defer(self) -> None:
        """The #1337 regression: `grep -q` over a pipe exits at the first line, before the writer has written the
        rest, and the writer's SIGPIPE then turned the test false. The deferring path leads the list because that
        is the order that reproduced it; a match at the end is read only once the whole list is."""
        changed = ["Directory.Build.props"] + [f"Bodu.Numerics/src/File{i}.cs" for i in range(LONG_LIST_LENGTH)]

        self.assert_deferred(run_scope(changed))

    def test_decide_when_deferring_path_ends_a_list_longer_than_a_pipe_buffer_should_defer(self) -> None:
        changed = [f"Bodu.Numerics/src/File{i}.cs" for i in range(LONG_LIST_LENGTH)] + ["Directory.Build.props"]

        self.assert_deferred(run_scope(changed))


class ProjectMappingTests(ScopeTestCase):
    """The mapping from changed production sources to the test projects a diff-scoped run collects."""

    def test_decide_when_project_source_changed_should_collect_its_test_project(self) -> None:
        self.assert_collects(run_scope(["Bodu.Numerics/src/Fraction.cs"]), NUMERICS_TEST)

    def test_decide_when_two_sources_of_one_project_changed_should_name_its_test_project_once(self) -> None:
        changed = ["Bodu.Numerics/src/Fraction.cs", "Bodu.Numerics/src/Numerics/Interval.cs"]

        self.assert_collects(run_scope(changed), NUMERICS_TEST)

    def test_decide_when_sources_of_two_projects_changed_should_name_both_test_projects_sorted(self) -> None:
        changed = ["Bodu.Text.Toml/src/TomlSerializer.cs", "Bodu.Numerics/src/Fraction.cs"]

        self.assert_collects(run_scope(changed), NUMERICS_TEST, TOML_TEST)

    def test_decide_when_source_and_tests_of_one_project_changed_should_collect_its_test_project(self) -> None:
        changed = ["Bodu.Numerics/src/Fraction.cs", "Bodu.Numerics/test/Numerics/FractionTests.cs"]

        self.assert_collects(run_scope(changed), NUMERICS_TEST)

    def test_decide_when_only_tests_changed_should_collect_nothing(self) -> None:
        self.assert_nothing_to_collect(run_scope(["Bodu.Numerics/test/Numerics/FractionTests.cs"]))

    def test_decide_when_only_documentation_outside_bld_changed_should_collect_nothing(self) -> None:
        changed = ["docs/articles/code-coverage.md", "CLAUDE.md", ".github/workflows/release.yml"]

        self.assert_nothing_to_collect(run_scope(changed))

    def test_decide_when_nothing_changed_should_collect_nothing(self) -> None:
        self.assert_nothing_to_collect(run_scope([]))

    def test_decide_when_codestyle_source_changed_should_leave_it_out(self) -> None:
        self.assert_nothing_to_collect(run_scope(["Bodu.CodeStyle/Bodu.CodeStyle.XmlDocumentation.Core/src/Rule.cs"]))

    def test_decide_when_codestyle_source_changed_beside_a_project_should_collect_that_project_alone(self) -> None:
        changed = ["Bodu.CodeStyle/Bodu.CodeStyle.XmlDocumentation.Core/src/Rule.cs", "Bodu.Numerics/src/Fraction.cs"]

        self.assert_collects(run_scope(changed), NUMERICS_TEST)

    def test_decide_when_changed_project_has_no_test_project_should_collect_nothing(self) -> None:
        self.assert_nothing_to_collect(run_scope(["Bodu.Untested/src/Program.cs"]))


class FullRunTests(ScopeTestCase):
    """A scheduled or dispatched run: every shard, no project list, nothing read from standard input."""

    def test_decide_when_event_is_schedule_should_select_every_shard(self) -> None:
        run = run_scope([], event="schedule", shard_count="8")

        self.assert_decision(run, mode="full", projects="", shards="1,2,3,4,5,6,7,8")

    def test_decide_when_event_is_workflow_dispatch_should_select_every_shard(self) -> None:
        run = run_scope([], event="workflow_dispatch", shard_count="4")

        self.assert_decision(run, mode="full", projects="", shards="1,2,3,4")

    def test_decide_when_shards_option_given_should_take_precedence_over_shard_count(self) -> None:
        run = run_scope([], event="schedule", shards="3", shard_count="8")

        self.assert_decision(run, mode="full", projects="", shards="1,2,3")

    def test_decide_when_full_run_has_no_shard_count_should_fail(self) -> None:
        run = run_scope([], event="schedule", shard_count=None)

        self.assertEqual(2, run.returncode, run.stderr)
        self.assertEqual("", run.stdout)
        self.assertIn("shard count", run.stderr)

    def test_decide_when_full_run_has_a_non_numeric_shard_count_should_fail(self) -> None:
        run = run_scope([], event="schedule", shard_count="eight")

        self.assertEqual(2, run.returncode, run.stderr)
        self.assertEqual("", run.stdout)

    def test_decide_when_event_is_pull_request_should_read_the_changed_paths(self) -> None:
        self.assert_collects(run_scope(["Bodu.Numerics/src/Fraction.cs"], event="pull_request"), NUMERICS_TEST)


class BuildFolderTests(ScopeTestCase):
    """What under bld/ defers (#1376).

    The deferral exists for the build configuration the whole tree shares: the MSBuild property and target files,
    the coverage scripts and their thresholds, and the release manifest, where the per-project mapping would be a
    full run in disguise. bld/ also holds documentation and artwork, which cannot change what a test run covers, so
    a change to those is judged like any other path outside bld/: the production sources beside it decide.
    """

    def test_decide_when_release_notes_and_project_source_changed_should_collect_its_test_project(self) -> None:
        changed = ["bld/RELEASING.md", "Bodu.Numerics/src/Fraction.cs"]

        self.assert_collects(run_scope(changed), NUMERICS_TEST)

    def test_decide_when_only_release_notes_changed_should_collect_nothing(self) -> None:
        self.assert_nothing_to_collect(run_scope(["bld/RELEASING.md"]))

    def test_decide_when_only_signing_or_policy_notes_changed_should_collect_nothing(self) -> None:
        self.assert_nothing_to_collect(run_scope(["bld/SIGNING.md", "bld/policy/POLICIES.md"]))

    def test_decide_when_only_icons_readme_changed_should_collect_nothing(self) -> None:
        self.assert_nothing_to_collect(run_scope(["bld/icons/README.md"]))

    def test_decide_when_only_artwork_changed_should_collect_nothing(self) -> None:
        changed = ["bld/artwork/generate-hero-banners.py", "bld/icons/Bodu.Core.png", "bld/icons/svg/Bodu.Core.svg"]

        self.assert_nothing_to_collect(run_scope(changed))

    def test_decide_when_release_notes_and_bodu_core_source_changed_should_defer(self) -> None:
        self.assert_deferred(run_scope(["bld/RELEASING.md", "Bodu.Core/src/ThrowHelper.cs"]))

    def test_decide_when_release_notes_and_build_props_changed_should_defer(self) -> None:
        self.assert_deferred(run_scope(["bld/RELEASING.md", "bld/Bodu.props"]))

    def test_decide_when_build_props_under_bld_changed_should_defer(self) -> None:
        self.assert_deferred(run_scope(["bld/Bodu.props"]))

    def test_decide_when_build_targets_under_bld_changed_should_defer(self) -> None:
        self.assert_deferred(run_scope(["bld/InternalsVisibleTo.targets"]))

    def test_decide_when_coverage_script_changed_should_defer(self) -> None:
        self.assert_deferred(run_scope(["bld/collect-coverage.sh"]))

    def test_decide_when_coverage_thresholds_changed_should_defer(self) -> None:
        self.assert_deferred(run_scope(["bld/coverage-thresholds.json"]))

    def test_decide_when_release_manifest_changed_should_defer(self) -> None:
        self.assert_deferred(run_scope(["bld/release-manifest.txt"]))

    def test_decide_when_build_props_under_bld_leads_a_list_longer_than_a_pipe_buffer_should_defer(self) -> None:
        """The #1337 order again, for the bld/ test: a deferring path first, then more than a pipe buffer of others."""
        changed = ["bld/Bodu.props"] + [f"bld/icons/svg/Icon{i}.svg" for i in range(LONG_LIST_LENGTH)]

        self.assert_deferred(run_scope(changed))


class UsageTests(ScopeTestCase):
    """The option surface."""

    def test_decide_when_option_is_unknown_should_fail_without_deciding(self) -> None:
        result = subprocess.run(["bash", SCRIPT, "--bogus"], input="", capture_output=True, text=True)

        self.assertEqual(2, result.returncode, result.stderr)
        self.assertEqual("", result.stdout)
        self.assertIn("unknown option", result.stderr)

    def test_help_when_asked_should_print_the_usage_and_exit_zero(self) -> None:
        result = subprocess.run(["bash", SCRIPT, "--help"], input="", capture_output=True, text=True)

        self.assertEqual(0, result.returncode, result.stderr)
        self.assertIn("Usage:", result.stdout)
        self.assertIn("--event <name>", result.stdout)
        self.assertNotIn("set -euo pipefail", result.stdout)


if __name__ == "__main__":
    unittest.main()
