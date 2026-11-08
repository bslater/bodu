"""Tests for tools/Update-CoverageThresholds.ps1.

The script is run end to end, as the floor-raising step runs it: each test writes package rows and a threshold file
into a temporary directory, runs the script over them with -RowsPath and -ThresholdPath, and reads the thresholds it
wrote and what it printed. The rows have the shape tools/New-CoverageMatrix.ps1 writes to packages.json; a row's
HardwareGated field is the verdict that script records for a package with hardware-gated files: whether the shard that
collected the package's own tests could execute them, in which case a floor recorded from the run is one a shard that
could not never reaches, so the floor is held (#1377).
"""

from __future__ import annotations

import json
import os
import shutil
import subprocess
import tempfile
import unittest

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))))
SCRIPT = os.path.join(ROOT, "tools", "Update-CoverageThresholds.ps1")

# The crypto floors as bld/coverage-thresholds.json holds them, and the rates scheduled run #300 measured on a shard
# with AVX-512: the pair the current tool proposed to raise (#1377).
CRYPTO = "Bodu.Security.Cryptography"
CRYPTO_FLOORS = {"line": 96.9, "branch": 92.0}

# The phrases the script's output carries for a hardware-gated hold, and for rows that record no verdict.
HELD_COUNTED = "could execute its hardware-gated kernels"
HELD_UNKNOWN = "could not be determined"
HELD_RULE = "raised only from a run whose shard could not"
NO_VERDICT_WARNING = "record no hardware-gating verdict"


def row(package: str, line: float, branch: float, **fields: object) -> dict[str, object]:
    """Returns one collected package row as New-CoverageMatrix.ps1 writes it, with any extra fields given."""
    data: dict[str, object] = {
        "Package": package,
        "Status": "Stable",
        "Collected": True,
        "LineRate": line,
        "BranchRate": branch,
        "CoveredLines": 1000,
        "TotalLines": 1050,
    }
    data.update(fields)
    return data


class ThresholdRun:
    """The outcome of one run of the script: its exit code, everything it wrote, and the threshold file after it."""

    def __init__(self, returncode: int, output: str, written: str, thresholds: dict[str, dict[str, float]]) -> None:
        self.returncode = returncode
        self.output = output
        self.written = written
        self.thresholds = thresholds


def run_script(rows: list[dict[str, object]], thresholds: dict[str, dict[str, float]], *args: str) -> ThresholdRun:
    """Writes the rows and thresholds to a temporary directory and runs the script over them."""
    pwsh = shutil.which("pwsh")
    if pwsh is None:
        raise AssertionError("pwsh is not on PATH; install PowerShell 7, which the coverage tools run under")

    with tempfile.TemporaryDirectory() as root:
        rows_path = os.path.join(root, "packages.json")
        threshold_path = os.path.join(root, "coverage-thresholds.json")
        with open(rows_path, "w", encoding="utf-8", newline="\n") as handle:
            json.dump(rows, handle, indent=2)
        with open(threshold_path, "w", encoding="utf-8", newline="\n") as handle:
            json.dump(thresholds, handle, indent=2)

        result = subprocess.run(
            [pwsh, "-NoProfile", "-NonInteractive", "-File", SCRIPT, "-RowsPath", rows_path, "-ThresholdPath", threshold_path, *args],
            cwd=root,
            capture_output=True,
            text=True,
        )

        with open(threshold_path, encoding="utf-8") as handle:
            written = handle.read()
        return ThresholdRun(result.returncode, result.stdout + result.stderr, written, json.loads(written))


class FloorRaisingTests(unittest.TestCase):
    """The ratchet's direction: a floor moves up from a measurement that clears it, and never down."""

    def test_floor_when_measured_clears_it_by_more_than_the_grace_should_be_raised_to_the_measurement_less_the_grace(self) -> None:
        run = run_script([row("Bodu.Sample", 95.27, 91.0)], {"Bodu.Sample": {"line": 90.0, "branch": 85.0}})

        self.assertEqual(0, run.returncode, run.output)
        self.assertEqual({"line": 94.8, "branch": 90.5}, run.thresholds["Bodu.Sample"])
        self.assertIn("Bodu.Sample line: 90% -> 94.8% (measured 95.27%)", run.output)
        self.assertIn("Bodu.Sample branch: 85% -> 90.5% (measured 91%)", run.output)

    def test_floor_when_measured_is_within_the_grace_or_below_should_be_held(self) -> None:
        rows = [row("Bodu.Within", 95.5, 85.4), row("Bodu.Below", 93.0, 80.0)]
        thresholds = {"Bodu.Within": {"line": 95.0, "branch": 85.0}, "Bodu.Below": {"line": 95.0, "branch": 85.0}}

        run = run_script(rows, thresholds)

        self.assertEqual(0, run.returncode, run.output)
        self.assertEqual(thresholds, run.thresholds)
        self.assertIn("No floor raised; the ratchet is unchanged.", run.output)
        self.assertRegex(run.output, r"\b4 metric\(s\) held\b")

    def test_floor_when_package_is_absent_from_the_rows_should_be_kept(self) -> None:
        thresholds = {"Bodu.Absent": {"line": 80.0, "branch": 70.0}, "Bodu.Sample": {"line": 90.0, "branch": 85.0}}

        run = run_script([row("Bodu.Sample", 95.27, 91.0)], thresholds)

        self.assertEqual(0, run.returncode, run.output)
        self.assertEqual({"line": 80.0, "branch": 70.0}, run.thresholds["Bodu.Absent"])
        self.assertEqual({"line": 94.8, "branch": 90.5}, run.thresholds["Bodu.Sample"])

    def test_floor_when_what_if_should_be_reported_and_not_written(self) -> None:
        thresholds = {"Bodu.Sample": {"line": 90.0, "branch": 85.0}}

        run = run_script([row("Bodu.Sample", 95.27, 91.0)], thresholds, "-WhatIf")

        self.assertEqual(0, run.returncode, run.output)
        self.assertEqual(json.dumps(thresholds, indent=2), run.written)
        self.assertIn("What if:", run.output)
        self.assertIn("Bodu.Sample line: 90% -> 94.8% (measured 95.27%)", run.output)


class HardwareGatedRowTests(unittest.TestCase):
    """The HardwareGated verdict New-CoverageMatrix.ps1 records per row, and the rule the script applies to it (#1377).

    A package with hardware-gated files is judged by the shard that collected its own tests. Where that shard could
    execute the files, the kernels and the lines that dispatch to them all ran, so a floor recorded from that figure is
    one a shard without the hardware never reaches: the floor is raised only from a run whose shard could not.
    """

    def test_floor_when_row_is_counted_should_be_held_on_both_metrics_and_say_why(self) -> None:
        run = run_script([row(CRYPTO, 97.5, 93.3, HardwareGated="counted")], {CRYPTO: CRYPTO_FLOORS})

        self.assertEqual(0, run.returncode, run.output)
        self.assertEqual(CRYPTO_FLOORS, run.thresholds[CRYPTO])
        self.assertNotIn("->", run.output)
        self.assertRegex(run.output, rf"{CRYPTO}.*{HELD_COUNTED}")
        self.assertIn(HELD_RULE, run.output)
        self.assertRegex(run.output, r"\b2 metric\(s\) held\b")
        self.assertNotIn(NO_VERDICT_WARNING, run.output)

    def test_floor_when_row_is_counted_and_package_has_no_floor_should_not_be_given_one(self) -> None:
        """A held package without a floor is left without one: a 0 / 0 placeholder would pass -Check at any coverage."""
        run = run_script([row("Bodu.NewGated", 95.0, 90.0, HardwareGated="counted")], {"Bodu.Other": {"line": 90.0, "branch": 80.0}})

        self.assertEqual(0, run.returncode, run.output)
        self.assertEqual({"Bodu.Other": {"line": 90.0, "branch": 80.0}}, run.thresholds)
        self.assertNotIn("->", run.output)
        self.assertRegex(run.output, r"Bodu\.NewGated.*no floor")
        self.assertIn(HELD_COUNTED, run.output)
        self.assertNotIn(NO_VERDICT_WARNING, run.output)

    def test_floor_when_row_is_unknown_should_be_held_on_both_metrics_and_say_why(self) -> None:
        run = run_script([row(CRYPTO, 97.5, 93.3, HardwareGated="unknown")], {CRYPTO: CRYPTO_FLOORS})

        self.assertEqual(0, run.returncode, run.output)
        self.assertEqual(CRYPTO_FLOORS, run.thresholds[CRYPTO])
        self.assertNotIn("->", run.output)
        self.assertRegex(run.output, rf"{CRYPTO}.*{HELD_UNKNOWN}")
        self.assertIn(HELD_RULE, run.output)
        self.assertNotIn(NO_VERDICT_WARNING, run.output)

    def test_floor_when_row_is_excluded_should_be_raised(self) -> None:
        run = run_script([row(CRYPTO, 97.5, 93.3, HardwareGated="excluded")], {CRYPTO: CRYPTO_FLOORS})

        self.assertEqual(0, run.returncode, run.output)
        self.assertEqual({"line": 97.0, "branch": 92.8}, run.thresholds[CRYPTO])
        self.assertIn(f"{CRYPTO} line: 96.9% -> 97% (measured 97.5%)", run.output)
        self.assertIn(f"{CRYPTO} branch: 92% -> 92.8% (measured 93.3%)", run.output)
        self.assertNotIn(NO_VERDICT_WARNING, run.output)

    def test_floor_when_row_has_no_gated_files_should_be_raised_without_a_warning(self) -> None:
        run = run_script([row("Bodu.Sample", 95.27, 91.0, HardwareGated=None)], {"Bodu.Sample": {"line": 90.0, "branch": 85.0}})

        self.assertEqual(0, run.returncode, run.output)
        self.assertEqual({"line": 94.8, "branch": 90.5}, run.thresholds["Bodu.Sample"])
        self.assertNotIn(NO_VERDICT_WARNING, run.output)

    def test_floor_when_rows_record_no_verdict_should_be_raised_with_one_warning_that_the_rule_is_applied_by_hand(self) -> None:
        rows = [row(CRYPTO, 97.5, 93.3), row("Bodu.Sample", 95.27, 91.0)]
        thresholds = {CRYPTO: CRYPTO_FLOORS, "Bodu.Sample": {"line": 90.0, "branch": 85.0}}

        run = run_script(rows, thresholds)

        self.assertEqual(0, run.returncode, run.output)
        self.assertEqual({"line": 97.0, "branch": 92.8}, run.thresholds[CRYPTO])
        self.assertEqual({"line": 94.8, "branch": 90.5}, run.thresholds["Bodu.Sample"])
        self.assertEqual(1, run.output.count(NO_VERDICT_WARNING), run.output)
        self.assertIn("applied by hand", run.output)


if __name__ == "__main__":
    unittest.main()
