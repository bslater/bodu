// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlDocumentTests.Typing.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Document;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies implicit scalar typing under the YAML 1.2 core and YAML 1.1 schemas: booleans, the Norway problem,
/// integer radices, digit grouping, and the special floating-point forms.
/// </summary>
public partial class YamlDocumentTests
{
    /// <summary>Verifies that the YAML 1.2 core schema keeps <c>no</c> a string (the Norway problem).</summary>
    [TestMethod]
    public void Parse_WhenNorwayUnderV12_ShouldStayString()
    {
        using var doc = YamlDocument.Parse("country: no\n");
        Assert.AreEqual(YamlValueKind.String, doc.RootElement.GetProperty("country").ValueKind);
        Assert.AreEqual("no", doc.RootElement.GetProperty("country").GetString());
    }

    /// <summary>Verifies that the YAML 1.1 schema treats <c>no</c> as the boolean false.</summary>
    [TestMethod]
    public void Parse_WhenNorwayUnderV11_ShouldBeBoolean()
    {
        using var doc = YamlDocument.Parse("country: no\n", new YamlDocumentOptions { SpecVersion = YamlSpecVersion.V1_1 });
        Assert.AreEqual(YamlValueKind.Boolean, doc.RootElement.GetProperty("country").ValueKind);
        Assert.IsFalse(doc.RootElement.GetProperty("country").GetBoolean());
    }

    /// <summary>Verifies that boolean case variants are recognized under the core schema.</summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenBooleanCaseVariants_ShouldResolveToBoolean()
    {
        using var doc = YamlDocument.Parse("a: true\nb: True\nc: TRUE\nd: false\ne: False\n");
        Assert.IsTrue(doc.RootElement.GetProperty("a").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("b").GetBoolean());
        Assert.IsTrue(doc.RootElement.GetProperty("c").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("d").GetBoolean());
        Assert.IsFalse(doc.RootElement.GetProperty("e").GetBoolean());
    }

    /// <summary>Verifies that <c>yes</c>/<c>no</c> stay strings under 1.2 but are booleans under 1.1.</summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenYesNoUnderBothVersions_ShouldFollowSchema()
    {
        using var d12 = YamlDocument.Parse("a: NO\nb: YES\n");
        Assert.AreEqual(YamlValueKind.String, d12.RootElement.GetProperty("a").ValueKind);
        Assert.AreEqual(YamlValueKind.String, d12.RootElement.GetProperty("b").ValueKind);

        using var d11 = YamlDocument.Parse("a: NO\nb: YES\n", new YamlDocumentOptions { SpecVersion = YamlSpecVersion.V1_1 });
        Assert.IsFalse(d11.RootElement.GetProperty("a").GetBoolean());
        Assert.IsTrue(d11.RootElement.GetProperty("b").GetBoolean());
    }

    /// <summary>Verifies that hexadecimal and negative integers resolve.</summary>
    [TestMethod]
    public void Parse_WhenHexAndNegativeIntegers_ShouldResolve()
    {
        using var doc = YamlDocument.Parse("hex: 0x1F\nneg: -42\n");
        Assert.AreEqual(31L, doc.RootElement.GetProperty("hex").GetInt64());
        Assert.AreEqual(-42L, doc.RootElement.GetProperty("neg").GetInt64());
    }

    /// <summary>Verifies that hexadecimal and octal integer forms resolve.</summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenHexAndOctalIntegers_ShouldResolve()
    {
        using var doc = YamlDocument.Parse("hex: 0x1A\noct: 0o12\n");
        Assert.AreEqual(26L, doc.RootElement.GetProperty("hex").GetInt64());
        Assert.AreEqual(10L, doc.RootElement.GetProperty("oct").GetInt64());
    }

    /// <summary>Verifies that binary integers and underscore digit groups resolve under YAML 1.1.</summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenBinaryAndUnderscoreDigits_ForV11_ShouldResolve()
    {
        using var doc = YamlDocument.Parse(
            "bin: 0b1010\nbig: 1_000\nflt: 1_000.5\n",
            new YamlDocumentOptions { SpecVersion = YamlSpecVersion.V1_1 });
        Assert.AreEqual(10L, doc.RootElement.GetProperty("bin").GetInt64());
        Assert.AreEqual(1000L, doc.RootElement.GetProperty("big").GetInt64());
        Assert.AreEqual(1000.5, doc.RootElement.GetProperty("flt").GetDouble());
    }

    /// <summary>Verifies that float special forms resolve.</summary>
    [TestMethod]
    public void Parse_WhenFloatSpecials_ShouldResolve()
    {
        using var doc = YamlDocument.Parse("inf: .inf\nninf: -.Inf\nnan: .nan\nexp: 1e3\n");
        Assert.IsTrue(double.IsPositiveInfinity(doc.RootElement.GetProperty("inf").GetDouble()));
        Assert.IsTrue(double.IsNegativeInfinity(doc.RootElement.GetProperty("ninf").GetDouble()));
        Assert.IsTrue(double.IsNaN(doc.RootElement.GetProperty("nan").GetDouble()));
        Assert.AreEqual(1000.0, doc.RootElement.GetProperty("exp").GetDouble());
    }

    /// <summary>Verifies that the special floating-point forms resolve to infinity and NaN.</summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenSpecialFloatForms_ShouldResolve()
    {
        using var doc = YamlDocument.Parse("a: .inf\nb: -.inf\nc: .Inf\nd: .nan\n");
        Assert.IsTrue(double.IsPositiveInfinity(doc.RootElement.GetProperty("a").GetDouble()));
        Assert.IsTrue(double.IsNegativeInfinity(doc.RootElement.GetProperty("b").GetDouble()));
        Assert.IsTrue(double.IsPositiveInfinity(doc.RootElement.GetProperty("c").GetDouble()));
        Assert.IsTrue(double.IsNaN(doc.RootElement.GetProperty("d").GetDouble()));
    }
}
