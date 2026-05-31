// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationDiagnostic.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Text.Configuration;

/// <summary>
/// Represents a single diagnostic — informational message, warning, or recoverable error — produced while reading or
/// resolving a configuration document.
/// </summary>
/// <remarks>
/// <para>
/// Diagnostics are immutable. When <see cref="ConfigurationDiagnosticMode.Collect" /> is in effect, the reader gathers
/// diagnostics on the parse result's diagnostics list rather than throwing at the first issue; the document is still
/// produced and its valid portions remain usable.
/// </para>
/// <para>
/// Each diagnostic carries a stable <see cref="Code" /> from <see cref="ConfigurationDiagnosticCode" /> so that callers
/// can suppress or escalate categories programmatically, a <see cref="Severity" /> for filtering, a human-readable
/// <see cref="Message" /> for display, and a <see cref="Location" /> pointing back into the source document for editor
/// integration. <see cref="ToString" /> renders the four fields in a single line suitable for log output.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// Surface every diagnostic the reader produced and tag any errors for caller attention.
/// foreach (ConfigurationDiagnostic d in result.Diagnostics)
/// {
///     Console.WriteLine(d); // "Warning DuplicateKey at line 12, column 3: ..."
///     if (d.Severity == ConfigurationDiagnosticSeverity.Error)
///         hasErrors = true;
/// }
///
/// Build one directly — useful when a host integrates Bodu diagnostics into its own pipeline.
/// var diag = new ConfigurationDiagnostic(
///     ConfigurationDiagnosticSeverity.Warning,
///     ConfigurationDiagnosticCode.UnknownKey,
///     "Key 'format:legacy' is no longer recognized.",
///     new ConfigurationSourceLocation(lineNumber: 7, linePosition: 1, length: 14));
///]]>
/// </example>
[DebuggerDisplay("{Severity}: {Code} {Message,nq}")]
public sealed class ConfigurationDiagnostic
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationDiagnostic" /> class with the specified severity,
    /// code, message, and location.
    /// </summary>
    /// <param name="severity">The severity classification.</param>
    /// <param name="code">The stable code identifying the diagnostic category.</param>
    /// <param name="message">The human-readable message text.</param>
    /// <param name="location">The location in the source document that produced the diagnostic.</param>
    /// <exception cref="ArgumentNullException"><paramref name="message" /> is <see langword="null" />.</exception>
    public ConfigurationDiagnostic(
        ConfigurationDiagnosticSeverity severity,
        ConfigurationDiagnosticCode code,
        string message,
        ConfigurationSourceLocation location)
    {
        ThrowHelper.ThrowIfNull(message);

        Severity = severity;
        Code = code;
        Message = message;
        Location = location;
    }

    /// <summary>
    /// Gets the severity classification for this diagnostic.
    /// </summary>
    /// <returns>The severity value supplied at construction.</returns>
    public ConfigurationDiagnosticSeverity Severity { get; }

    /// <summary>
    /// Gets the stable code that identifies the diagnostic category.
    /// </summary>
    /// <returns>A <see cref="ConfigurationDiagnosticCode" /> value.</returns>
    public ConfigurationDiagnosticCode Code { get; }

    /// <summary>
    /// Gets the human-readable message that describes this diagnostic.
    /// </summary>
    /// <returns>A non-null message string.</returns>
    public string Message { get; }

    /// <summary>
    /// Gets the location in the source document that produced this diagnostic.
    /// </summary>
    /// <returns>The associated <see cref="ConfigurationSourceLocation" />.</returns>
    public ConfigurationSourceLocation Location { get; }

    /// <inheritdoc />
    public override string ToString() =>
        $"{Severity} {Code} at {Location}: {Message}";
}
