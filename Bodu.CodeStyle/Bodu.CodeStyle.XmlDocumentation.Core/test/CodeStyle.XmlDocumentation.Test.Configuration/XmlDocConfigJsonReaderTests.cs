// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocConfigJsonReaderTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using Bodu.CodeStyle.XmlDocumentation;
using Bodu.CodeStyle.XmlDocumentation.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.CodeStyle.XmlDocumentation.Test.Configuration;

[TestClass]
public sealed class XmlDocConfigJsonReaderTests
{
    /// <summary>
    /// Verifies that <see cref="XmlDocConfigJsonReader.Read" /> throws when the JSON text is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenJsonIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = XmlDocConfigJsonReader.Read(null!);
        });

        Assert.AreEqual("json", ex.ParamName);
    }

    /// <summary>
    /// Verifies that an empty JSON object yields the canonical Bodu defaults.
    /// </summary>
    [TestMethod]
    public void Read_WhenJsonIsEmptyObject_ShouldReturnDefaults()
    {
        XmlDocFormatOptions options = XmlDocConfigJsonReader.Read("{}");

        Assert.AreEqual(120, options.MaxLineLength);
        Assert.AreEqual("/// ", options.DocumentationPrefix);
    }

    /// <summary>
    /// Verifies that <c>maxLineLength</c> overrides the default value.
    /// </summary>
    [TestMethod]
    public void Read_WhenMaxLineLengthExplicit_ShouldOverrideDefault()
    {
        XmlDocFormatOptions options = XmlDocConfigJsonReader.Read("{\"maxLineLength\":80}");

        Assert.AreEqual(80, options.MaxLineLength);
    }

    /// <summary>
    /// Verifies that <c>indentText</c> overrides the default value.
    /// </summary>
    [TestMethod]
    public void Read_WhenIndentTextExplicit_ShouldOverrideDefault()
    {
        XmlDocFormatOptions options = XmlDocConfigJsonReader.Read("{\"indentText\":\"\\t\"}");

        Assert.AreEqual("\t", options.IndentText);
    }

    /// <summary>
    /// Verifies that <c>blockTags</c> overrides the default block-tag set.
    /// </summary>
    [TestMethod]
    public void Read_WhenBlockTagsExplicit_ShouldOverrideDefault()
    {
        XmlDocFormatOptions options = XmlDocConfigJsonReader.Read("{\"blockTags\":[\"summary\",\"remarks\"]}");

        Assert.AreEqual(2, options.BlockTags.Count);
        Assert.IsTrue(options.BlockTags.Contains("summary"));
        Assert.IsTrue(options.BlockTags.Contains("remarks"));
    }

    /// <summary>
    /// Verifies that a per-tag policy in <c>tagPolicies</c> overrides the matching default.
    /// </summary>
    [TestMethod]
    public void Read_WhenTagPolicyOverridesSummary_ShouldReplaceLayout()
    {
        const string Json = "{\"tagPolicies\":{\"summary\":{\"layout\":\"singleLineWhenShort\",\"maxSingleLineLength\":60}}}";

        XmlDocFormatOptions options = XmlDocConfigJsonReader.Read(Json);
        XmlDocTagPolicy policy = options.GetTagPolicy("summary");

        Assert.AreEqual(XmlDocTagLayout.SingleLineWhenShort, policy.Layout);
        Assert.AreEqual(60, policy.MaxSingleLineLength);
    }

    /// <summary>
    /// Verifies that a malformed JSON document throws <see cref="XmlDocConfigException" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenJsonIsMalformed_ShouldThrowXmlDocConfigException()
    {
        XmlDocConfigException ex = Assert.ThrowsExactly<XmlDocConfigException>(() =>
        {
            _ = XmlDocConfigJsonReader.Read("{not json}");
        });

        Assert.IsNotNull(ex.InnerException);
    }

    /// <summary>
    /// Verifies that a non-object top-level JSON value throws <see cref="XmlDocConfigException" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenTopLevelIsNotObject_ShouldThrowXmlDocConfigException()
    {
        Assert.ThrowsExactly<XmlDocConfigException>(() =>
        {
            _ = XmlDocConfigJsonReader.Read("[\"summary\"]");
        });
    }

    /// <summary>
    /// Verifies that an integer-typed key receiving a string throws <see cref="XmlDocConfigException" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenMaxLineLengthIsString_ShouldThrowXmlDocConfigException()
    {
        Assert.ThrowsExactly<XmlDocConfigException>(() =>
        {
            _ = XmlDocConfigJsonReader.Read("{\"maxLineLength\":\"120\"}");
        });
    }

    /// <summary>
    /// Verifies that an unknown tag-layout name throws <see cref="XmlDocConfigException" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenTagLayoutIsUnknown_ShouldThrowXmlDocConfigException()
    {
        Assert.ThrowsExactly<XmlDocConfigException>(() =>
        {
            _ = XmlDocConfigJsonReader.Read("{\"tagPolicies\":{\"summary\":{\"layout\":\"banana\"}}}");
        });
    }

    /// <summary>
    /// Verifies that a boolean value is parsed and applied (exercising the dependency-free parser's boolean and
    /// whitespace handling).
    /// </summary>
    [TestMethod]
    public void Read_WhenBooleanWithSurroundingWhitespace_ShouldApply()
    {
        XmlDocFormatOptions options = XmlDocConfigJsonReader.Read("{\r\n  \"preserveBlankLines\" : true\r\n}");

        Assert.IsTrue(options.PreserveBlankLines);
    }

    /// <summary>
    /// Verifies that trailing content after the JSON value is rejected as malformed.
    /// </summary>
    [TestMethod]
    public void Read_WhenTrailingContentAfterObject_ShouldThrowXmlDocConfigException()
    {
        Assert.ThrowsExactly<XmlDocConfigException>(() =>
        {
            _ = XmlDocConfigJsonReader.Read("{} trailing");
        });
    }

    /// <summary>
    /// Verifies that an unterminated string is rejected as malformed.
    /// </summary>
    [TestMethod]
    public void Read_WhenStringIsUnterminated_ShouldThrowXmlDocConfigException()
    {
        Assert.ThrowsExactly<XmlDocConfigException>(() =>
        {
            _ = XmlDocConfigJsonReader.Read("{\"documentationPrefix\":\"oops}");
        });
    }
}
