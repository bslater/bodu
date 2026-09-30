"""Tests for the pure helpers in bld/docs/validate_api_site.py."""

from __future__ import annotations

import os
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import docs_common as dc  # noqa: E402
import validate_api_site as v  # noqa: E402

LABELS = {tfm: dc.parse_framework(tfm) for tfm in ("net8.0", "net10.0")}


class ValidateHelperTests(unittest.TestCase):
    """The helpers the site checks are built on."""

    def test_expected_applies_to_when_both_frameworks_should_list_versions_oldest_first(self) -> None:
        """Verifies that an API in .NET 8 and .NET 10 expects one '.NET | 8, 10' row."""
        self.assertEqual(v.expected_applies_to(["net8.0", "net10.0"], LABELS), [(".NET", "8, 10")])

    def test_expected_applies_to_when_one_framework_should_list_only_it(self) -> None:
        """Verifies that a .NET 10-only API expects a '.NET | 10' row."""
        self.assertEqual(v.expected_applies_to(["net10.0"], LABELS), [(".NET", "10")])

    def test_toc_uids_when_nested_should_collect_every_level(self) -> None:
        """Verifies that UIDs are collected from every depth of a nested table of contents."""
        toc = [{"uid": "Bodu", "items": [{"uid": "Bodu.A", "items": [{"uid": "Bodu.A.T"}]}, {"name": "no uid"}]}]
        self.assertEqual(v.toc_uids(toc), {"Bodu", "Bodu.A", "Bodu.A.T"})

    def test_generated_source_when_link_leads_to_generated_code_should_match(self) -> None:
        """Verifies that links into obj/, *.Designer.cs, and *.g.cs are recognised as generated code."""
        for href in (
            "https://github.com/o/r/blob/sha/P/src/obj/Release/net8.0/X.cs",
            "https://github.com/o/r/blob/sha/P/src/ResourceStrings.Designer.cs",
            "https://github.com/o/r/blob/sha/P/src/Gen.g.cs#L3",
        ):
            with self.subTest(href=href):
                self.assertRegex(href, v.GENERATED_SOURCE)

    def test_generated_source_when_link_leads_to_source_should_not_match(self) -> None:
        """Verifies that an ordinary source file is not mistaken for generated code."""
        self.assertNotRegex("https://github.com/o/r/blob/sha/P/src/Designer/Widget.cs", v.GENERATED_SOURCE)


if __name__ == "__main__":
    unittest.main()
