"""Tests for the target-framework check in bld/check-docs.py."""

from __future__ import annotations

import os
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import docs_common as dc  # noqa: E402

check_docs = dc.check_docs()

EXPECTED = ["net8.0", "net10.0"]


def problems(text: str) -> list[str]:
    return check_docs.framework_claim_problems("page.md", text, EXPECTED)


class FrameworkClaimTests(unittest.TestCase):
    """The claims framework_claim_problems reads, and the ones it leaves alone."""

    def test_framework_claim_problems_when_sentence_names_every_framework_should_report_nothing(self) -> None:
        self.assertEqual([], problems("Targets `net8.0` and `net10.0`. Depends on `Bodu.Core`.\n"))

    def test_framework_claim_problems_when_sentence_names_one_framework_should_report_its_line(self) -> None:
        found = problems("# Install\n\nTargets `net8.0`. Depends on `Bodu.Core`.\n")
        self.assertEqual(1, len(found))
        self.assertTrue(found[0].startswith("page.md:3: names net8.0 as the target frameworks"), found[0])

    def test_framework_claim_problems_when_claim_wraps_onto_the_next_line_should_read_the_whole_list(self) -> None:
        self.assertEqual([], problems("All packages target\n`net8.0` and `net10.0`.\n"))
        found = problems("All packages target\n`net8.0`.\n")
        self.assertEqual(1, len(found))
        self.assertTrue(found[0].startswith("page.md:2:"), found[0])

    def test_framework_claim_problems_when_bold_label_names_one_framework_should_report_it(self) -> None:
        self.assertEqual(1, len(problems("- **Target framework.** `net8.0`.\n")))
        self.assertEqual([], problems("- **Target frameworks.** `net8.0` and `net10.0`.\n"))

    def test_framework_claim_problems_when_verb_has_another_subject_should_read_the_claim(self) -> None:
        self.assertEqual(1, len(problems("Both target `net8.0` and share one namespace.\n")))
        self.assertEqual(1, len(problems("It targets `net8.0`.\n")))

    def test_framework_claim_problems_when_frameworks_are_listed_in_another_order_should_report_nothing(self) -> None:
        self.assertEqual([], problems("Targets `net10.0` and `net8.0`.\n"))
        self.assertEqual([], problems("Targets `net8.0`, `net10.0`.\n"))

    def test_framework_claim_problems_when_netstandard_stands_alone_or_beside_them_should_report_nothing(self) -> None:
        self.assertEqual([], problems("The task package targets `netstandard2.0` so it loads in any MSBuild host.\n"))
        self.assertEqual([], problems("Targets `net8.0`, `net10.0` and `netstandard2.0`.\n"))

    def test_framework_claim_problems_when_column_cell_names_one_framework_should_report_its_row(self) -> None:
        table = (
            "| Package | What it provides | Target frameworks |\n"
            "|---|---|---|\n"
            "| A | Widgets | `net8.0`, `net10.0` |\n"
            "| B | Gadgets | `net8.0` |\n"
            "\n"
            "| Package | Notes |\n"
            "|---|---|\n"
            "| C | Ships `net8.0` assets |\n")
        found = problems(table)
        self.assertEqual(1, len(found))
        self.assertTrue(found[0].startswith("page.md:4:"), found[0])

    def test_framework_claim_problems_when_moniker_is_not_a_claim_should_report_nothing(self) -> None:
        self.assertEqual([], problems("A consumer on .NET 9 resolved the `net8.0` asset.\n"))
        self.assertEqual([], problems("While the package targeted `net8.0` alone, the call was ambiguous.\n"))
        self.assertEqual([], problems("Every framework this package targets. Then `net8.0` comes up.\n"))

    def test_framework_claim_problems_when_claim_is_inside_a_code_block_should_report_nothing(self) -> None:
        self.assertEqual([], problems("```text\nTargets `net8.0`.\n```\n"))


class NetTargetsTests(unittest.TestCase):
    """The one reader of $(BoduNetTargets) both documentation tools share."""

    def test_net_targets_when_props_are_read_should_match_the_pipeline_frameworks(self) -> None:
        self.assertEqual(check_docs.net_targets(), dc.bodu_net_targets())
        self.assertIn("net8.0", check_docs.net_targets())


if __name__ == "__main__":
    unittest.main()
