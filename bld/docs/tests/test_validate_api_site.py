"""Tests for the pure helpers in bld/docs/validate_api_site.py."""

from __future__ import annotations

import os
import sys
import tempfile
import unittest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import docs_common as dc  # noqa: E402
import validate_api_site as v  # noqa: E402

LABELS = {tfm: dc.parse_framework(tfm) for tfm in ("net8.0", "net10.0")}
SITE_URL = "https://bslater.github.io/bodu/"

# A miniature built site: a type page with a member anchor, a guide with a section anchor, and
# a guide folder served through its index.html.
SITE_PAGES = {
    "index.html": '<a href="api/Bodu.Widget.html">API</a>',
    "api/Bodu.Widget.html": '<h2 id="Bodu_Widget_Spin_">Spin</h2><a name="legacy"></a>',
    "guides/index.html": "<h1>Guides</h1>",
    "guides/widgets.html": '<h2 id="spinning">Spinning</h2>',
    "public/main.css": "",
}


def write_files(root: str, files: dict[str, str]) -> None:
    for path, text in files.items():
        full = os.path.join(root, *path.split("/"))
        os.makedirs(os.path.dirname(full), exist_ok=True)
        with open(full, "w", encoding="utf-8") as stream:
            stream.write(text)


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


class ResolveLinkTests(unittest.TestCase):
    """Resolving a link found on a rendered page to a target in the site."""

    def test_resolve_link_when_relative_should_resolve_from_the_page_directory(self) -> None:
        """Verifies that a relative link resolves against the directory of the page it appears on."""
        self.assertEqual(v.resolve_link("api/Bodu.A.html", "../guides/widgets.html#spinning"), ("guides/widgets.html", "spinning"))

    def test_resolve_link_when_fragment_only_should_target_the_same_page(self) -> None:
        """Verifies that a bare #anchor targets the page it appears on."""
        self.assertEqual(v.resolve_link("api/Bodu.A.html", "#Bodu_A_Spin_"), ("api/Bodu.A.html", "Bodu_A_Spin_"))

    def test_resolve_link_when_query_present_should_drop_it(self) -> None:
        """Verifies that the framework selector's ?view= query does not become part of the target."""
        self.assertEqual(v.resolve_link("index.html", "api/Bodu.A.html?view=net-8.0#m"), ("api/Bodu.A.html", "m"))

    def test_resolve_link_when_encoded_should_decode_entities_and_percent_escapes(self) -> None:
        """Verifies that HTML entities and percent escapes are decoded before the target is looked up."""
        self.assertEqual(v.resolve_link("index.html", "api/Bodu.A%601.html#a&amp;b"), ("api/Bodu.A`1.html", "a&b"))

    def test_resolve_link_when_link_leaves_the_site_should_return_none(self) -> None:
        """Verifies that links with a scheme, protocol-relative links, and empty links are not resolved."""
        for href in ("https://www.nuget.org/packages/Bodu.Core", "mailto:someone@example.com", "//cdn.example.com/x.js", "javascript:void(0)", ""):
            with self.subTest(href=href):
                self.assertIsNone(v.resolve_link("index.html", href))

    def test_resolve_link_when_root_absolute_should_keep_the_leading_slash(self) -> None:
        """Verifies that a root-absolute link is returned as it is, so the site check can report it."""
        self.assertEqual(v.resolve_link("api/Bodu.A.html", "/bodu/index.html"), ("/bodu/index.html", ""))


class XmlDocTargetTests(unittest.TestCase):
    """Resolving an href from XML documentation to a target in the site."""

    def test_xml_doc_target_when_relative_should_resolve_from_the_api_pages(self) -> None:
        """Verifies that a relative href resolves as it does on the API page the documentation renders on."""
        self.assertEqual(v.xml_doc_target("../guides/widgets.html#spinning", SITE_URL), ("guides/widgets.html", "spinning"))

    def test_xml_doc_target_when_published_site_url_should_resolve_within_the_site(self) -> None:
        """Verifies that an absolute URL of the published site resolves within it, at the root and in a version slot."""
        for href in (f"{SITE_URL}guides/widgets.html#spinning", f"{SITE_URL}dev/guides/widgets.html#spinning", f"{SITE_URL}1.0/guides/widgets.html#spinning"):
            with self.subTest(href=href):
                self.assertEqual(v.xml_doc_target(href, SITE_URL), ("guides/widgets.html", "spinning"))

    def test_xml_doc_target_when_other_site_should_return_none(self) -> None:
        """Verifies that an href to any other site is not checked against this one."""
        self.assertIsNone(v.xml_doc_target("https://www.rfc-editor.org/rfc/rfc8439", SITE_URL))

    def test_published_site_url_when_repository_named_should_follow_github_pages(self) -> None:
        """Verifies that the published URL is GitHub Pages' project URL for the repository."""
        saved = os.environ.get("GITHUB_REPOSITORY")
        os.environ["GITHUB_REPOSITORY"] = "Owner/repo"
        try:
            self.assertEqual(v.published_site_url(), "https://owner.github.io/repo/")
        finally:
            if saved is None:
                del os.environ["GITHUB_REPOSITORY"]
            else:
                os.environ["GITHUB_REPOSITORY"] = saved


class RenderedSiteTests(unittest.TestCase):
    """Deciding whether a resolved link lands on a file and anchor the built site has."""

    def setUp(self) -> None:
        self._directory = tempfile.TemporaryDirectory()
        self.addCleanup(self._directory.cleanup)
        write_files(self._directory.name, SITE_PAGES)
        self.site = v.RenderedSite(self._directory.name)

    def test_problem_when_file_and_anchor_exist_should_return_none(self) -> None:
        """Verifies that a link to an existing page, with or without an existing id or name anchor, resolves."""
        for target, fragment in (("api/Bodu.Widget.html", ""), ("api/Bodu.Widget.html", "Bodu_Widget_Spin_"), ("api/Bodu.Widget.html", "legacy"), ("public/main.css", "")):
            with self.subTest(target=target, fragment=fragment):
                self.assertIsNone(self.site.problem(target, fragment))

    def test_problem_when_directory_has_index_should_return_none(self) -> None:
        """Verifies that a link to a folder resolves through its index.html, including the site root."""
        self.assertIsNone(self.site.problem("guides", ""))
        self.assertIsNone(self.site.problem(".", ""))

    def test_problem_when_file_missing_should_report_it(self) -> None:
        """Verifies that a link to a page the site does not have is reported, as an old member page would be."""
        self.assertEqual(self.site.problem("api/Bodu.Widget.Spin.html", ""), "no such file in the site")

    def test_problem_when_anchor_missing_should_name_the_page(self) -> None:
        """Verifies that a link to an anchor the target page does not define is reported with that page."""
        self.assertEqual(self.site.problem("guides/widgets.html", "stopping"), "guides/widgets.html has no anchor #stopping")

    def test_problem_when_root_absolute_should_report_it(self) -> None:
        """Verifies that a root-absolute link is reported, since the site is served under a path prefix."""
        self.assertIn("root-absolute", self.site.problem("/api/Bodu.Widget.html", "") or "")

    def test_problem_when_outside_the_site_should_report_it(self) -> None:
        """Verifies that a link that climbs above the site root is reported."""
        self.assertEqual(self.site.problem("../docs/guides/widgets.html", ""), "leads outside the site")


class LinkScanTests(unittest.TestCase):
    """The two scans: the links in the rendered pages, and the hrefs in XML documentation comments."""

    def setUp(self) -> None:
        self._directory = tempfile.TemporaryDirectory()
        self.addCleanup(self._directory.cleanup)
        self.root = self._directory.name
        site_root = os.path.join(self.root, "site")
        write_files(site_root, SITE_PAGES)
        self.site_root = site_root

    def test_rendered_link_problems_when_links_resolve_should_report_nothing(self) -> None:
        """Verifies that a site whose links all resolve reports no problem."""
        self.assertEqual(v.rendered_link_problems(v.RenderedSite(self.site_root)), [])

    def test_rendered_link_problems_when_links_break_should_report_each_with_its_page(self) -> None:
        """Verifies that a missing page, a missing anchor, and an apidoc file link are each reported against their page."""
        write_files(self.site_root, {
            "api/Bodu.html": '<a href="Bodu.Widget.html#Bodu_Widget_Spin_">ok</a> <a href="../apidoc/Bodu.md">x</a>'
            ' <a href="Bodu.Widget.Spin.html">y</a> <a href="../guides/widgets.html#stopping">z</a>'
            ' <a href="https://www.nuget.org/packages/Bodu.Core">external</a>',
        })
        self.assertEqual(v.rendered_link_problems(v.RenderedSite(self.site_root)), [
            "api/Bodu.html: ../apidoc/Bodu.md (no such file in the site)",
            "api/Bodu.html: Bodu.Widget.Spin.html (no such file in the site)",
            "api/Bodu.html: ../guides/widgets.html#stopping (guides/widgets.html has no anchor #stopping)",
        ])

    def test_xml_doc_link_problems_when_href_breaks_should_report_file_and_line(self) -> None:
        """Verifies that a broken href in an XML documentation comment is reported with its file and line, and that sound, external, non-documentation, and build-output hrefs are not."""
        write_files(os.path.join(self.root, "src"), {
            "Widget.cs": "\n".join([
                '/// <seealso href="../guides/widgets.html#spinning">Spinning</seealso>',
                '/// <seealso href="https://www.rfc-editor.org/rfc/rfc8439">RFC 8439</seealso>',
                '/// <seealso href="https://bslater.github.io/bodu/dev/guides/widgets.html">Widgets</seealso>',
                '/// <seealso href="../guides/gadgets.html">Gadgets</seealso>',
                '// <seealso href="../guides/not-documentation.html" />',
                'var html = "<a href=\\"../guides/not-documentation.html\\">";',
                '    /// <see href="https://bslater.github.io/bodu/guides/widgets.html#stopping">Stopping</see>',
            ]),
            "obj/Generated.cs": '/// <seealso href="../guides/build-output.html" />',
        })
        problems = v.xml_doc_link_problems(v.RenderedSite(self.site_root), os.path.join(self.root, "src"), SITE_URL)
        self.assertEqual(problems, [
            "Widget.cs:4: ../guides/gadgets.html (no such file in the site)",
            "Widget.cs:7: https://bslater.github.io/bodu/guides/widgets.html#stopping (guides/widgets.html has no anchor #stopping)",
        ])

    def test_xml_doc_line_start_problems_when_line_opens_markdown_block_should_report_it(self) -> None:
        """Verifies that documentation lines opening a Markdown list, heading or quote are reported with file and line, and that code blocks, look-alikes, plain comments and test folders are not."""
        write_files(self.root, {
            "Pkg/src/Widget.cs": "\n".join([
                "/// <summary>",                       # 1
                "/// Its full precision",              # 2
                "/// - including zeros - is kept",     # 3 bullet
                "/// + a term",                        # 4 bullet
                "/// 1. first",                        # 5 ordered list from one
                "/// 2. second, which cannot interrupt a paragraph",
                "/// &gt; zero",                       # 7 block quote
                "/// # heading",                       # 8 heading
                "/// #1 tag, -1 value, -> arrow",
                "/// -1 value",
                "/// =",                               # 11 setext underline
                "/// </summary>",
                "/// <code>",
                "/// - inside a code block",
                "/// </code>",
                "/// <code><![CDATA[",
                "/// * inside CDATA",
                "/// ]]></code>",
                "// - a plain comment",
                "var difference = 1 - 2;",
                "/// <para>",
                "/// 1. an authored step opening its paragraph",
                "/// </para>",
                "/// See <see cref=\"Widget\" />",
                "/// - continuing after an inline tag",  # 25
            ]),
            "Pkg/test/WidgetTests.cs": "/// Test prose\n/// - a test's documentation is not rendered",
            "Pkg/shared/Shared.cs": "/// Shared prose\n/// * shared source is rendered too",
        })
        problems = v.xml_doc_line_start_problems([os.path.join(self.root, "Pkg")], self.root)
        self.assertEqual([p.split(": ", 1)[0] for p in problems], [
            os.path.join("Pkg", "shared", "Shared.cs") + ":2",
            os.path.join("Pkg", "src", "Widget.cs") + ":3",
            os.path.join("Pkg", "src", "Widget.cs") + ":4",
            os.path.join("Pkg", "src", "Widget.cs") + ":5",
            os.path.join("Pkg", "src", "Widget.cs") + ":7",
            os.path.join("Pkg", "src", "Widget.cs") + ":8",
            os.path.join("Pkg", "src", "Widget.cs") + ":11",
            os.path.join("Pkg", "src", "Widget.cs") + ":25",
        ])

    def test_continues_prose_when_previous_line_bounds_a_block_should_be_false(self) -> None:
        """Verifies that a line after a blank line or a block tag starts a block, and one after prose or an inline tag continues it."""
        for previous in ("", "<para>", "</para>", "<summary>", "<item>", '<list type="bullet">', "<description>"):
            with self.subTest(previous=previous):
                self.assertFalse(v.continues_prose(previous))
        for previous in ("some prose", 'the <see cref="X" />', "a <c>value</c>", "ends with a comma,"):
            with self.subTest(previous=previous):
                self.assertTrue(v.continues_prose(previous))

    def test_capped_when_over_the_limit_should_count_the_rest(self) -> None:
        """Verifies that a long list of broken links is cut at the limit and the remainder counted."""
        self.assertEqual(v.capped(["a", "b", "c"], limit=2), ["a", "b", "... and 1 more broken link(s)"])
        self.assertEqual(v.capped(["a", "b"], limit=2), ["a", "b"])


if __name__ == "__main__":
    unittest.main()
