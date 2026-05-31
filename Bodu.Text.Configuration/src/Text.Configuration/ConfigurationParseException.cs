// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationParseException.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;

namespace Bodu.Text.Configuration;

/// <summary>
/// The exception raised when a configuration document cannot be parsed. Exposes the originating diagnostic (
/// <see cref="Diagnostic" />) together with any additional diagnostics gathered before the failure.
/// </summary>
/// <remarks>
/// <para>
/// In <see cref="ConfigurationDiagnosticMode.Throw" /> mode the parser raises this exception on the first recoverable
/// error it encounters. In <see cref="ConfigurationDiagnosticMode.Collect" /> mode this exception is raised only for
/// non-recoverable errors (such as a truncated stream); recoverable errors surface instead on the
/// <see cref="ConfigurationParseResult.Diagnostics" /> list.
/// </para>
/// <para>
/// The exception always provides a non-default <see cref="Diagnostics" /> array; when a single diagnostic triggered the
/// failure it also appears as <see cref="Diagnostic" />. <see cref="Location" /> forwards to the primary diagnostic's
/// location and falls back to <see cref="ConfigurationSourceLocation.None" /> when no diagnostic is attached.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// try
/// {
///     IniDocument doc = ConfigurationDocument.Parse(text);
/// }
/// catch (ConfigurationParseException ex)
/// {
///     // The primary diagnostic includes a source location that points back into the document.
///     Console.WriteLine($"Parse failed at {ex.Location}: {ex.Message}");
///
///     // Every diagnostic gathered before the failure is preserved.
///     foreach (ConfigurationDiagnostic d in ex.Diagnostics)
///         Console.WriteLine($"  {d}");
/// }
///]]>
/// </example>
[Serializable]
public sealed class ConfigurationParseException : FormatException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationParseException" /> class with a default message.
    /// </summary>
    public ConfigurationParseException()
        : base("The configuration document could not be parsed.")
    {
        Diagnostics = [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationParseException" /> class with the specified message.
    /// </summary>
    /// <param name="message">The message describing the failure.</param>
    public ConfigurationParseException(string message)
        : base(message)
    {
        Diagnostics = [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationParseException" /> class with the specified message
    /// and inner exception.
    /// </summary>
    /// <param name="message">The message describing the failure.</param>
    /// <param name="innerException">The exception that caused the failure.</param>
    public ConfigurationParseException(string message, Exception? innerException)
        : base(message, innerException)
    {
        Diagnostics = [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationParseException" /> class from a single diagnostic. The
    /// diagnostic is exposed both via <see cref="Diagnostic" /> and as the single entry of <see cref="Diagnostics" />.
    /// </summary>
    /// <param name="diagnostic">The diagnostic that triggered the failure.</param>
    /// <exception cref="ArgumentNullException"><paramref name="diagnostic" /> is <see langword="null" />.</exception>
    public ConfigurationParseException(ConfigurationDiagnostic diagnostic)
        : base(GetMessage(diagnostic))
    {
        Diagnostic = diagnostic;
        Diagnostics = [diagnostic];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationParseException" /> class from a set of previously
    /// collected diagnostics. The first entry, if any, is exposed via <see cref="Diagnostic" />.
    /// </summary>
    /// <param name="diagnostics">The diagnostics gathered before the failure.</param>
    /// <exception cref="ArgumentNullException"><paramref name="diagnostics" /> is <see langword="null" />.</exception>
    public ConfigurationParseException(IReadOnlyList<ConfigurationDiagnostic> diagnostics)
        : base(GetMessage(diagnostics))
    {
        ThrowHelper.ThrowIfNull(diagnostics);

        Diagnostics = [.. diagnostics];
        Diagnostic = Diagnostics.Length > 0 ? Diagnostics[0] : null;
    }

#if !NET8_0_OR_GREATER
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationParseException" /> class from serialized data.
    /// </summary>
    /// <param name="info">The serialization information.</param>
    /// <param name="context">The streaming context.</param>
    private ConfigurationParseException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
        this.Diagnostics = ImmutableArray<ConfigurationDiagnostic>.Empty;
    }
#endif

    /// <summary>
    /// Gets the primary diagnostic that triggered this exception, or <see langword="null" /> if the exception was
    /// constructed without one.
    /// </summary>
    /// <returns>The primary diagnostic, or <see langword="null" />.</returns>
    public ConfigurationDiagnostic? Diagnostic { get; }

    /// <summary>
    /// Gets the diagnostics gathered prior to the failure. Always returns a non-default array; may be empty.
    /// </summary>
    /// <returns>An immutable, possibly empty list of diagnostics.</returns>
    public ImmutableArray<ConfigurationDiagnostic> Diagnostics { get; }

    /// <summary>
    /// Gets the location in the source document that triggered the exception.
    /// </summary>
    /// <returns>
    /// The associated <see cref="ConfigurationSourceLocation" />, or <see cref="ConfigurationSourceLocation.None" />
    /// when no primary diagnostic is available.
    /// </returns>
    public ConfigurationSourceLocation Location =>
        Diagnostic?.Location ?? ConfigurationSourceLocation.None;

    /// <summary>
    /// Derives the exception message from a single diagnostic.
    /// </summary>
    /// <param name="diagnostic">The diagnostic that triggered the failure.</param>
    /// <returns>The diagnostic's message text.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="diagnostic" /> is <see langword="null" />.</exception>
    private static string GetMessage(ConfigurationDiagnostic diagnostic)
    {
        ThrowHelper.ThrowIfNull(diagnostic);
        return diagnostic.Message;
    }

    /// <summary>
    /// Derives the exception message from a set of collected diagnostics.
    /// </summary>
    /// <param name="diagnostics">The diagnostics gathered before the failure.</param>
    /// <returns>The first diagnostic's message, or a default message when the set is empty.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="diagnostics" /> is <see langword="null" />.</exception>
    private static string GetMessage(IReadOnlyList<ConfigurationDiagnostic> diagnostics)
    {
        ThrowHelper.ThrowIfNull(diagnostics);
        return diagnostics.Count > 0
            ? diagnostics[0].Message
            : "The configuration document could not be parsed.";
    }
}
