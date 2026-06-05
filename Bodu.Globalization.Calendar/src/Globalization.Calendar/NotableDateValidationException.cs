// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateValidationException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// The exception thrown when a notable-date document resource fails to load because one or more error-severity
/// validation diagnostics were produced.
/// </summary>
public sealed class NotableDateValidationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDateValidationException" /> class.
    /// </summary>
    public NotableDateValidationException()
        : this(string.Empty, [])
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDateValidationException" /> class with a message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public NotableDateValidationException(string message)
        : this(message, [])
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
        this.Diagnostics = [];
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
