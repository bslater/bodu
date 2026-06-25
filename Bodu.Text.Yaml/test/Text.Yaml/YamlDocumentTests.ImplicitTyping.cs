// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlDocumentTests.ImplicitTyping.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies implicit scalar typing: integer radices, boolean recognition, schema-dependent resolution (the Norway
/// problem), special floating-point forms, and numbers that resolve differently under the 1.1 and 1.2 schemas.
/// </summary>
public partial class YamlDocumentTests
{
    /// <summary>Verifies that integer radix forms resolve to their decimal value under the 1.2 core schema.</summary>
    /// <param name="literal">The integer literal under test.</param>
    /// <param name="expected">The expected decoded value.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("0x1A", 26L, DisplayName = "hexadecimal")]
    [DataRow("0x1F", 31L, DisplayName = "hexadecimal upper")]
    [DataRow("0o12", 10L, DisplayName = "octal")]
    [DataRow("-42", -42L, DisplayName = "negative")]
    public void Parse_WhenIntegerLiteral_ForV12_ShouldResolveValue(string literal, long expected)
    {
        using var d = Doc($"v: {literal}\n");
        Assert.AreEqual(expected, d.RootElement.GetProperty("v").GetInt64());
    }

    /// <summary>Verifies that binary integers and underscore digit groups resolve under the 1.1 schema.</summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenBinaryAndUnderscore_ForV11_ShouldResolveValue()
    {
        using var d = Doc11("bin: 0b1010\nbig: 1_000\nflt: 1_000.5\n");
        Assert.AreEqual(10L, d.RootElement.GetProperty("bin").GetInt64());
        Assert.AreEqual(1000L, d.RootElement.GetProperty("big").GetInt64());
        Assert.AreEqual(1000.5, d.RootElement.GetProperty("flt").GetDouble());
    }

    /// <summary>Verifies that boolean case variants are recognized under the core schema.</summary>
    /// <param name="token">The boolean token under test.</param>
    /// <param name="expected">The expected boolean value.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("true", true, DisplayName = "lowercase true")]
    [DataRow("True", true, DisplayName = "title-case true")]
    [DataRow("TRUE", true, DisplayName = "uppercase true")]
    [DataRow("false", false, DisplayName = "lowercase false")]
    [DataRow("False", false, DisplayName = "title-case false")]
    public void Parse_WhenBooleanToken_ForV12_ShouldResolveBoolean(string token, bool expected)
    {
        using var d = Doc($"v: {token}\n");
        Assert.AreEqual(expected, d.RootElement.GetProperty("v").GetBoolean());
    }

    /// <summary>Verifies that the Norway tokens remain strings under the 1.2 core schema.</summary>
    /// <param name="token">The Norway-problem token under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("NO", DisplayName = "NO")]
    [DataRow("YES", DisplayName = "YES")]
    [DataRow("no", DisplayName = "no")]
    public void Parse_WhenNorwayToken_ForV12_ShouldStayString(string token)
    {
        using var d = Doc($"v: {token}\n");
        Assert.AreEqual(YamlValueKind.String, d.RootElement.GetProperty("v").ValueKind);
        Assert.AreEqual(token, d.RootElement.GetProperty("v").GetString());
    }

    /// <summary>Verifies that the Norway tokens are booleans under the 1.1 schema.</summary>
    /// <param name="token">The Norway-problem token under test.</param>
    /// <param name="expected">The expected boolean value.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("NO", false, DisplayName = "NO")]
    [DataRow("YES", true, DisplayName = "YES")]
    [DataRow("no", false, DisplayName = "no")]
    public void Parse_WhenNorwayToken_ForV11_ShouldBeBoolean(string token, bool expected)
    {
        using var d = Doc11($"v: {token}\n");
        Assert.AreEqual(YamlValueKind.Boolean, d.RootElement.GetProperty("v").ValueKind);
        Assert.AreEqual(expected, d.RootElement.GetProperty("v").GetBoolean());
    }

    /// <summary>Verifies that the special floating-point forms resolve.</summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenSpecialFloats_ShouldResolve()
    {
        using var d = Doc("a: .inf\nb: -.inf\nc: .Inf\nd: -.Inf\ne: .nan\nf: 1e3\n");
        Assert.IsTrue(double.IsPositiveInfinity(d.RootElement.GetProperty("a").GetDouble()));
        Assert.IsTrue(double.IsNegativeInfinity(d.RootElement.GetProperty("b").GetDouble()));
        Assert.IsTrue(double.IsPositiveInfinity(d.RootElement.GetProperty("c").GetDouble()));
        Assert.IsTrue(double.IsNegativeInfinity(d.RootElement.GetProperty("d").GetDouble()));
        Assert.IsTrue(double.IsNaN(d.RootElement.GetProperty("e").GetDouble()));
        Assert.AreEqual(1000.0, d.RootElement.GetProperty("f").GetDouble());
    }
}
