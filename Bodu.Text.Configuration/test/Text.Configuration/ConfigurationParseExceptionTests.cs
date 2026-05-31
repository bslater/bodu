// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationParseExceptionTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

/// <summary>
/// Tests for <see cref="ConfigurationParseException" /> construction and diagnostic exposure.
/// </summary>
[TestClass]
public class ConfigurationParseExceptionTests
{
    /// <summary>
    /// Verifies that constructing from a single diagnostic exposes both <c>Diagnostic</c> and
    /// <c>Diagnostics</c>.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSingleDiagnostic_ShouldExposeDiagnosticAndDiagnostics()
    {
        ConfigurationDiagnostic diagnostic = new(
            ConfigurationDiagnosticSeverity.Error,
            ConfigurationDiagnosticCode.MissingEquals,
            "missing equals",
            default);

        ConfigurationParseException ex = new(diagnostic);

        Assert.AreSame(diagnostic, ex.Diagnostic);
        Assert.HasCount(1, ex.Diagnostics);
        Assert.AreSame(diagnostic, ex.Diagnostics[0]);
    }

    /// <summary>
    /// Verifies that constructing from a single diagnostic uses the diagnostic message as the exception
    /// message.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSingleDiagnostic_ShouldUseDiagnosticMessageAsExceptionMessage()
    {
        ConfigurationDiagnostic diagnostic = new(
            ConfigurationDiagnosticSeverity.Error,
            ConfigurationDiagnosticCode.MissingEquals,
            "missing equals",
            default);

        ConfigurationParseException ex = new(diagnostic);

        Assert.AreEqual("missing equals", ex.Message);
    }

    /// <summary>
    /// Verifies that constructing from an empty diagnostics list yields an empty
    /// <see cref="ConfigurationParseException.Diagnostics" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDiagnosticsIsEmpty_ShouldExposeEmptyDiagnostics()
    {
        ConfigurationParseException ex = new(System.Array.Empty<ConfigurationDiagnostic>());

        Assert.IsEmpty(ex.Diagnostics);
        Assert.IsNull(ex.Diagnostic);
    }

    /// <summary>
    /// Verifies that the parameterless ctor produces an exception with no diagnostics.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDefault_ShouldExposeEmptyDiagnostics()
    {
        ConfigurationParseException ex = new();

        Assert.IsEmpty(ex.Diagnostics);
        Assert.IsNull(ex.Diagnostic);
    }
}
