"""Tests for the target-framework, session-limit and spelling checks in bld/check-docs.py."""

from __future__ import annotations

import os
import sys
import tempfile
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



# The word pairs the spelling tests read, in the data file's grammar.
SPELLING_PAIRS_TEXT = (
    "# British -> American, one inflection per line\n"
    "behaviour -> behavior\n"
    "behaviours -> behaviors\n"
    "\n"
    "initialise -> initialize\n"
    "initialised -> initialized   # an inflection is its own entry\n"
    "catalogue -> catalog\n"
    "flavoured -> flavored\n"
    "cancelled -> canceled\n")

SPELLING_PAIRS = {
    "behaviour": "behavior",
    "behaviours": "behaviors",
    "initialise": "initialize",
    "initialised": "initialized",
    "catalogue": "catalog",
    "flavoured": "flavored",
    "cancelled": "canceled",
}


def findings(*lines: str) -> list[tuple[int, str, str]]:
    return check_docs.spelling_findings("\n".join(lines) + "\n", SPELLING_PAIRS)


class SpellingFindingsTests(unittest.TestCase):
    """The British spellings spelling_findings reads in XML documentation, and the text it leaves alone."""

    def test_spelling_findings_when_doc_line_has_british_word_should_report_line_word_and_replacement(self) -> None:
        """Verifies that a British word in a documentation line is reported with its line, as written, and its US form."""
        found = findings(
            "namespace Widgets;",
            "",
            "/// <summary>",
            "/// Describes the widget's behaviour when it is initialised.",
            "/// </summary>")
        self.assertEqual([(4, "behaviour", "behavior"), (4, "initialised", "initialized")], found)

    def test_spelling_findings_when_word_is_capitalized_should_match_and_keep_the_case(self) -> None:
        """Verifies that the match ignores case and that the replacement keeps the case of the word found."""
        self.assertEqual(
            [(1, "Behaviours", "Behaviors"), (2, "CATALOGUE", "CATALOG")],
            findings("/// Behaviours differ.", "/// See the CATALOGUE."))

    def test_spelling_findings_when_word_is_part_of_a_longer_word_should_report_nothing(self) -> None:
        """Verifies that only whole words match, so a longer word or an identifier containing one is not reported."""
        self.assertEqual([], findings(
            "/// Misbehaviours, catalogues and the CatalogueEntry type are not on the list.",
            "/// Neither is reinitialised or behaviour_mode or behaviour2."))

    def test_spelling_findings_when_word_is_hyphenated_or_possessive_should_report_it(self) -> None:
        """Verifies that a hyphen or an apostrophe bounds a word, so a compound or a possessive is still read."""
        self.assertEqual(
            [(1, "flavoured", "flavored"), (2, "behaviour", "behavior")],
            findings("/// Quartz-flavoured cron.", "/// The reader's behaviour's cost."))

    def test_spelling_findings_when_only_some_inflections_are_listed_should_report_only_those(self) -> None:
        """Verifies that each inflection is an entry of its own, so an inflection that is not listed is not reported."""
        self.assertEqual([(1, "initialise", "initialize")], findings("/// Call initialise; it initialises the state."))

    def test_spelling_findings_when_word_is_not_in_a_doc_comment_should_report_nothing(self) -> None:
        """Verifies that code, string literals, identifiers, ordinary comments and four-slash comments are not read."""
        self.assertEqual([], findings(
            "var behaviour = Initialise(\"catalogue\");",
            "// The behaviour of this loop is initialised elsewhere.",
            "//// behaviour",
            "/* catalogue */",
            "string s = \"/// behaviour\";"))

    def test_spelling_findings_when_word_is_inline_code_or_a_code_block_should_report_nothing(self) -> None:
        """Verifies that text inside <c> and <code> elements, including a code block over several lines, is not read."""
        self.assertEqual([], findings(
            "/// Reads the <c># catalogue: ...</c> line.",
            "/// <example>",
            "/// <code><![CDATA[",
            "/// var behaviour = settings[\"behaviour\"];",
            "/// ]]></code>",
            "/// </example>"))

    def test_spelling_findings_when_code_block_ends_should_read_the_prose_after_it(self) -> None:
        """Verifies that prose after a code block is read again, on its own line."""
        self.assertEqual([(4, "behaviour", "behavior")], findings(
            "/// <code>",
            "/// behaviour = 1;",
            "/// </code>",
            "/// The behaviour shown above."))

    def test_spelling_findings_when_word_is_in_a_tag_attribute_or_url_should_report_nothing(self) -> None:
        """Verifies that tag names, attribute values such as cref and href, and bare URLs are not read."""
        self.assertEqual([], findings(
            "/// <see cref=\"Catalogue.Behaviour\" /> and <paramref name=\"initialised\" />.",
            "/// <a href=\"https://reveng.sourceforge.io/crc-catalogue/all.htm\">CRC RevEng list</a>,",
            "/// mirrored at https://example.org/catalogue/behaviour."))

    def test_spelling_findings_when_link_text_is_british_should_report_it(self) -> None:
        """Verifies that the text of a link is prose and is read, while its href is not."""
        self.assertEqual([(1, "catalogue", "catalog")], findings(
            "/// <a href=\"https://reveng.sourceforge.io/crc-catalogue/all.htm\">CRC RevEng catalogue</a>"))

    def test_spelling_findings_when_unclosed_code_block_ends_its_comment_should_read_the_next_comment(self) -> None:
        """Verifies that a code block left open does not run past the end of its documentation comment."""
        self.assertEqual([(4, "behaviour", "behavior")], findings(
            "/// <code>",
            "/// x = 1;",
            "int x;",
            "/// The behaviour."))


class SpellingPairsTests(unittest.TestCase):
    """The grammar of bld/docs-checks/us-spelling.txt, and the list it holds."""

    def test_parse_spelling_pairs_when_text_is_well_formed_should_map_each_british_word_to_its_us_form(self) -> None:
        """Verifies that each 'british -> american' line is read, and that comments and blank lines are skipped."""
        pairs, problems = check_docs.parse_spelling_pairs(SPELLING_PAIRS_TEXT)
        self.assertEqual([], problems)
        self.assertEqual(SPELLING_PAIRS, pairs)

    def test_parse_spelling_pairs_when_line_is_malformed_should_report_its_line(self) -> None:
        """Verifies that a line without an arrow, a duplicate British word, and a pair that changes nothing are reported."""
        _, problems = check_docs.parse_spelling_pairs(
            "behaviour behavior\ncolour -> color\ncolour -> colour\nhonour -> honor\nhonor -> honor\n")
        self.assertEqual(3, len(problems), problems)
        self.assertTrue(problems[0].startswith("us-spelling.txt:1:"), problems[0])
        self.assertTrue(problems[1].startswith("us-spelling.txt:3:"), problems[1])
        self.assertTrue(problems[2].startswith("us-spelling.txt:5:"), problems[2])

    def test_parse_spelling_pairs_when_repository_list_is_read_should_hold_the_agreed_pairs(self) -> None:
        """Verifies that the repository's list parses cleanly and holds the agreed US forms, cancelled as the BCL spells it."""
        pairs, problems = check_docs.parse_spelling_pairs(check_docs.read(os.path.join(check_docs.CHECKS, "us-spelling.txt")))
        self.assertEqual([], problems)
        for british, american in (
                ("behaviour", "behavior"), ("behaviours", "behaviors"), ("behavioural", "behavioral"),
                ("initialise", "initialize"), ("initialised", "initialized"), ("normalisation", "normalization"),
                ("analyse", "analyze"), ("colour", "color"), ("artefact", "artifact"), ("labelled", "labeled"),
                ("modelling", "modeling"), ("cancelled", "canceled"), ("cancelling", "canceling"),
                ("licence", "license"), ("defence", "defense"), ("centre", "center"), ("catalogue", "catalog"),
                ("analogue", "analog"), ("grey", "gray"), ("programme", "program"), ("whilst", "while"),
                ("judgement", "judgment"), ("acknowledgement", "acknowledgment"), ("fulfil", "fulfill")):
            with self.subTest(british=british):
                self.assertEqual(american, pairs.get(british))

    def test_parse_spelling_pairs_when_repository_list_is_read_should_hold_no_us_word(self) -> None:
        """Verifies that the list holds no word US English shares, so an -ise or -ence ending alone never flags a word."""
        pairs, _ = check_docs.parse_spelling_pairs(check_docs.read(os.path.join(check_docs.CHECKS, "us-spelling.txt")))
        for word in (
                "otherwise", "precise", "exercise", "promise", "noise", "advise", "devise", "comprise", "compromise",
                "surprise", "expertise", "enterprise", "premise", "revise", "supervise", "emphasis", "analysis",
                "analyses", "cancellation", "license", "practice", "prologue"):
            with self.subTest(word=word):
                self.assertNotIn(word, pairs)


class SpellingAllowlistTests(unittest.TestCase):
    """The grammar of bld/docs-checks/us-spelling-allowlist.txt."""

    def test_parse_spelling_allowlist_when_entries_are_well_formed_should_key_them_by_path_and_line(self) -> None:
        """Verifies that an entry names a file, or a line of one, followed by its reason, and that comments are skipped."""
        entries, problems = check_docs.parse_spelling_allowlist(
            "# path[:line]  reason\n"
            "Widgets/src/Widget.cs:12  Quotes RFC 9999 verbatim.\n"
            "\n"
            "Widgets/test/Quoted Widget.cs  The whole file is a third-party corpus.\n")
        self.assertEqual([], problems)
        self.assertEqual({
            ("Widgets/src/Widget.cs", 12): "Quotes RFC 9999 verbatim.",
            ("Widgets/test/Quoted Widget.cs", None): "The whole file is a third-party corpus.",
        }, entries)

    def test_parse_spelling_allowlist_when_entry_has_no_reason_should_report_its_line(self) -> None:
        """Verifies that an entry without a reason is reported rather than excusing anything."""
        entries, problems = check_docs.parse_spelling_allowlist("# header\nWidgets/src/Widget.cs:12\n")
        self.assertEqual({}, entries)
        self.assertEqual(1, len(problems), problems)
        self.assertTrue(problems[0].startswith("us-spelling-allowlist.txt:2:"), problems[0])


class GeneratedSourceTests(unittest.TestCase):
    """The C# sources is_generated_source treats as generated, which the spelling check leaves alone."""

    def test_is_generated_source_when_file_is_a_resource_designer_should_return_true(self) -> None:
        """Verifies that a *.Designer.cs file is generated whatever its text."""
        self.assertTrue(check_docs.is_generated_source("WidgetResourceStrings.Designer.cs", "/// behaviour\n"))

    def test_is_generated_source_when_banner_follows_the_copyright_header_should_return_true(self) -> None:
        """Verifies that an <auto-generated> banner among the comments opening a file marks it generated."""
        text = (
            "﻿// ----\n// <copyright file=\"Widget.cs\" company=\"Bodu Pty. Ltd.\">\n// </copyright>\n// ----\n\n"
            "// ----\n// <auto-generated />\n// ----\n\nnamespace Widgets;\n")
        self.assertTrue(check_docs.is_generated_source("Widget.cs", text))

    def test_is_generated_source_when_banner_comes_after_code_should_return_false(self) -> None:
        """Verifies that <auto-generated> written after the opening comments, as a generator's template would, is not a banner."""
        text = "// <copyright file=\"Emitter.cs\" />\n\nnamespace Widgets;\n\n// Emits \"// <auto-generated />\".\n"
        self.assertFalse(check_docs.is_generated_source("Emitter.cs", text))


SPELLING_SOURCE = "// <copyright />\n\nnamespace Widgets;\n\n/// <summary>Sets the widget's behaviour.</summary>\npublic sealed class Widget { }\n"


def write_tree(root: str, files: dict[str, str]) -> None:
    for relative, text in files.items():
        path = os.path.join(root, *relative.split("/"))
        os.makedirs(os.path.dirname(path), exist_ok=True)
        with open(path, "w", encoding="utf-8") as stream:
            stream.write(text)


class CheckSpellingTests(unittest.TestCase):
    """The sources check_spelling reads, the data files it reads them against, and how it reports what it finds."""

    def setUp(self) -> None:
        self._directory = tempfile.TemporaryDirectory()
        self.addCleanup(self._directory.cleanup)
        self.root = self._directory.name
        self.checks = os.path.join(self.root, "bld", "docs-checks")
        write_tree(self.checks, {"us-spelling.txt": SPELLING_PAIRS_TEXT, "us-spelling-allowlist.txt": "# none\n"})

    def check(self) -> list[str]:
        return check_docs.check_spelling(self.root, self.checks)

    def test_check_spelling_when_project_source_has_british_word_should_report_file_line_word_and_replacement(self) -> None:
        """Verifies that a British word in a project's src or test documentation is reported with its file, line, word and US form."""
        write_tree(self.root, {"Widgets/src/Widget.cs": SPELLING_SOURCE, "Widgets/test/WidgetTests.cs": SPELLING_SOURCE})
        found = self.check()
        self.assertEqual(2, len(found), found)
        self.assertTrue(found[0].startswith("Widgets/src/Widget.cs:5:"), found[0])
        self.assertIn("'behaviour'", found[0])
        self.assertIn("'behavior'", found[0])
        self.assertTrue(found[1].startswith("Widgets/test/WidgetTests.cs:5:"), found[1])

    def test_check_spelling_when_source_is_nested_or_shared_should_report_it(self) -> None:
        """Verifies that a src folder below a grouping folder, and shared source compiled into projects, are read."""
        write_tree(self.root, {
            "Widgets.Data/Widgets.Data.Europe/src/Data.cs": SPELLING_SOURCE,
            "Widgets/shared/Shared.cs": SPELLING_SOURCE,
        })
        self.assertEqual(2, len(self.check()))

    def test_check_spelling_when_source_is_outside_the_documented_trees_should_report_nothing(self) -> None:
        """Verifies that samples, build output, generated sources and the separate Bodu.CodeStyle solution are not read."""
        write_tree(self.root, {
            "samples/Widgets/Program.cs": SPELLING_SOURCE,
            "Widgets/src/obj/Release/Widget.cs": SPELLING_SOURCE,
            "Widgets/src/bin/Widget.cs": SPELLING_SOURCE,
            "Widgets/src/WidgetResourceStrings.Designer.cs": SPELLING_SOURCE,
            "Widgets/src/Generated.cs": "// <auto-generated />\n" + SPELLING_SOURCE,
            "Bodu.CodeStyle/Analyzers/src/Analyzer.cs": SPELLING_SOURCE,
            "Widgets/src/Widget.md": "/// behaviour\n",
        })
        self.assertEqual([], self.check())

    def test_check_spelling_when_allowlist_names_the_line_or_file_should_report_nothing(self) -> None:
        """Verifies that an allowlisted line, and every line of an allowlisted file, are excused."""
        write_tree(self.root, {
            "Widgets/src/Widget.cs": SPELLING_SOURCE,
            "Widgets/test/Quoted.cs": SPELLING_SOURCE,
            "bld/docs-checks/us-spelling-allowlist.txt":
                "Widgets/src/Widget.cs:5  Quotes a third-party message verbatim.\n"
                "Widgets/test/Quoted.cs  Restates a third-party test suite.\n",
        })
        self.assertEqual([], self.check())

    def test_check_spelling_when_allowlist_entry_excuses_nothing_should_report_it(self) -> None:
        """Verifies that an allowlist entry whose line no longer has a British word, or whose file is gone, is reported as stale."""
        write_tree(self.root, {
            "Widgets/src/Widget.cs": SPELLING_SOURCE,
            "bld/docs-checks/us-spelling-allowlist.txt":
                "Widgets/src/Widget.cs:5  Quotes a third-party message verbatim.\n"
                "Widgets/src/Widget.cs:6  Moved.\n"
                "Widgets/src/Gone.cs  Deleted.\n",
        })
        found = self.check()
        self.assertEqual(2, len(found), found)
        self.assertTrue(all(line.startswith("us-spelling-allowlist.txt:") for line in found), found)
        self.assertIn("Widgets/src/Widget.cs:6", found[0])
        self.assertIn("Widgets/src/Gone.cs", found[1])

    def test_check_spelling_when_word_list_is_missing_should_report_it(self) -> None:
        """Verifies that the check fails, rather than passing vacuously, when its word list is missing."""
        os.remove(os.path.join(self.checks, "us-spelling.txt"))
        found = self.check()
        self.assertEqual(1, len(found), found)
        self.assertIn("us-spelling.txt", found[0])

    def test_check_spelling_when_check_is_named_on_the_command_line_should_run_it(self) -> None:
        """Verifies that 'spelling' is a check the command line runs, and that 'all' includes it."""
        self.assertIn("spelling", check_docs.CHECK_NAMES)


if __name__ == "__main__":
    unittest.main()
