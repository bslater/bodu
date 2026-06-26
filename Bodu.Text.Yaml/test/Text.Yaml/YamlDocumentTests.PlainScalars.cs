// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlDocumentTests.PlainScalars.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Document;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies plain-scalar parsing rules: the colon and hash indicator boundaries, multi-line folding, and empty
/// (null) mapping values.
/// </summary>
public partial class YamlDocumentTests
{
    /// <summary>Verifies that a colon without a following space stays within a plain scalar (a URL).</summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenColonHasNoFollowingSpace_ShouldStayInPlainScalar()
    {
        using var doc = YamlDocument.Parse("url: http://example.com\n");
        Assert.AreEqual("http://example.com", doc.RootElement.GetProperty("url").GetString());
    }

    /// <summary>Verifies that a hash without a preceding space does not start a comment.</summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenHashHasNoPrecedingSpace_ShouldNotStartComment()
    {
        using var doc = YamlDocument.Parse("text: hash#tag\n");
        Assert.AreEqual("hash#tag", doc.RootElement.GetProperty("text").GetString());
    }

    /// <summary>Verifies that a space before a hash starts a comment that truncates the value.</summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenSpacePrecedesHash_ShouldTruncateAtComment()
    {
        using var doc = YamlDocument.Parse("text: hello # comment\n");
        Assert.AreEqual("hello", doc.RootElement.GetProperty("text").GetString());
    }

    /// <summary>Verifies that a multi-line plain scalar folds continuation lines into spaces.</summary>
    [TestMethod]
    public void Parse_WhenMultiLinePlain_ShouldFold()
    {
        using var doc = YamlDocument.Parse("text: this is\n  a long value\n");
        Assert.AreEqual("this is a long value", doc.RootElement.GetProperty("text").GetString());
    }

    /// <summary>Verifies that an empty mapping value resolves to null.</summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenMappingValueEmpty_ShouldResolveToNull()
    {
        using var doc = YamlDocument.Parse("key:\nother: value\n");
        Assert.AreEqual(YamlValueKind.Null, doc.RootElement.GetProperty("key").ValueKind);
        Assert.AreEqual("value", doc.RootElement.GetProperty("other").GetString());
    }

    /// <summary>Verifies that an explicit key with an empty value resolves to null.</summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenExplicitKeyHasEmptyValue_ShouldResolveToNull()
    {
        using var doc = YamlDocument.Parse("? key\n:\n");
        Assert.AreEqual(YamlValueKind.Null, doc.RootElement.GetProperty("key").ValueKind);
    }
}
