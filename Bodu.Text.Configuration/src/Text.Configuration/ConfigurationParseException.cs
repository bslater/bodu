// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationParseException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;

namespace Bodu.Text.Configuration;

/// <summary>
/// The exception raised when a configuration document cannot be parsed or resolved. Exposes the diagnostic that stopped
/// the operation as <see cref="Diagnostic" />; the library raises it with that diagnostic alone.
/// </summary>
/// <remarks>
/// <para>
/// In <see cref="ConfigurationDiagnosticMode.Throw" /> mode the parser raises this exception on the first recoverable
/// error it encounters. In <see cref="ConfigurationDiagnosticMode.Collect" /> and
/// <see cref="ConfigurationDiagnosticMode.Ignore" /> modes parsing never raises it: errors surface on the
/// <see cref="ConfigurationParseResult.Diagnostics" /> list or are dropped, and an I/O error from the source propagates
/// unchanged. Resolving a document raises it too, when a section name is not a valid glob (see
/// <see cref="ConfigurationPattern.Compile(string, StringComparison)" />).
/// </para>
/// <para>
/// Every exception the library raises carries exactly one diagnostic, the error that stopped the operation, as
/// <see cref="Diagnostic" /> and as the single entry of <see cref="Diagnostics" />; warnings the parser reported before
/// the error are not attached. <see cref="Diagnostics" /> holds more than one entry only when a caller constructs the
/// exception from a list with <see cref="ConfigurationParseException(IReadOnlyList{ConfigurationDiagnostic})" />, and
/// is empty, but never default, when it is constructed from a message alone. <see cref="Location" /> forwards to the
/// primary diagnostic's location and falls back to <see cref="ConfigurationSourceLocation.None" /> when no diagnostic
/// is attached.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// try
/// {
///     ConfigurationDocument doc = ConfigurationDocument.Parse(text);
/// }
/// catch (ConfigurationParseException ex)
/// {
///     // The primary diagnostic includes a source location that points back into the document.
///     Console.WriteLine($"Parse failed at {ex.Location}: {ex.Message}");
///
///     // Diagnostics holds the diagnostic that stopped the parse.
///     foreach (ConfigurationDiagnostic d in ex.Diagnostics)
///         Console.WriteLine($"  {d}");
/// }
///]]>
/// </code>
/// </example>
[Serializable]
public sealed class ConfigurationParseException
    : FormatException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationParseException" /> class with a default message.
    /// </summary>
    /// <remarks>
    /// The default message is read from the library's resources for the current UI culture.
    /// </remarks>
    public ConfigurationParseException()
        : base(ConfigurationResourceStrings.Format_Invalid_ConfigurationDocument)
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
    /// <param name="diagnostics">The diagnostics the exception carries.</param>
    /// <remarks>
    /// The library itself never uses this constructor; it is for a caller that collects diagnostics, for example with
    /// <see cref="ConfigurationDiagnosticMode.Collect" />, and reports them together.
    /// </remarks>
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
    /// <value>The primary diagnostic, or <see langword="null" />.</value>
    public ConfigurationDiagnostic? Diagnostic { get; }

    /// <summary>
    /// Gets the diagnostics the exception carries: the single diagnostic that stopped the operation when the library
    /// raises it, or the list a caller supplied. Always returns a non-default array; may be empty.
    /// </summary>
    /// <value>An immutable, possibly empty list of diagnostics.</value>
    public ImmutableArray<ConfigurationDiagnostic> Diagnostics { get; }

    /// <summary>
    /// Gets the location in the source document that triggered the exception.
    /// </summary>
    /// <value>
    /// The associated <see cref="ConfigurationSourceLocation" />, or <see cref="ConfigurationSourceLocation.None" />
    /// when no primary diagnostic is available.
    /// </value>
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
            : ConfigurationResourceStrings.Format_Invalid_ConfigurationDocument;
    }
}
