// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocFormatPolicyDefaultsTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.CodeStyle.XmlDocumentation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.CodeStyle.XmlDocumentation.Test.Formatting;

[TestClass]
public sealed class XmlDocFormatPolicyDefaultsTests
{
    /// <summary>
    /// Verifies that the default policy reports the documented scalar defaults.
    /// </summary>
    [TestMethod]
    public void CreateBoduDefaults_ShouldExposeDocumentedScalarDefaults()
    {
        XmlDocFormatOptions options = XmlDocFormatPolicyDefaults.CreateBoduDefaults();

        Assert.AreEqual(120, options.MaxLineLength);
        Assert.AreEqual("/// ", options.DocumentationPrefix);
        Assert.AreEqual("    ", options.IndentText);
        Assert.IsTrue(options.CollapseProseWhitespace);
        Assert.IsFalse(options.PreserveBlankLines);
        Assert.IsTrue(options.PreserveXmlTagAttributes);
        Assert.IsTrue(options.PreserveCrefText);
    }

    /// <summary>
    /// Verifies that <c>summary</c> is in the block-tag, force-multiline tag set.
    /// </summary>
    [TestMethod]
    public void CreateBoduDefaults_ShouldIncludeSummaryInBlockAndForceMultiline()
    {
        XmlDocFormatOptions options = XmlDocFormatPolicyDefaults.CreateBoduDefaults();

        Assert.IsTrue(options.BlockTags.Contains("summary"));
        Assert.IsTrue(options.ForceMultilineTags.Contains("summary"));
    }

    /// <summary>
    /// Verifies that the inline tag set contains the canonical Bodu inline atoms.
    /// </summary>
    [TestMethod]
    public void CreateBoduDefaults_ShouldExposeInlineTagSet()
    {
        XmlDocFormatOptions options = XmlDocFormatPolicyDefaults.CreateBoduDefaults();

        Assert.IsTrue(options.InlineTags.Contains("c"));
        Assert.IsTrue(options.InlineTags.Contains("see"));
        Assert.IsTrue(options.InlineTags.Contains("paramref"));
        Assert.IsTrue(options.InlineTags.Contains("typeparamref"));
    }

    /// <summary>
    /// Verifies that the single-line-when-short tag set contains the canonical Bodu shortlist.
    /// </summary>
    [TestMethod]
    public void CreateBoduDefaults_ShouldExposeSingleLineWhenShortTagSet()
    {
        XmlDocFormatOptions options = XmlDocFormatPolicyDefaults.CreateBoduDefaults();

        Assert.IsTrue(options.SingleLineWhenShortTags.Contains("param"));
        Assert.IsTrue(options.SingleLineWhenShortTags.Contains("typeparam"));
        Assert.IsTrue(options.SingleLineWhenShortTags.Contains("returns"));
        Assert.IsTrue(options.SingleLineWhenShortTags.Contains("exception"));
        Assert.IsTrue(options.SingleLineWhenShortTags.Contains("value"));
    }

    /// <summary>
    /// Verifies that the default policy configures the canonical inline atoms with the trailing-space
    /// override.
    /// </summary>
    [TestMethod]
    public void CreateBoduDefaults_ShouldConfigureInlineAtomsWithTrailingSpace()
    {
        XmlDocFormatOptions options = XmlDocFormatPolicyDefaults.CreateBoduDefaults();

        Assert.AreEqual(true, options.GetTagPolicy("see").SelfClosingTrailingSpace);
        Assert.AreEqual(true, options.GetTagPolicy("paramref").SelfClosingTrailingSpace);
        Assert.AreEqual(true, options.GetTagPolicy("typeparamref").SelfClosingTrailingSpace);
        Assert.AreEqual(true, options.GetTagPolicy("c").SelfClosingTrailingSpace);
    }

    /// <summary>
    /// Verifies that the default policy reports the documented scalar accessor properties.
    /// </summary>
    [TestMethod]
    public void StaticDefaults_ShouldMatchPolicyValues()
    {
        Assert.AreEqual(120, XmlDocFormatPolicyDefaults.DefaultMaxLineLength);
        Assert.AreEqual("/// ", XmlDocFormatPolicyDefaults.DefaultDocumentationPrefix);
        Assert.AreEqual("    ", XmlDocFormatPolicyDefaults.DefaultIndentText);
    }
}
