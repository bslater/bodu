"""Tests for the target-framework and session-limit checks in bld/check-docs.py."""

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


def opening_problems(text: str) -> list[str]:
    return check_docs.framework_opening_problems("page.md", text, EXPECTED)


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


class FrameworkOpeningTests(unittest.TestCase):
    """The .NET versions a page's opening paragraph names, and the parts of a page that come before it."""

    def test_framework_opening_problems_when_paragraph_names_one_version_should_report_its_line(self) -> None:
        found = opening_problems("# Widgets\n\nA widget library for .NET 8. It makes widgets.\n")
        self.assertEqual(1, len(found))
        self.assertTrue(found[0].startswith("page.md:3: the opening paragraph names .NET 8,"), found[0])

    def test_framework_opening_problems_when_paragraph_names_every_version_should_report_nothing(self) -> None:
        self.assertEqual([], opening_problems("# Widgets\n\nA widget library for .NET 8 and .NET 10.\n"))
        self.assertEqual([], opening_problems("# Widgets\n\nA widget library for .NET 10 and .NET 8.\n"))

    def test_framework_opening_problems_when_oldest_version_is_named_as_a_minimum_should_report_nothing(self) -> None:
        self.assertEqual([], opening_problems("# Widgets\n\nA widget library for .NET 8 and later.\n"))
        self.assertEqual([], opening_problems("# Widgets\n\nA widget library for .NET 8 or later.\n"))
        self.assertEqual([], opening_problems("# Widgets\n\nA widget library for .NET 8+.\n"))

    def test_framework_opening_problems_when_newer_version_is_named_as_a_minimum_should_report_it(self) -> None:
        self.assertEqual(1, len(opening_problems("# Widgets\n\nA widget library for .NET 10 and later.\n")))

    def test_framework_opening_problems_when_version_wraps_onto_a_later_line_should_report_that_line(self) -> None:
        found = opening_problems("# Widgets\n\nA widget library\nfor .NET 8.\n")
        self.assertEqual(1, len(found))
        self.assertTrue(found[0].startswith("page.md:4:"), found[0])

    def test_framework_opening_problems_when_version_follows_the_opening_paragraph_should_report_nothing(self) -> None:
        self.assertEqual([], opening_problems("# Widgets\n\nA widget library.\n\nIts loop ran slower on .NET 8.\n"))

    def test_framework_opening_problems_when_front_matter_banner_and_image_come_first_should_read_the_paragraph_after_them(self) -> None:
        page = (
            "---\nuid: Widgets\n---\n\n"
            "![Widgets](~/images/hero-widgets.svg)\n\n"
            "## Purpose\n\n"
            "> **API stability - Stable.** Unchanged since .NET 8.\n\n"
            "**Widgets** is a widget library\nfor .NET 8.\n")
        found = opening_problems(page)
        self.assertEqual(1, len(found))
        self.assertTrue(found[0].startswith("page.md:12:"), found[0])

    def test_framework_opening_problems_when_another_dotnet_product_is_named_should_report_nothing(self) -> None:
        self.assertEqual([], opening_problems("# Widgets\n\nPorted from .NET Framework 4.8 and .NET Standard 2.0.\n"))

    def test_framework_opening_problems_when_page_has_no_prose_should_report_nothing(self) -> None:
        self.assertEqual([], opening_problems("# Widgets\n\n> Moved to Gadgets on .NET 8.\n"))


def description_problems(text: str) -> list[str]:
    return check_docs.framework_description_problems("x.csproj", text, EXPECTED)


def project(description: str) -> str:
    return (
        "<Project Sdk=\"Microsoft.NET.Sdk\">\n\n  <PropertyGroup>\n"
        "    <TargetFrameworks>$(BoduNetTargets)</TargetFrameworks>\n"
        f"    <Description>{description}</Description>\n  </PropertyGroup>\n\n</Project>\n")


class FrameworkDescriptionTests(unittest.TestCase):
    """The .NET versions a project's package description names."""

    def test_framework_description_problems_when_description_names_one_version_should_report_its_line(self) -> None:
        found = description_problems(project("Widgets for .NET 8: a widget and a gadget."))
        self.assertEqual(1, len(found))
        self.assertTrue(found[0].startswith("x.csproj:5: the package description names .NET 8,"), found[0])

    def test_framework_description_problems_when_description_names_every_version_should_report_nothing(self) -> None:
        self.assertEqual([], description_problems(project("Widgets for .NET 8 and .NET 10.")))

    def test_framework_description_problems_when_oldest_version_is_named_as_a_minimum_should_report_nothing(self) -> None:
        self.assertEqual([], description_problems(project("Widgets for .NET 8 and later.")))

    def test_framework_description_problems_when_newer_version_is_named_as_a_minimum_should_report_it(self) -> None:
        self.assertEqual(1, len(description_problems(project("Widgets for .NET 10 and later."))))

    def test_framework_description_problems_when_description_wraps_should_report_the_line_of_the_version(self) -> None:
        found = description_problems(project("Widgets and gadgets\n      for .NET 8."))
        self.assertEqual(1, len(found))
        self.assertTrue(found[0].startswith("x.csproj:6:"), found[0])

    def test_framework_description_problems_when_version_is_outside_the_description_should_report_nothing(self) -> None:
        text = project("Widgets ported from .NET Framework 4.8.").replace("</Project>", "<!-- Built on .NET 8. -->\n</Project>")
        self.assertEqual([], description_problems(text))


class NetTargetsTests(unittest.TestCase):
    """The one reader of $(BoduNetTargets) both documentation tools share."""

    def test_net_targets_when_props_are_read_should_match_the_pipeline_frameworks(self) -> None:
        self.assertEqual(check_docs.net_targets(), dc.bodu_net_targets())
        self.assertIn("net8.0", check_docs.net_targets())



def session_problems(text: str) -> list[str]:
    return check_docs.session_timeout_problems("x.runsettings", text)


def runsettings(comment: str | None, timeout: str) -> str:
    above = f"    <!-- {comment} -->\n" if comment is not None else ""
    return (
        "<RunSettings>\n  <RunConfiguration>\n    <!-- Use all available logical processors -->\n"
        f"    <MaxCpuCount>0</MaxCpuCount>\n\n{above}    <TestSessionTimeout>{timeout}</TestSessionTimeout>\n"
        "  </RunConfiguration>\n</RunSettings>\n")


class SessionTimeoutTests(unittest.TestCase):
    """The limit session_timeout_problems reads from a runsettings comment, and the value it holds to it."""

    def test_session_timeout_problems_when_value_is_the_stated_limit_should_report_nothing(self) -> None:
        text = runsettings("Kill the test session if it runs longer than 10 minutes per assembly", "600000")
        self.assertEqual([], session_problems(text))

    def test_session_timeout_problems_when_value_has_one_zero_too_many_should_report_its_line(self) -> None:
        found = session_problems(runsettings("Kill the test session if it runs longer than 20 minutes per assembly", "12000000"))
        self.assertEqual(1, len(found))
        self.assertTrue(found[0].startswith("x.runsettings:7: <TestSessionTimeout> is 12000000 ms (200 minutes)"), found[0])
        self.assertIn("states 20 minutes per assembly (1200000 ms)", found[0])

    def test_session_timeout_problems_when_comment_names_another_limit_in_passing_should_read_its_own(self) -> None:
        comment = ("Stress loops run for tens of minutes, so the 10-minute per-session guard used by the other tiers does "
                   "not apply. Allow up to 60 minutes per assembly.")
        self.assertEqual([], session_problems(runsettings(comment, "3600000")))

    def test_session_timeout_problems_when_comment_states_no_limit_should_report_it(self) -> None:
        found = session_problems(runsettings("Kill the test session if it runs too long", "600000"))
        self.assertEqual(1, len(found))
        self.assertTrue(found[0].startswith("x.runsettings:7: the comment above <TestSessionTimeout> must state"), found[0])

    def test_session_timeout_problems_when_element_has_no_comment_should_report_it(self) -> None:
        found = session_problems(runsettings(None, "600000"))
        self.assertEqual(1, len(found))
        self.assertTrue(found[0].startswith("x.runsettings:6: the comment above <TestSessionTimeout> must state"), found[0])

    def test_session_timeout_problems_when_limit_is_stated_in_an_earlier_comment_should_report_it(self) -> None:
        text = runsettings("Kill the test session if it runs too long", "600000").replace(
            "Use all available logical processors", "Run each of 10 minutes per assembly")
        self.assertEqual(1, len(session_problems(text)))

    def test_session_timeout_problems_when_comment_states_two_limits_should_report_it(self) -> None:
        text = runsettings("Allow 10 minutes per assembly here, and 20 minutes per assembly in CI", "600000")
        self.assertEqual(1, len(session_problems(text)))

    def test_session_timeout_problems_when_file_sets_no_limit_should_report_nothing(self) -> None:
        self.assertEqual([], session_problems("<RunSettings>\n  <RunConfiguration>\n  </RunConfiguration>\n</RunSettings>\n"))


if __name__ == "__main__":
    unittest.main()
