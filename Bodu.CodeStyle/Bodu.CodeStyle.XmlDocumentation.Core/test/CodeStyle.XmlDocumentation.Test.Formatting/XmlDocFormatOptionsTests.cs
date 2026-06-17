// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocFormatOptionsTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Immutable;
using Bodu.CodeStyle.XmlDocumentation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.CodeStyle.XmlDocumentation.Test.Formatting;

[TestClass]
public sealed class XmlDocFormatOptionsTests
{
    /// <summary>
    /// Verifies that the constructor throws when the documentation prefix is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDocumentationPrefixIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new XmlDocFormatOptions(
                maxLineLength: 120,
                documentationPrefix: null!,
                indentText: "    ",
                collapseProseWhitespace: true,
                preserveBlankLines: false,
                preserveXmlTagAttributes: true,
                preserveCrefText: true,
                keepFieldSummaryOnSingleLine: false,
                blockTags: [],
                inlineTags: [],
                forceMultilineTags: [],
                singleLineWhenShortTags: [],
                neverSplitTagContent: [],
                tagPolicies: ImmutableDictionary<string, XmlDocTagPolicy>.Empty);
        });

        Assert.AreEqual("documentationPrefix", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the constructor throws when the indent text is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenIndentTextIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new XmlDocFormatOptions(
                maxLineLength: 120,
                documentationPrefix: "/// ",
                indentText: null!,
                collapseProseWhitespace: true,
                preserveBlankLines: false,
                preserveXmlTagAttributes: true,
                preserveCrefText: true,
                keepFieldSummaryOnSingleLine: false,
                blockTags: [],
                inlineTags: [],
                forceMultilineTags: [],
                singleLineWhenShortTags: [],
                neverSplitTagContent: [],
                tagPolicies: ImmutableDictionary<string, XmlDocTagPolicy>.Empty);
        });

        Assert.AreEqual("indentText", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the constructor throws <see cref="ArgumentOutOfRangeException" /> when the max line length
    /// is non-positive.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenMaxLineLengthIsZero_ShouldThrowArgumentOutOfRangeException()
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new XmlDocFormatOptions(
                maxLineLength: 0,
                documentationPrefix: "/// ",
                indentText: "    ",
                collapseProseWhitespace: true,
                preserveBlankLines: false,
                preserveXmlTagAttributes: true,
                preserveCrefText: true,
                keepFieldSummaryOnSingleLine: false,
                blockTags: [],
                inlineTags: [],
                forceMultilineTags: [],
                singleLineWhenShortTags: [],
                neverSplitTagContent: [],
                tagPolicies: ImmutableDictionary<string, XmlDocTagPolicy>.Empty);
        });

        Assert.AreEqual("maxLineLength", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="XmlDocFormatOptions.WithMaxLineLength" /> returns a new instance with the
    /// updated value and leaves the original intact.
    /// </summary>
    [TestMethod]
    public void WithMaxLineLength_ShouldReturnNewInstanceWithOverride()
    {
        XmlDocFormatOptions original = XmlDocFormatPolicyDefaults.CreateBoduDefaults();

        XmlDocFormatOptions updated = original.WithMaxLineLength(80);

        Assert.AreNotSame(original, updated);
        Assert.AreEqual(120, original.MaxLineLength);
        Assert.AreEqual(80, updated.MaxLineLength);
    }

    /// <summary>
    /// Verifies that <see cref="XmlDocFormatOptions.GetTagPolicy" /> returns the configured policy for known
    /// tag names.
    /// </summary>
    [TestMethod]
    public void GetTagPolicy_WhenTagIsConfigured_ShouldReturnConfiguredPolicy()
    {
        XmlDocFormatOptions options = XmlDocFormatPolicyDefaults.CreateBoduDefaults();

        XmlDocTagPolicy policy = options.GetTagPolicy("summary");

        // Default per-tag policies defer the layout decision to the convenience sets (Layout = Auto). The
        // effective layout is resolved by ResolveLayout, which classifies summary as a multiline block.
        Assert.AreEqual(XmlDocTagLayout.Auto, policy.Layout);
        Assert.AreEqual(XmlDocTagLayout.MultilineBlock, options.ResolveLayout("summary"));
    }

    /// <summary>
    /// Verifies that <see cref="XmlDocFormatOptions.GetTagPolicy" /> returns the default policy for unknown
    /// tags.
    /// </summary>
    [TestMethod]
    public void GetTagPolicy_WhenTagIsUnknown_ShouldReturnDefaultPolicy()
    {
        XmlDocFormatOptions options = XmlDocFormatPolicyDefaults.CreateBoduDefaults();

        XmlDocTagPolicy policy = options.GetTagPolicy("unknownTag");

        Assert.AreSame(XmlDocTagPolicy.Default, policy);
    }

    /// <summary>
    /// Verifies that <see cref="XmlDocFormatOptions.GetTagPolicy" /> throws when the tag name is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void GetTagPolicy_WhenTagNameIsNull_ShouldThrowArgumentNullException()
    {
        XmlDocFormatOptions options = XmlDocFormatPolicyDefaults.CreateBoduDefaults();

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = options.GetTagPolicy(null!);
        });

        Assert.AreEqual("tagName", ex.ParamName);
    }
}
