// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationDiagnosticTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

/// <summary>
/// Tests for <see cref="ConfigurationDiagnostic" /> construction and rendering.
/// </summary>
[TestClass]
public class ConfigurationDiagnosticTests
{
    /// <summary>
    /// Verifies that the ctor stores the supplied severity, code, message, and location.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenValuesProvided_ShouldExposeAllFields()
    {
        ConfigurationSourceLocation location = new(5, 1, 10);
        ConfigurationDiagnostic diagnostic = new(
            ConfigurationDiagnosticSeverity.Error,
            ConfigurationDiagnosticCode.MissingEquals,
            "missing =",
            location);

        Assert.AreEqual(ConfigurationDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.AreEqual(ConfigurationDiagnosticCode.MissingEquals, diagnostic.Code);
        Assert.AreEqual("missing =", diagnostic.Message);
        Assert.AreEqual(location, diagnostic.Location);
    }

    /// <summary>
    /// Verifies that the ctor rejects a <see langword="null" /> message.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenMessageIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new ConfigurationDiagnostic(
                ConfigurationDiagnosticSeverity.Error,
                ConfigurationDiagnosticCode.None,
                null!,
                default);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationDiagnostic.ToString" /> mentions the severity, code, and
    /// message.
    /// </summary>
    [TestMethod]
    public void ToString_WhenRendered_ShouldIncludeSeverityCodeAndMessage()
    {
        ConfigurationDiagnostic diagnostic = new(
            ConfigurationDiagnosticSeverity.Warning,
            ConfigurationDiagnosticCode.EmptyKey,
            "blank",
            default);

        var text = diagnostic.ToString();

        StringAssert.Contains(text, "Warning");
        StringAssert.Contains(text, "EmptyKey");
        StringAssert.Contains(text, "blank");
    }
}
