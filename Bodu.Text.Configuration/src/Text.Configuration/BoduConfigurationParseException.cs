// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationParseException.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;
using System.Runtime.Serialization;

namespace Bodu.Text.Configuration;

/// <summary>
/// The exception raised when a configuration document cannot be parsed. Exposes the originating diagnostic
/// (<see cref="Diagnostic" />) together with any additional diagnostics gathered before the failure.
/// </summary>
/// <remarks>
/// In <see cref="BoduConfigurationDiagnosticMode.Throw" /> mode the parser raises this exception on the first
/// recoverable error it encounters. In <see cref="BoduConfigurationDiagnosticMode.Collect" /> mode this exception
/// is raised only for non-recoverable errors (such as a truncated stream).
/// </remarks>
[Serializable]
public sealed class BoduConfigurationParseException : FormatException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BoduConfigurationParseException" /> class with a default
    /// message.
    /// </summary>
    public BoduConfigurationParseException()
        : base("The configuration document could not be parsed.")
    {
        this.Diagnostics = ImmutableArray<BoduConfigurationDiagnostic>.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BoduConfigurationParseException" /> class with the
    /// specified message.
    /// </summary>
    /// <param name="message">The message describing the failure.</param>
    public BoduConfigurationParseException(string message)
        : base(message)
    {
        this.Diagnostics = ImmutableArray<BoduConfigurationDiagnostic>.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BoduConfigurationParseException" /> class with the
    /// specified message and inner exception.
    /// </summary>
    /// <param name="message">The message describing the failure.</param>
    /// <param name="innerException">The exception that caused the failure.</param>
    public BoduConfigurationParseException(string message, Exception? innerException)
        : base(message, innerException)
    {
        this.Diagnostics = ImmutableArray<BoduConfigurationDiagnostic>.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BoduConfigurationParseException" /> class from a single
    /// diagnostic. The diagnostic is exposed both via <see cref="Diagnostic" /> and as the single entry of
    /// <see cref="Diagnostics" />.
    /// </summary>
    /// <param name="diagnostic">The diagnostic that triggered the failure.</param>
    /// <exception cref="ArgumentNullException"><paramref name="diagnostic" /> is <see langword="null" />.</exception>
    public BoduConfigurationParseException(BoduConfigurationDiagnostic diagnostic)
        : base(GetMessage(diagnostic))
    {
        this.Diagnostic = diagnostic;
        this.Diagnostics = ImmutableArray.Create(diagnostic);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BoduConfigurationParseException" /> class from a set of
    /// previously collected diagnostics. The first entry, if any, is exposed via <see cref="Diagnostic" />.
    /// </summary>
    /// <param name="diagnostics">The diagnostics gathered before the failure.</param>
    /// <exception cref="ArgumentNullException"><paramref name="diagnostics" /> is <see langword="null" />.</exception>
    public BoduConfigurationParseException(IReadOnlyList<BoduConfigurationDiagnostic> diagnostics)
        : base(GetMessage(diagnostics))
    {
        ThrowHelper.ThrowIfNull(diagnostics);

        this.Diagnostics = [.. diagnostics];
        this.Diagnostic = this.Diagnostics.Length > 0 ? this.Diagnostics[0] : null;
    }

#if !NET8_0_OR_GREATER
    /// <summary>Serialization constructor.</summary>
    /// <param name="info">The serialization information.</param>
    /// <param name="context">The streaming context.</param>
    private BoduConfigurationParseException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
        this.Diagnostics = ImmutableArray<BoduConfigurationDiagnostic>.Empty;
    }
#endif

    /// <summary>
    /// Gets the primary diagnostic that triggered this exception, or <see langword="null" /> if the exception
    /// was constructed without one.
    /// </summary>
    /// <returns>The primary diagnostic, or <see langword="null" />.</returns>
    public BoduConfigurationDiagnostic? Diagnostic { get; }

    /// <summary>
    /// Gets the diagnostics gathered prior to the failure. Always returns a non-default array; may be empty.
    /// </summary>
    /// <returns>An immutable, possibly empty list of diagnostics.</returns>
    public ImmutableArray<BoduConfigurationDiagnostic> Diagnostics { get; }

    /// <summary>
    /// Gets the location in the source document that triggered the exception.
    /// </summary>
    /// <returns>The associated <see cref="BoduConfigurationSourceLocation" />, or
    /// <see cref="BoduConfigurationSourceLocation.None" /> when no primary diagnostic is available.</returns>
    public BoduConfigurationSourceLocation Location =>
        this.Diagnostic?.Location ?? BoduConfigurationSourceLocation.None;

    private static string GetMessage(BoduConfigurationDiagnostic diagnostic)
    {
        ThrowHelper.ThrowIfNull(diagnostic);
        return diagnostic.Message;
    }

    private static string GetMessage(IReadOnlyList<BoduConfigurationDiagnostic> diagnostics)
    {
        ThrowHelper.ThrowIfNull(diagnostics);
        return diagnostics.Count > 0
            ? diagnostics[0].Message
            : "The configuration document could not be parsed.";
    }
}
