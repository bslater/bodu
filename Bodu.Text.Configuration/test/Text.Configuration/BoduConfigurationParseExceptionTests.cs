// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationParseExceptionTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

/// <summary>
/// Tests for <see cref="BoduConfigurationParseException" /> construction and diagnostic exposure.
/// </summary>
[TestClass]
public class BoduConfigurationParseExceptionTests
{
    /// <summary>
    /// Verifies that constructing from a single diagnostic exposes both <c>Diagnostic</c> and
    /// <c>Diagnostics</c>.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSingleDiagnostic_ShouldExposeDiagnosticAndDiagnostics()
    {
        BoduConfigurationDiagnostic diagnostic = new(
            BoduConfigurationDiagnosticSeverity.Error,
            BoduConfigurationDiagnosticCode.MissingEquals,
            "missing equals",
            default);

        BoduConfigurationParseException ex = new(diagnostic);

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
        BoduConfigurationDiagnostic diagnostic = new(
            BoduConfigurationDiagnosticSeverity.Error,
            BoduConfigurationDiagnosticCode.MissingEquals,
            "missing equals",
            default);

        BoduConfigurationParseException ex = new(diagnostic);

        Assert.AreEqual("missing equals", ex.Message);
    }

    /// <summary>
    /// Verifies that constructing from an empty diagnostics list yields an empty
    /// <see cref="BoduConfigurationParseException.Diagnostics" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDiagnosticsIsEmpty_ShouldExposeEmptyDiagnostics()
    {
        BoduConfigurationParseException ex = new(System.Array.Empty<BoduConfigurationDiagnostic>());

        Assert.IsEmpty(ex.Diagnostics);
        Assert.IsNull(ex.Diagnostic);
    }

    /// <summary>
    /// Verifies that the parameterless ctor produces an exception with no diagnostics.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDefault_ShouldExposeEmptyDiagnostics()
    {
        BoduConfigurationParseException ex = new();

        Assert.IsEmpty(ex.Diagnostics);
        Assert.IsNull(ex.Diagnostic);
    }
}
