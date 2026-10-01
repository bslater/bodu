"""Tests for bld/docs/merge_framework_metadata.py.

The fixtures are miniature ManagedReference documents shaped like DocFX's output: a namespace page
and type pages whose items carry the fields the merge reads (uid, type, children, syntax, assemblies,
commentId). Run with ``python3 -m unittest discover -s bld/docs/tests``.
"""

from __future__ import annotations

import os
import sys
import tempfile
import unittest
from typing import Any

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import docs_common as dc  # noqa: E402
import merge_framework_metadata as m  # noqa: E402

NET8 = "net8.0"
NET10 = "net10.0"
NEWEST_FIRST = [NET10, NET8]

PACKAGES = {
    "Bodu.Alpha": {"id": "Bodu.Alpha", "version": "1.0.0", "published": True},
    "Bodu.Beta": {"id": "Bodu.Beta", "version": "0.7.1-preview.3", "published": False},
}


def type_item(uid: str, children: list[str], assembly: str = "Bodu.Alpha", syntax: str | None = None, kind: str = "Class") -> dict[str, Any]:
    """Builds a type item."""
    return {
        "uid": uid,
        "commentId": f"T:{uid}",
        "children": children,
        "type": kind,
        "assemblies": [assembly],
        "syntax": {"content": syntax or f"public class {uid.rsplit('.', 1)[-1]}"},
        "summary": f"Summary of {uid}.",
    }


def member_item(uid: str, assembly: str = "Bodu.Alpha", syntax: str | None = None, return_type: str | None = None) -> dict[str, Any]:
    """Builds a method item."""
    item: dict[str, Any] = {
        "uid": uid,
        "commentId": f"M:{uid}",
        "type": "Method",
        "assemblies": [assembly],
        "syntax": {"content": syntax or f"public void {uid.rsplit('.', 1)[-1]}()"},
    }
    if return_type:
        item["syntax"]["return"] = {"type": return_type}
    return item


def namespace_page(uid: str, children: list[str]) -> dict[str, Any]:
    """Builds a namespace page."""
    return {"items": [{"uid": uid, "commentId": f"N:{uid}", "children": children, "type": "Namespace", "assemblies": ["Bodu.Alpha"]}], "references": [{"uid": c, "name": c} for c in children]}


def type_page(item: dict[str, Any], members: list[dict[str, Any]]) -> dict[str, Any]:
    """Builds a type page holding the type and its members."""
    return {"items": [item, *members], "references": [{"uid": "System.Object", "isExternal": True}]}


def toc(*namespaces: tuple[str, list[str]]) -> dict[str, Any]:
    """Builds a nested table of contents."""
    return {"items": [{"uid": ns, "name": ns, "type": "Namespace", "items": [{"uid": t, "name": t, "type": "Class"} for t in types]} for ns, types in namespaces]}


def build_inputs() -> tuple[dict[str, dict[str, dict[str, Any]]], dict[str, dict[str, Any]], dict[str, dict[str, str]]]:
    """Builds two frameworks' metadata with every applicability case:

    * ``Bodu.Ns.Shared`` exists in both, with ``Both()`` in both, ``Old()`` only in .NET 8, and
      ``New()`` only in .NET 10;
    * ``Bodu.Ns.NewOnly`` (from the preview package) exists only in .NET 10;
    * ``Bodu.Ns.OldOnly`` exists only in .NET 8.
    """
    shared8 = type_item("Bodu.Ns.Shared", ["Bodu.Ns.Shared.Both", "Bodu.Ns.Shared.Old"])
    shared10 = type_item("Bodu.Ns.Shared", ["Bodu.Ns.Shared.Both", "Bodu.Ns.Shared.New"])
    pages = {
        NET8: {
            "Bodu.Ns.yml": namespace_page("Bodu.Ns", ["Bodu.Ns.OldOnly", "Bodu.Ns.Shared"]),
            "Bodu.Ns.Shared.yml": type_page(shared8, [member_item("Bodu.Ns.Shared.Both"), member_item("Bodu.Ns.Shared.Old")]),
            "Bodu.Ns.OldOnly.yml": type_page(type_item("Bodu.Ns.OldOnly", []), []),
        },
        NET10: {
            "Bodu.Ns.yml": namespace_page("Bodu.Ns", ["Bodu.Ns.NewOnly", "Bodu.Ns.Shared"]),
            "Bodu.Ns.Shared.yml": type_page(shared10, [member_item("Bodu.Ns.Shared.Both"), member_item("Bodu.Ns.Shared.New")]),
            "Bodu.Ns.NewOnly.yml": type_page(type_item("Bodu.Ns.NewOnly", [], assembly="Bodu.Beta"), []),
        },
    }
    tocs = {
        NET8: toc(("Bodu.Ns", ["Bodu.Ns.OldOnly", "Bodu.Ns.Shared"])),
        NET10: toc(("Bodu.Ns", ["Bodu.Ns.NewOnly", "Bodu.Ns.Shared"])),
    }
    manifests = {
        NET8: {"Bodu.Ns": "Bodu.Ns.yml", "Bodu.Ns.OldOnly": "Bodu.Ns.OldOnly.yml"},
        NET10: {"Bodu.Ns": "Bodu.Ns.yml", "Bodu.Ns.NewOnly": "Bodu.Ns.NewOnly.yml"},
    }
    return pages, tocs, manifests


def items_by_uid(result: m.MergeResult) -> dict[str, dict[str, Any]]:
    """Indexes every merged item by UID."""
    return {item["uid"]: item for page in result.pages.values() for item in page["items"]}


class UnionOrderTests(unittest.TestCase):
    """union_order keeps the first sequence's order and slots later additions after their predecessor."""

    def test_union_order_when_later_sequence_adds_keys_should_insert_after_predecessor(self) -> None:
        """Verifies that a key only a later sequence has lands immediately after the key preceding it there."""
        self.assertEqual(m.union_order([["a", "c", "e"], ["a", "b", "c", "d", "e"]]), ["a", "b", "c", "d", "e"])

    def test_union_order_when_addition_has_no_predecessor_should_insert_before_successor(self) -> None:
        """Verifies that a key with nothing preceding it in its sequence is placed just before the key that follows it."""
        self.assertEqual(m.union_order([["b", "c"], ["a", "b"]]), ["a", "b", "c"])
        self.assertEqual(m.union_order([["a", "c"], ["b", "c"]]), ["a", "b", "c"])

    def test_union_order_when_addition_has_neither_neighbour_should_append(self) -> None:
        """Verifies that a key sharing no neighbour with the result is appended."""
        self.assertEqual(m.union_order([["a"], ["z"]]), ["a", "z"])

    def test_union_order_when_sequences_are_identical_should_return_one_copy(self) -> None:
        """Verifies that unioning a sequence with itself returns it unchanged."""
        self.assertEqual(m.union_order([["x", "y"], ["x", "y"]]), ["x", "y"])


class MergeTests(unittest.TestCase):
    """merge() folds per-framework metadata into one framework-annotated set."""

    def setUp(self) -> None:
        pages, tocs, manifests = build_inputs()
        self.result = m.merge(NEWEST_FIRST, pages, tocs, manifests, PACKAGES, {})
        self.items = items_by_uid(self.result)

    def test_merge_when_api_is_in_both_frameworks_should_list_both_oldest_first(self) -> None:
        """Verifies that an API present in both frameworks applies to .NET 8 and .NET 10, in that order."""
        self.assertEqual(self.items["Bodu.Ns.Shared.Both"]["frameworks"], [NET8, NET10])
        self.assertEqual(self.items["Bodu.Ns.Shared"]["frameworks"], [NET8, NET10])

    def test_merge_when_member_is_only_in_older_framework_should_keep_it_for_that_framework(self) -> None:
        """Verifies that a .NET 8-only member is kept, applies only to .NET 8, and stays in its type's children."""
        self.assertEqual(self.items["Bodu.Ns.Shared.Old"]["frameworks"], [NET8])
        self.assertIn("Bodu.Ns.Shared.Old", self.items["Bodu.Ns.Shared"]["children"])

    def test_merge_when_member_is_only_in_newer_framework_should_apply_only_to_it(self) -> None:
        """Verifies that a .NET 10-only member applies only to .NET 10."""
        self.assertEqual(self.items["Bodu.Ns.Shared.New"]["frameworks"], [NET10])

    def test_merge_when_type_children_differ_should_union_them_in_order(self) -> None:
        """Verifies that a type's children are the union of both frameworks', each added after its predecessor."""
        self.assertEqual(self.items["Bodu.Ns.Shared"]["children"], ["Bodu.Ns.Shared.Both", "Bodu.Ns.Shared.Old", "Bodu.Ns.Shared.New"])

    def test_merge_when_pages_exist_in_one_framework_should_keep_each_once(self) -> None:
        """Verifies that single-framework pages are kept, and a shared page is written once."""
        self.assertEqual(sorted(self.result.pages), ["Bodu.Ns.NewOnly.yml", "Bodu.Ns.OldOnly.yml", "Bodu.Ns.Shared.yml", "Bodu.Ns.yml"])
        self.assertEqual(self.items["Bodu.Ns.OldOnly"]["frameworks"], [NET8])
        self.assertEqual(self.items["Bodu.Ns.NewOnly"]["frameworks"], [NET10])

    def test_merge_when_counting_applicability_should_count_every_item_once(self) -> None:
        """Verifies that the applicability counts partition the merged items."""
        self.assertEqual(self.result.applicability[(NET8, NET10)], 3)  # namespace, Shared, Both
        self.assertEqual(self.result.applicability[(NET8,)], 2)  # Old, OldOnly
        self.assertEqual(self.result.applicability[(NET10,)], 2)  # New, NewOnly
        self.assertEqual(sum(self.result.applicability.values()), len(self.items))

    def test_merge_when_type_is_merged_should_attach_its_package(self) -> None:
        """Verifies that each type carries its assembly's package, and members inherit it rather than repeat it."""
        self.assertEqual(self.items["Bodu.Ns.Shared"]["package"], PACKAGES["Bodu.Alpha"])
        self.assertEqual(self.items["Bodu.Ns.NewOnly"]["package"], PACKAGES["Bodu.Beta"])
        self.assertNotIn("package", self.items["Bodu.Ns.Shared.Both"])

    def test_merge_when_namespace_spans_packages_should_list_each_distinct_package(self) -> None:
        """Verifies that a namespace lists the distinct packages of its types, sorted by id, not its first assembly."""
        self.assertEqual(self.items["Bodu.Ns"]["packages"], [PACKAGES["Bodu.Alpha"], PACKAGES["Bodu.Beta"]])

    def test_merge_when_reference_names_documented_api_should_carry_its_frameworks(self) -> None:
        """Verifies that a namespace page's reference to a single-framework type carries that applicability."""
        references = {r["uid"]: r for r in self.result.pages["Bodu.Ns.yml"]["references"]}
        self.assertEqual(references["Bodu.Ns.NewOnly"]["frameworks"], [NET10])
        self.assertEqual(references["Bodu.Ns.OldOnly"]["frameworks"], [NET8])

    def test_merge_when_tocs_differ_should_union_them_by_uid(self) -> None:
        """Verifies that the merged table of contents reaches every type from either framework."""
        (namespace,) = self.result.toc["items"]
        self.assertEqual([node["uid"] for node in namespace["items"]], ["Bodu.Ns.NewOnly", "Bodu.Ns.OldOnly", "Bodu.Ns.Shared"])

    def test_merge_when_manifests_differ_should_union_them(self) -> None:
        """Verifies that the merged UID manifest names the pages of both frameworks."""
        self.assertEqual(self.result.manifest, {"Bodu.Ns": "Bodu.Ns.yml", "Bodu.Ns.NewOnly": "Bodu.Ns.NewOnly.yml", "Bodu.Ns.OldOnly": "Bodu.Ns.OldOnly.yml"})

    def test_merge_when_run_twice_should_produce_identical_output(self) -> None:
        """Verifies that the merge is deterministic."""
        pages, tocs, manifests = build_inputs()
        again = m.merge(NEWEST_FIRST, pages, tocs, manifests, PACKAGES, {})
        self.assertEqual(again.pages, self.result.pages)
        self.assertEqual(again.toc, self.result.toc)

    def test_merge_when_frameworks_agree_should_report_no_divergence(self) -> None:
        """Verifies that APIs identical in both frameworks produce no divergence."""
        self.assertEqual(self.result.divergences, [])

    def test_merge_when_type_assembly_is_not_documented_should_raise(self) -> None:
        """Verifies that a type whose assembly resolves to no documented package is an error, not a silent gap."""
        pages, tocs, manifests = build_inputs()
        pages[NET10]["Bodu.Ns.NewOnly.yml"]["items"][0]["assemblies"] = ["Bodu.Unknown"]
        with self.assertRaises(dc.PipelineError):
            m.merge(NEWEST_FIRST, pages, tocs, manifests, PACKAGES, {})


class DivergenceTests(unittest.TestCase):
    """compare_item() and the allowlist decide which cross-framework differences fail the merge."""

    def merge_with(self, net8_member: dict[str, Any], net10_member: dict[str, Any], documentation: dict[str, dict[str, str]] | None = None) -> m.MergeResult:
        pages = {
            tfm: {"Bodu.Ns.T.yml": type_page(type_item("Bodu.Ns.T", ["Bodu.Ns.T.M"]), [member])}
            for tfm, member in ((NET8, net8_member), (NET10, net10_member))
        }
        return m.merge(NEWEST_FIRST, pages, {NET8: {"items": []}, NET10: {"items": []}}, {}, PACKAGES, documentation or {})

    def test_compare_when_return_type_differs_should_report_signature_divergence(self) -> None:
        """Verifies that a member whose return type differs between frameworks is reported against 'return'."""
        result = self.merge_with(member_item("Bodu.Ns.T.M", return_type="System.Int32"), member_item("Bodu.Ns.T.M", return_type="System.Int64"))
        self.assertEqual([(d.uid, d.field) for d in result.divergences], [("Bodu.Ns.T.M", "return")])

    def test_compare_when_declaration_differs_should_report_syntax_divergence(self) -> None:
        """Verifies that a changed C# declaration is reported against 'syntax'."""
        result = self.merge_with(member_item("Bodu.Ns.T.M", syntax="public void M(int x)"), member_item("Bodu.Ns.T.M", syntax="public void M(long x)"))
        self.assertIn(("Bodu.Ns.T.M", "syntax"), [(d.uid, d.field) for d in result.divergences])

    def test_compare_when_xml_documentation_differs_should_report_documentation_divergence(self) -> None:
        """Verifies that differing compiler-written documentation is reported against 'documentation'."""
        documentation = {f"{NET8}/Bodu.Alpha": {"M:Bodu.Ns.T.M": "<summary>Old.</summary>"}, f"{NET10}/Bodu.Alpha": {"M:Bodu.Ns.T.M": "<summary>New.</summary>"}}
        result = self.merge_with(member_item("Bodu.Ns.T.M"), member_item("Bodu.Ns.T.M"), documentation)
        self.assertEqual([(d.uid, d.field) for d in result.divergences], [("Bodu.Ns.T.M", "documentation")])

    def test_compare_when_only_expanded_summaries_differ_should_report_nothing(self) -> None:
        """Verifies that DocFX-expanded summaries (framework <inheritdoc /> text) are not compared, only the raw XML."""
        documentation = {f"{tfm}/Bodu.Alpha": {"M:Bodu.Ns.T.M": "<inheritdoc />"} for tfm in (NET8, NET10)}
        old, new = member_item("Bodu.Ns.T.M"), member_item("Bodu.Ns.T.M")
        old["summary"], new["summary"] = "Framework text as .NET 8 words it.", "Framework text as .NET 10 words it."
        self.assertEqual(self.merge_with(old, new, documentation).divergences, [])

    def test_compare_when_only_interface_order_differs_should_report_nothing(self) -> None:
        """Verifies that a type's base list is compared as a set, so a reordered interface list is not a divergence."""
        pages = {
            NET8: {"Bodu.Ns.S.yml": type_page(type_item("Bodu.Ns.S", [], syntax="public readonly struct S : IA<S>, IB<S, int>, IC where T : notnull", kind="Struct"), [])},
            NET10: {"Bodu.Ns.S.yml": type_page(type_item("Bodu.Ns.S", [], syntax="public readonly struct S : IC, IA<S>, IB<S, int> where T : notnull", kind="Struct"), [])},
        }
        result = m.merge(NEWEST_FIRST, pages, {NET8: {"items": []}, NET10: {"items": []}}, {}, PACKAGES, {})
        self.assertEqual(result.divergences, [])

    def test_compare_when_framework_interfaces_differ_should_report_nothing(self) -> None:
        """Verifies that only Bodu interfaces are part of a type's compared 'implements' list."""
        old, new = type_item("Bodu.Ns.T", []), type_item("Bodu.Ns.T", [])
        old["implements"], new["implements"] = ["Bodu.IThing", "System.IOld"], ["Bodu.IThing", "System.INew"]
        self.assertEqual(m.compare_item("Bodu.Ns.T", {NET8: old, NET10: new}, {}), [])

    def test_compare_when_bodu_interface_differs_should_report_implements_divergence(self) -> None:
        """Verifies that a Bodu interface implemented in only one framework is a divergence."""
        old, new = type_item("Bodu.Ns.T", []), type_item("Bodu.Ns.T", [])
        old["implements"], new["implements"] = ["Bodu.IThing"], []
        self.assertEqual([d.field for d in m.compare_item("Bodu.Ns.T", {NET8: old, NET10: new}, {})], ["implements"])

    def test_evaluate_when_divergence_is_allowlisted_should_allow_it(self) -> None:
        """Verifies that an allowlisted divergence is allowed, not unexpected."""
        divergence = m.Divergence("Bodu.Ns.T.M", "return", "detail")
        unexpected, allowed, stale = m.evaluate_divergences([divergence], {("Bodu.Ns.T.M", "return"): "Intentional."})
        self.assertEqual((unexpected, allowed, stale), ([], [divergence], []))

    def test_evaluate_when_divergence_is_not_allowlisted_should_report_it_unexpected(self) -> None:
        """Verifies that an unlisted divergence is unexpected."""
        divergence = m.Divergence("Bodu.Ns.T.M", "return", "detail")
        unexpected, _, _ = m.evaluate_divergences([divergence], {})
        self.assertEqual(unexpected, [divergence])

    def test_evaluate_when_allowlist_entry_no_longer_diverges_should_report_it_stale(self) -> None:
        """Verifies that an allowlist entry whose divergence is gone is reported stale."""
        _, _, stale = m.evaluate_divergences([], {("Bodu.Ns.T.M", "syntax"): "Was intentional."})
        self.assertEqual(stale, [("Bodu.Ns.T.M", "syntax")])


class AllowlistTests(unittest.TestCase):
    """read_allowlist() accepts '<uid> <field>  # reason' lines and rejects anything else."""

    def read(self, text: str) -> dict[tuple[str, str], str]:
        with tempfile.NamedTemporaryFile("w", suffix=".txt", delete=False, encoding="utf-8") as stream:
            stream.write(text)
        try:
            return m.read_allowlist(stream.name)
        finally:
            os.unlink(stream.name)

    def test_read_allowlist_when_entry_is_valid_should_parse_it(self) -> None:
        """Verifies that a well-formed entry and comment lines parse."""
        entries = self.read("# header\n\nBodu.Ns.T.M(System.Int32) return  # widened in .NET 10\n")
        self.assertEqual(entries, {("Bodu.Ns.T.M(System.Int32)", "return"): "widened in .NET 10"})

    def test_read_allowlist_when_reason_is_missing_should_raise(self) -> None:
        """Verifies that an entry without a reason is rejected."""
        with self.assertRaises(dc.PipelineError):
            self.read("Bodu.Ns.T.M return\n")

    def test_read_allowlist_when_field_is_unknown_should_raise(self) -> None:
        """Verifies that an entry naming a field the merge does not compare is rejected."""
        with self.assertRaises(dc.PipelineError):
            self.read("Bodu.Ns.T.M summary  # not a compared field\n")


class DeclarationTests(unittest.TestCase):
    """normalized_declaration() puts a type's base list in canonical order and leaves everything else alone."""

    def test_normalized_declaration_when_generic_bases_should_split_at_top_level_commas(self) -> None:
        """Verifies that commas inside generic arguments do not split a base type."""
        self.assertEqual(
            m.normalized_declaration("public sealed class D<TKey, TValue> : IDictionary<TKey, TValue>, Base<TKey> where TKey : notnull"),
            "public sealed class D<TKey, TValue> : Base<TKey>, IDictionary<TKey, TValue> where TKey : notnull",
        )

    def test_normalized_declaration_when_no_base_list_should_return_it_unchanged(self) -> None:
        """Verifies that a declaration without a base list is returned as is."""
        self.assertEqual(m.normalized_declaration("public enum Color"), "public enum Color")


if __name__ == "__main__":
    unittest.main()
