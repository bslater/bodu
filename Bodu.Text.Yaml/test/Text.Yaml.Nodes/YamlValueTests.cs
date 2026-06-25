// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlValueTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Nodes;

namespace Bodu.Text.Yaml.Nodes;

/// <summary>
/// Verifies the mutable <see cref="YamlValue" /> scalar node: the typed <c>Create</c> factories, the reported value
/// kind, and typed retrieval through <see cref="YamlValue.GetValue{T}" />.
/// </summary>
[TestClass]
public sealed class YamlValueTests
{
    /// <summary>Verifies that a string scalar reports the string kind and retrieves its value.</summary>
    [TestMethod]
    public void Create_WhenString_ShouldReportStringKindAndValue()
    {
        var value = YamlValue.Create("hello");

        Assert.AreEqual(YamlValueKind.String, value.ValueKind);
        Assert.AreEqual("hello", value.GetValue<string>());
    }

    /// <summary>Verifies that an integer scalar reports the integer kind and retrieves its value.</summary>
    [TestMethod]
    public void Create_WhenInteger_ShouldReportIntegerKindAndValue()
    {
        var value = YamlValue.Create(42L);

        Assert.AreEqual(YamlValueKind.Integer, value.ValueKind);
        Assert.AreEqual(42L, value.GetValue<long>());
    }

    /// <summary>Verifies that a floating-point scalar reports the float kind and retrieves its value.</summary>
    [TestMethod]
    public void Create_WhenDouble_ShouldReportFloatKindAndValue()
    {
        var value = YamlValue.Create(3.5);

        Assert.AreEqual(YamlValueKind.Float, value.ValueKind);
        Assert.AreEqual(3.5, value.GetValue<double>());
    }

    /// <summary>Verifies that a boolean scalar reports the boolean kind and retrieves its value.</summary>
    [TestMethod]
    public void Create_WhenBoolean_ShouldReportBooleanKindAndValue()
    {
        var value = YamlValue.Create(true);

        Assert.AreEqual(YamlValueKind.Boolean, value.ValueKind);
        Assert.IsTrue(value.GetValue<bool>());
    }

    /// <summary>Verifies that an integer scalar converts to its canonical string form.</summary>
    [TestMethod]
    public void GetValue_WhenIntegerAsString_ShouldReturnCanonicalText()
    {
        var value = YamlValue.Create(42L);

        Assert.AreEqual("42", value.GetValue<string>());
    }

    /// <summary>Verifies that <see cref="YamlValue.GetValue{T}" /> throws when the value cannot be converted.</summary>
    [TestMethod]
    public void GetValue_WhenConversionUnsupported_ShouldThrowInvalidOperationException()
    {
        var value = YamlValue.Create("not a number");

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = value.GetValue<long>();
        });
    }
}
