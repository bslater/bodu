// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffFormatException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

/// <summary>
/// The exception thrown when BIFF data is structurally invalid: a record header is truncated, a declared payload runs
/// past the available data, or a known record's payload does not match its documented layout.
/// </summary>
/// <remarks>
/// A record type the codec does not recognize is never reported through this exception; unknown records remain readable
/// through their identifier and raw payload. When the failure is tied to a position in the data, <see cref="Offset" />
/// carries the byte offset of the record header relative to the start of the buffer the reader was created over.
/// </remarks>
public sealed class BiffFormatException
    : FormatException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BiffFormatException" /> class.
    /// </summary>
    public BiffFormatException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BiffFormatException" /> class with the specified message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public BiffFormatException(string? message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BiffFormatException" /> class with the specified message and inner
    /// exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public BiffFormatException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BiffFormatException" /> class with the specified message and the
    /// offset of the record at which the failure was detected.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="offset">The byte offset of the offending record header within the source buffer.</param>
    public BiffFormatException(string? message, int offset)
        : base(message)
    {
        Offset = offset;
    }

    /// <summary>
    /// Gets the byte offset of the record at which the failure was detected, relative to the start of the buffer the
    /// reader was created over.
    /// </summary>
    /// <value>The record header offset, or <see langword="null" /> when the failure is not tied to a position.</value>
    public int? Offset { get; }
}
