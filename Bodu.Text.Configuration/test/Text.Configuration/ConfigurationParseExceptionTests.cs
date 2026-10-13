// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationParseExceptionTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;
using System.Resources;

namespace Bodu.Text.Configuration;

/// <summary>
/// Tests for <see cref="ConfigurationParseException" /> construction and diagnostic exposure.
/// </summary>
[TestClass]
public partial class ConfigurationParseExceptionTests
{
    /// <summary>The name of the resource entry the default message is read from.</summary>
    private const string DefaultMessageResourceName = "Format_Invalid_ConfigurationDocument";

    /// <summary>The text the overriding resource manager answers for <see cref="DefaultMessageResourceName" />.</summary>
    private const string OverriddenMessage = "Overridden configuration parse message.";

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

    /// <summary>
    /// Verifies that the message constructor sets the message and exposes no diagnostics.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenMessage_ShouldSetMessageAndExposeEmptyDiagnostics()
    {
        ConfigurationParseException ex = new("parse failed");

        Assert.AreEqual("parse failed", ex.Message);
        Assert.IsEmpty(ex.Diagnostics);
    }

    /// <summary>
    /// Verifies that the message-and-inner-exception constructor sets both.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenMessageAndInnerException_ShouldSetBoth()
    {
        InvalidOperationException inner = new("root cause");

        ConfigurationParseException ex = new("parse failed", inner);

        Assert.AreEqual("parse failed", ex.Message);
        Assert.AreSame(inner, ex.InnerException);
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationParseException.Location" /> returns
    /// <see cref="ConfigurationSourceLocation.None" /> when no primary diagnostic is present.
    /// </summary>
    [TestMethod]
    public void Location_WhenNoDiagnostic_ShouldReturnNone()
    {
        ConfigurationParseException ex = new("parse failed");

        Assert.AreEqual(ConfigurationSourceLocation.None, ex.Location);
    }

    /// <summary>
    /// Verifies that the parameterless constructor takes its message from the project's resources, which can be
    /// localized, rather than from a string literal.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDefault_ShouldTakeTheMessageFromResources()
    {
        var message = WithResourceOverride(DefaultMessageResourceName, static () => new ConfigurationParseException().Message);

        Assert.AreEqual(OverriddenMessage, message);
    }

    /// <summary>
    /// Verifies that constructing from an empty diagnostics list takes the message from the project's resources, which
    /// can be localized, rather than from a string literal.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDiagnosticsIsEmpty_ShouldTakeTheMessageFromResources()
    {
        var message = WithResourceOverride(
            DefaultMessageResourceName,
            static () => new ConfigurationParseException(Array.Empty<ConfigurationDiagnostic>()).Message);

        Assert.AreEqual(OverriddenMessage, message);
    }

    /// <summary>
    /// Runs <paramref name="create" /> while the project's resources answer <paramref name="name" /> with
    /// <see cref="OverriddenMessage" />, and restores the resources afterwards.
    /// </summary>
    /// <param name="name">The name of the resource entry to override.</param>
    /// <param name="create">The callback that reads a message.</param>
    /// <returns>The message <paramref name="create" /> returned.</returns>
    private static string WithResourceOverride(string name, Func<string> create)
    {
        FieldInfo field = typeof(ConfigurationResourceStrings).GetField("resourceMan", BindingFlags.NonPublic | BindingFlags.Static)!;
        ResourceManager original = ConfigurationResourceStrings.ResourceManager;
        field.SetValue(null, new OverridingResourceManager(original, name, OverriddenMessage));
        try
        {
            return create();
        }
        finally
        {
            field.SetValue(null, original);
        }
    }
}
