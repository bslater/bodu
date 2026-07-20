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

    /// <summary>Verifies that an unsigned hexadecimal literal resolves to an integer under the core schema.</summary>
    [TestMethod]
    public void Parse_WhenUnsignedHexLiteral_ForV12_ShouldResolveInteger()
    {
        using var doc = YamlDocument.Parse("v: 0xF\n");
        Assert.AreEqual(YamlValueKind.Integer, doc.RootElement.GetProperty("v").ValueKind);
        Assert.AreEqual(15L, doc.RootElement.GetProperty("v").GetInt64());
    }

    /// <summary>Verifies that a signed hexadecimal literal is not an integer and stays a string under the core schema.</summary>
    [TestMethod]
    public void Parse_WhenSignedHexLiteral_ForV12_ShouldStayString()
    {
        using var doc = YamlDocument.Parse("v: -0xF\n");
        Assert.AreEqual(YamlValueKind.String, doc.RootElement.GetProperty("v").ValueKind);
        Assert.AreEqual("-0xF", doc.RootElement.GetProperty("v").GetString());
    }

    /// <summary>Verifies that a signed octal literal is not an integer and stays a string under YAML 1.1.</summary>
    [TestMethod]
    public void Parse_WhenSignedOctalLiteral_ForV11_ShouldStayString()
    {
        using var doc = YamlDocument.Parse("v: -0o7\n", new YamlDocumentOptions { SpecVersion = YamlSpecVersion.V1_1 });
        Assert.AreEqual(YamlValueKind.String, doc.RootElement.GetProperty("v").ValueKind);
        Assert.AreEqual("-0o7", doc.RootElement.GetProperty("v").GetString());
    }

    /// <summary>Verifies that an integer just above 2^53 is preserved exactly rather than rounded through a double.</summary>
    [TestMethod]
    public void Parse_WhenIntegerAbovePow2_53_ShouldPreserveExactValue()
    {
        using var doc = YamlDocument.Parse("v: 9007199254740993\n");
        Assert.AreEqual(YamlValueKind.Integer, doc.RootElement.GetProperty("v").ValueKind);
        Assert.AreEqual(9007199254740993L, doc.RootElement.GetProperty("v").GetInt64());
    }

    /// <summary>Verifies that <see cref="long.MaxValue" /> resolves to an integer with its exact value.</summary>
    [TestMethod]
    public void Parse_WhenInt64MaxValue_ShouldResolveExactly()
    {
        using var doc = YamlDocument.Parse("v: 9223372036854775807\n");
        Assert.AreEqual(YamlValueKind.Integer, doc.RootElement.GetProperty("v").ValueKind);
        Assert.AreEqual(long.MaxValue, doc.RootElement.GetProperty("v").GetInt64());
    }

    /// <summary>
    /// Verifies that an integer beyond the profile's 64-bit integer range is not silently coerced to a lossy integer.
    /// </summary>
    [TestMethod]
    public void Parse_WhenIntegerBeyondInt64Range_ShouldNotResolveToInteger()
    {
        using var doc = YamlDocument.Parse("v: 18446744073709551615\n");
        Assert.AreNotEqual(YamlValueKind.Integer, doc.RootElement.GetProperty("v").ValueKind);
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

    /// <summary>
    /// Verifies that colon-separated base-60 ("sexagesimal") scalars remain strings under the YAML 1.2 core schema,
    /// so tokens such as a time-of-day, a port range, or a git duration are never silently reinterpreted as numbers.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("22:22")]
    [DataRow("1:2:3")]
    [DataRow("190:20:30")]
    [DataRow("22:22:22.5")]
    public void Parse_WhenSexagesimalUnderV12_ShouldStayString(string scalar)
    {
        using var doc = YamlDocument.Parse($"v: {scalar}\n");
        Assert.AreEqual(YamlValueKind.String, doc.RootElement.GetProperty("v").ValueKind);
        Assert.AreEqual(scalar, doc.RootElement.GetProperty("v").GetString());
    }

    /// <summary>
    /// Verifies that colon-separated base-60 ("sexagesimal") scalars remain strings even under the YAML 1.1 schema.
    /// The historical 1.1 schema resolved <c>base-60</c> integers and floats — a notorious footgun that silently
    /// turned MAC-address-like or time-like values into large numbers — so this parser deliberately does not honor it.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("22:22")]
    [DataRow("1:2:3")]
    [DataRow("190:20:30")]
    public void Parse_WhenSexagesimalUnderV11_ShouldStayString(string scalar)
    {
        using var doc = YamlDocument.Parse($"v: {scalar}\n", new YamlDocumentOptions { SpecVersion = YamlSpecVersion.V1_1 });
        Assert.AreEqual(YamlValueKind.String, doc.RootElement.GetProperty("v").ValueKind);
        Assert.AreEqual(scalar, doc.RootElement.GetProperty("v").GetString());
    }

    /// <summary>
    /// Verifies that a leading-zero integer is read as octal under YAML 1.1, where the legacy schema treats
    /// <c>0NNN</c> as base-8.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenLeadingZeroInteger_ForV11_ShouldResolveAsOctal()
    {
        using var doc = YamlDocument.Parse("a: 010\nb: 0777\n", new YamlDocumentOptions { SpecVersion = YamlSpecVersion.V1_1 });
        Assert.AreEqual(8L, doc.RootElement.GetProperty("a").GetInt64());
        Assert.AreEqual(511L, doc.RootElement.GetProperty("b").GetInt64());
    }

    /// <summary>
    /// Verifies that the same leading-zero integer is read as decimal under the YAML 1.2 core schema, whose integer
    /// production is plain base-10. The identical literal <c>010</c> therefore means 8 under 1.1 but 10 under 1.2 — the
    /// version-dependent octal ambiguity is pinned on both sides so the divergence stays intentional.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenLeadingZeroInteger_ForV12Core_ShouldResolveAsDecimal()
    {
        using var doc = YamlDocument.Parse("a: 010\nb: 0777\n");
        Assert.AreEqual(10L, doc.RootElement.GetProperty("a").GetInt64());
        Assert.AreEqual(777L, doc.RootElement.GetProperty("b").GetInt64());
    }

    /// <summary>
    /// Verifies that ISO-8601-like date and timestamp scalars remain strings under the YAML 1.2 core schema, which —
    /// unlike YAML 1.1 — carries no implicit <c>!!timestamp</c> resolution, so a date is never coerced to a number.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("2002-12-14")]
    [DataRow("2001-12-14T21:59:43.10-05:00")]
    [DataRow("2001-12-14t21:59:43.10-05:00")]
    [DataRow("2001-12-15 2:59:43.10")]
    public void Parse_WhenTimestampLikeScalar_ForV12Core_ShouldStayString(string scalar)
    {
        using var doc = YamlDocument.Parse($"when: {scalar}\n");
        Assert.AreEqual(YamlValueKind.String, doc.RootElement.GetProperty("when").ValueKind);
        Assert.AreEqual(scalar, doc.RootElement.GetProperty("when").GetString());
    }

    /// <summary>Verifies that every recognized null spelling and the empty value resolve to the null kind.</summary>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("~")]
    [DataRow("null")]
    [DataRow("Null")]
    [DataRow("NULL")]
    public void Parse_WhenNullSpelling_ShouldResolveToNull(string scalar)
    {
        using var doc = YamlDocument.Parse($"v: {scalar}\n");
        Assert.AreEqual(YamlValueKind.Null, doc.RootElement.GetProperty("v").ValueKind);
    }

    /// <summary>
    /// Verifies that null spellings borrowed from other languages are not recognized and stay strings, so the null
    /// set is exactly the three case forms and <c>~</c> rather than an open-ended list.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("None")]
    [DataRow("nil")]
    [DataRow("NuLL")]
    [DataRow("nan")]
    public void Parse_WhenNonCanonicalNullSpelling_ShouldStayString(string scalar)
    {
        using var doc = YamlDocument.Parse($"v: {scalar}\n");
        Assert.AreEqual(YamlValueKind.String, doc.RootElement.GetProperty("v").ValueKind);
        Assert.AreEqual(scalar, doc.RootElement.GetProperty("v").GetString());
    }

    /// <summary>
    /// Verifies that floating-point literals with a bare leading or trailing decimal point resolve to floats, matching
    /// the YAML 1.2 core float production which permits <c>.5</c> and <c>5.</c>.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenBareDecimalPointFloats_ShouldResolve()
    {
        using var doc = YamlDocument.Parse("a: .5\nb: 5.\n");
        Assert.AreEqual(YamlValueKind.Float, doc.RootElement.GetProperty("a").ValueKind);
        Assert.AreEqual(0.5, doc.RootElement.GetProperty("a").GetDouble());
        Assert.AreEqual(YamlValueKind.Float, doc.RootElement.GetProperty("b").ValueKind);
        Assert.AreEqual(5.0, doc.RootElement.GetProperty("b").GetDouble());
    }
}
