"""Tests for bld/docs/docfx_yaml.py: DocFX's YAML 1.2 scalars must survive a load and dump unchanged."""

from __future__ import annotations

import os
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import docfx_yaml as dy  # noqa: E402

# Lines DocFX writes, taken from real metadata: the VB name of operator == (a plain "=", which YAML 1.1
# reads as its "value" tag), a quoted enum member named Null, an empty VB name, and a generic type
# parameter reference.
DOCFX_SAMPLE = """\
### YamlMime:ManagedReference
items:
- uid: Bodu.Formats.Excel.ExcelErrorCode.Null
  id: "Null"
  name.vb: =
  nameWithType.vb: ''
  isExternal: true
  value: 12
  type: '{T}'
  summary: >-
    First line

    second line.
"""


class DocfxYamlTests(unittest.TestCase):
    """The loader and dumper agree with YamlDotNet's YAML 1.2 core schema."""

    def test_load_when_docfx_sample_should_keep_every_scalar_type(self) -> None:
        """Verifies that "=", "Null", "" and "{T}" load as strings, and true and 12 as bool and int."""
        (item,) = dy.load(DOCFX_SAMPLE)["items"]
        self.assertEqual(item["name.vb"], "=")
        self.assertEqual(item["id"], "Null")
        self.assertEqual(item["nameWithType.vb"], "")
        self.assertEqual(item["type"], "{T}")
        self.assertIs(item["isExternal"], True)
        self.assertEqual(item["value"], 12)

    def test_load_when_yaml11_only_forms_should_read_strings(self) -> None:
        """Verifies that YAML 1.1 booleans and octal-looking integers keep their YAML 1.2 meaning."""
        value = dy.load("a: On\nb: yes\nc: 012\nd: ~\ne: Null\n")
        self.assertEqual(value, {"a": "On", "b": "yes", "c": 12, "d": None, "e": None})

    def test_dump_when_string_would_retype_should_quote_it(self) -> None:
        """Verifies that strings the 1.2 schema would read as null, bool or number are quoted, and others are not."""
        text = dy.dump({"a": "Null", "b": "true", "c": "012", "d": "=", "e": "On"})
        self.assertEqual(text, "a: 'Null'\nb: 'true'\nc: '012'\nd: =\ne: On\n")

    def test_dump_when_value_is_shared_should_not_write_aliases(self) -> None:
        """Verifies that a list shared by two keys is written in full twice, never as an anchor and alias."""
        shared = ["net8.0", "net10.0"]
        self.assertNotIn("&", dy.dump({"a": shared, "b": shared}))

    def test_round_trip_when_docfx_sample_should_reproduce_the_data(self) -> None:
        """Verifies that loading the dump of a document gives back the same data, header included."""
        data = dy.load(DOCFX_SAMPLE)
        text = dy.dump(data, dy.MANAGED_REFERENCE_HEADER)
        self.assertTrue(text.startswith(dy.MANAGED_REFERENCE_HEADER + "\n"))
        self.assertEqual(dy.load(text), data)


if __name__ == "__main__":
    unittest.main()
