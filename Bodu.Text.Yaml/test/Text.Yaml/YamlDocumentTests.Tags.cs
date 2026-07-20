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

    /// <summary>Verifies that the <c>!!int</c> tag accepts a hexadecimal literal, resolving it under the schema.</summary>
    [TestMethod]
    public void Parse_WhenIntTagHasHexLiteral_ShouldResolveValue()
    {
        using var doc = YamlDocument.Parse("a: !!int 0x2A\n");
        Assert.AreEqual(YamlValueKind.Integer, doc.RootElement.GetProperty("a").ValueKind);
        Assert.AreEqual(42L, doc.RootElement.GetProperty("a").GetInt64());
    }

    /// <summary>Verifies that the <c>!!float</c> tag widens an integral literal to a floating-point value.</summary>
    [TestMethod]
    public void Parse_WhenFloatTagHasIntegralLiteral_ShouldWidenToFloat()
    {
        using var doc = YamlDocument.Parse("a: !!float 42\n");
        Assert.AreEqual(YamlValueKind.Float, doc.RootElement.GetProperty("a").ValueKind);
        Assert.AreEqual(42.0, doc.RootElement.GetProperty("a").GetDouble());
    }

    /// <summary>Verifies that the <c>!!int</c> tag with non-integer content is rejected.</summary>
    [TestMethod]
    public void Parse_WhenIntTagHasNonIntegerContent_ShouldThrow()
    {
        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var _ = YamlDocument.Parse("a: !!int abc\n");
        });
    }

    /// <summary>Verifies that the <c>!!bool</c> tag with invalid content is rejected.</summary>
    [TestMethod]
    public void Parse_WhenBoolTagHasInvalidContent_ShouldThrow()
    {
        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var _ = YamlDocument.Parse("a: !!bool not-a-bool\n");
        });
    }

    /// <summary>Verifies that the <c>!!float</c> tag with non-numeric content is rejected.</summary>
    [TestMethod]
    public void Parse_WhenFloatTagHasInvalidContent_ShouldThrow()
    {
        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var _ = YamlDocument.Parse("a: !!float abc\n");
        });
    }

    /// <summary>Verifies that the <c>!!str</c> tag wrapping a boolean-like value keeps it a string.</summary>
    [TestMethod]
    public void Parse_WhenStrTagWrapsBoolLikeValue_ShouldRemainString()
    {
        using var doc = YamlDocument.Parse("a: !!str true\n");
        Assert.AreEqual(YamlValueKind.String, doc.RootElement.GetProperty("a").ValueKind);
        Assert.AreEqual("true", doc.RootElement.GetProperty("a").GetString());
    }

    /// <summary>Verifies that the <c>!!null</c> tag forces the null kind onto an otherwise non-null scalar.</summary>
    [TestMethod]
    public void Parse_WhenNullTag_ShouldForceNull()
    {
        using var doc = YamlDocument.Parse("a: !!null\nb: !!null value\n");
        Assert.AreEqual(YamlValueKind.Null, doc.RootElement.GetProperty("a").ValueKind);
        Assert.AreEqual(YamlValueKind.Null, doc.RootElement.GetProperty("b").ValueKind);
    }
}
