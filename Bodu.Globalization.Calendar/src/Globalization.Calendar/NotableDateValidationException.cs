// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateValidationException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// The exception thrown when a notable-date document resource fails to load because one or more error-severity
/// validation diagnostics were produced. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="Diagnostics" /> property carries the full diagnostic set — both the errors that caused the failure
/// and any warnings gathered along the way — so a caller can report every problem at once rather than discovering them
/// one load at a time.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// try
/// {
///     NotableDateResource resource = NotableDateResourceLoader.Load(documentXml);
/// }
/// catch (NotableDateValidationException ex)
/// {
///     foreach (NotableDateValidationDiagnostic diagnostic in ex.Diagnostics)
///         Console.WriteLine($"{diagnostic.Severity} {diagnostic.Code}: {diagnostic.Message}");
/// }
///]]>
/// </code>
/// </example>
/// <seealso cref="NotableDateValidationDiagnostic" />
/// <seealso cref="NotableDateResourceLoader" />
/// <seealso href="../guides/calendar/building-the-service.html">Building and extending the service (guide)</seealso>
public sealed class NotableDateValidationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDateValidationException" /> class.
    /// </summary>
    public NotableDateValidationException()
        : this(string.Empty, Array.Empty<NotableDateValidationDiagnostic>())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDateValidationException" /> class with a message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public NotableDateValidationException(string message)
        : this(message, Array.Empty<NotableDateValidationDiagnostic>())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDateValidationException" /> class with a message and inner
    /// exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public NotableDateValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
        this.Diagnostics = Array.Empty<NotableDateValidationDiagnostic>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDateValidationException" /> class with a message and the
    /// diagnostics that caused the failure.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="diagnostics">The diagnostics produced while loading the notable-date document.</param>
    /// <exception cref="ArgumentNullException"><paramref name="diagnostics" /> is <see langword="null" />.</exception>
    public NotableDateValidationException(string message, IReadOnlyList<NotableDateValidationDiagnostic> diagnostics)
        : base(message)
    {
        ThrowHelper.ThrowIfNull(diagnostics);

        this.Diagnostics = diagnostics;
    }

    /// <summary>
    /// Gets the diagnostics produced while loading the notable-date document.
    /// </summary>
    /// <returns>The diagnostics; empty when none were supplied.</returns>
    public IReadOnlyList<NotableDateValidationDiagnostic> Diagnostics { get; }
}
