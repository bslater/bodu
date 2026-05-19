// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationDiagnostic.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Text.Configuration;

/// <summary>
/// Represents a single diagnostic — informational message, warning, or recoverable error — produced while reading or
/// resolving a configuration document.
/// </summary>
/// <remarks>
/// Diagnostics are immutable. When <see cref="BoduConfigurationDiagnosticMode.Collect" /> is in effect, the reader
/// gathers diagnostics on the parse result's diagnostics list rather than throwing at the first issue; the document is
/// still produced and its valid portions remain usable.
/// </remarks>
[DebuggerDisplay("{Severity}: {Code} {Message,nq}")]
public sealed class BoduConfigurationDiagnostic
{
    /// <summary>
    /// Initializes a new diagnostic with the specified severity, code, message, and location.
    /// </summary>
    /// <param name="severity">The severity classification.</param>
    /// <param name="code">The stable code identifying the diagnostic category.</param>
    /// <param name="message">The human-readable message text.</param>
    /// <param name="location">The location in the source document that produced the diagnostic.</param>
    /// <exception cref="ArgumentNullException"><paramref name="message" /> is <see langword="null" />.</exception>
    public BoduConfigurationDiagnostic(
        BoduConfigurationDiagnosticSeverity severity,
        BoduConfigurationDiagnosticCode code,
        string message,
        BoduConfigurationSourceLocation location)
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
    public BoduConfigurationDiagnosticSeverity Severity { get; }

    /// <summary>
    /// Gets the stable code that identifies the diagnostic category.
    /// </summary>
    /// <returns>A <see cref="BoduConfigurationDiagnosticCode" /> value.</returns>
    public BoduConfigurationDiagnosticCode Code { get; }

    /// <summary>
    /// Gets the human-readable message that describes this diagnostic.
    /// </summary>
    /// <returns>A non-null message string.</returns>
    public string Message { get; }

    /// <summary>
    /// Gets the location in the source document that produced this diagnostic.
    /// </summary>
    /// <returns>The associated <see cref="BoduConfigurationSourceLocation" />.</returns>
    public BoduConfigurationSourceLocation Location { get; }

    /// <inheritdoc />
    public override string ToString() =>
        $"{Severity} {Code} at {Location}: {Message}";
}
