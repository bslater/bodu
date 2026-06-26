// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlDocumentTests.Tags.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Document;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies core-tag composition: the <c>!!str</c> tag forces a string and the <c>!!int</c> tag reinterprets a
/// quoted scalar as an integer.
/// </summary>
public partial class YamlDocumentTests
{
    /// <summary>Verifies that the <c>!!str</c> tag forces a numeric-looking scalar to remain a string.</summary>
    [TestMethod]
    public void Parse_WhenStrTag_ShouldForceString()
    {
        using var doc = YamlDocument.Parse("a: !!str 123\nb: !!str true\n");
        Assert.AreEqual(YamlValueKind.String, doc.RootElement.GetProperty("a").ValueKind);
        Assert.AreEqual("123", doc.RootElement.GetProperty("a").GetString());
        Assert.AreEqual("true", doc.RootElement.GetProperty("b").GetString());
    }

    /// <summary>Verifies that the <c>!!int</c> tag reinterprets a quoted scalar as an integer.</summary>
    [TestMethod]
    public void Parse_WhenIntTag_ShouldForceInteger()
    {
        using var doc = YamlDocument.Parse("a: !!int \"42\"\n");
        Assert.AreEqual(YamlValueKind.Integer, doc.RootElement.GetProperty("a").ValueKind);
        Assert.AreEqual(42L, doc.RootElement.GetProperty("a").GetInt64());
    }
}
