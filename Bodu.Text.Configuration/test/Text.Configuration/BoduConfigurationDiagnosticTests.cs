// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationDiagnosticTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

/// <summary>
/// Tests for <see cref="BoduConfigurationDiagnostic" /> construction and rendering.
/// </summary>
[TestClass]
public class BoduConfigurationDiagnosticTests
{
    /// <summary>
    /// Verifies that the ctor stores the supplied severity, code, message, and location.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenValuesProvided_ShouldExposeAllFields()
    {
        BoduConfigurationSourceLocation location = new(5, 1, 10);
        BoduConfigurationDiagnostic diagnostic = new(
            BoduConfigurationDiagnosticSeverity.Error,
            BoduConfigurationDiagnosticCode.MissingEquals,
            "missing =",
            location);

        Assert.AreEqual(BoduConfigurationDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.AreEqual(BoduConfigurationDiagnosticCode.MissingEquals, diagnostic.Code);
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
            _ = new BoduConfigurationDiagnostic(
                BoduConfigurationDiagnosticSeverity.Error,
                BoduConfigurationDiagnosticCode.None,
                null!,
                default);
        });
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationDiagnostic.ToString" /> mentions the severity, code, and
    /// message.
    /// </summary>
    [TestMethod]
    public void ToString_WhenRendered_ShouldIncludeSeverityCodeAndMessage()
    {
        BoduConfigurationDiagnostic diagnostic = new(
            BoduConfigurationDiagnosticSeverity.Warning,
            BoduConfigurationDiagnosticCode.EmptyKey,
            "blank",
            default);

        var text = diagnostic.ToString();

        StringAssert.Contains(text, "Warning");
        StringAssert.Contains(text, "EmptyKey");
        StringAssert.Contains(text, "blank");
    }
}
