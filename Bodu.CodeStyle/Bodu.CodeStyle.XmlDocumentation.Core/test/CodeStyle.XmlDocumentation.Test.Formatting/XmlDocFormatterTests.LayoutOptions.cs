// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocFormatterTests.LayoutOptions.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.CodeStyle.XmlDocumentation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.CodeStyle.XmlDocumentation.Test.Formatting;

public partial class XmlDocFormatterTests
{
    /// <summary>
    /// Verifies that a per-tag policy whose <see cref="XmlDocTagLayout" /> is set to
    /// <see cref="XmlDocTagLayout.SingleLineWhenShort" /> is authoritative: it overrides the default
    /// force-multiline classification so a short <c>&lt;summary&gt;</c> stays on a single line.
    /// </summary>
    [TestMethod]
    public void Format_WhenTagPolicyOverridesSummaryToSingleLine_ShouldKeepSummaryOnOneLine()
    {
        XmlDocFormatOptions options = CreateOptions();
        var policies = options.TagPolicies.SetItem(
            "summary",
            new XmlDocTagPolicy(XmlDocTagLayout.SingleLineWhenShort, maxSingleLineLength: options.MaxLineLength, allowLineBreakInside: true, selfClosingTrailingSpace: null));
        options = options.WithTagPolicies(policies);

        var input = "/// <summary>Foo bar.</summary>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(), options);

        Assert.IsFalse(result.Changed, "Tag policy SingleLineWhenShort should keep a short summary on one line.");
        Assert.AreEqual(input, result.FormattedText);
    }

    /// <summary>
    /// Verifies that a per-tag policy whose <see cref="XmlDocTagLayout" /> is set to
    /// <see cref="XmlDocTagLayout.MultilineBlock" /> is authoritative: it overrides the default
    /// single-line-when-short classification so a short <c>&lt;returns&gt;</c> expands to a block.
    /// </summary>
    [TestMethod]
    public void Format_WhenTagPolicyOverridesReturnsToMultiline_ShouldExpandReturns()
    {
        XmlDocFormatOptions options = CreateOptions();
        var policies = options.TagPolicies.SetItem(
            "returns",
            new XmlDocTagPolicy(XmlDocTagLayout.MultilineBlock, maxSingleLineLength: null, allowLineBreakInside: true, selfClosingTrailingSpace: null));
        options = options.WithTagPolicies(policies);

        var input = "/// <returns>The value.</returns>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(), options);

        Assert.IsTrue(result.Changed);
        Assert.AreEqual(
            "/// <returns>\r\n" +
            "    /// The value.\r\n" +
            "    /// </returns>\r\n",
            result.FormattedText);
    }

    /// <summary>
    /// Verifies that adding a custom element name to <see cref="XmlDocFormatOptions.BlockTags" /> drives that
    /// tag to a multiline block layout, even though it has no force-multiline or per-tag policy entry.
    /// </summary>
    [TestMethod]
    public void Format_WhenCustomTagAddedToBlockTags_ShouldRenderItAsMultilineBlock()
    {
        XmlDocFormatOptions options = CreateOptions().WithBlockTags(CreateOptions().BlockTags.Add("note"));

        var input = "/// <note>Hello.</note>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(), options);

        Assert.IsTrue(result.Changed);
        Assert.AreEqual(
            "/// <note>\r\n" +
            "    /// Hello.\r\n" +
            "    /// </note>\r\n",
            result.FormattedText);
    }

    /// <summary>
    /// Verifies that, by default, an element name that is not in any layout set (here <c>&lt;note&gt;</c>)
    /// flows inline and is left unchanged — the contrast case for the <see cref="XmlDocFormatOptions.BlockTags" />
    /// override above.
    /// </summary>
    [TestMethod]
    public void Format_WhenCustomTagNotInAnyLayoutSet_ShouldRemainInline()
    {
        var input = "/// <note>Hello.</note>\r\n";

        XmlDocFormatResult result = CreateFormatter().FormatTrivia(input, CreateContext(), CreateOptions());

        Assert.IsFalse(result.Changed);
        Assert.AreEqual(input, result.FormattedText);
    }
}
