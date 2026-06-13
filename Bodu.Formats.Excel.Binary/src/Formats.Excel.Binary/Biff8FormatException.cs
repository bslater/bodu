// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Biff8FormatException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel.Binary;

/// <summary>
/// The exception thrown when the content of a workbook stream does not conform to the BIFF8 record structure.
/// </summary>
/// <remarks>
/// Reports a structural failure of the BIFF record stream — a truncated record, an inconsistent length, or a malformed
/// shared string table — as opposed to a missing workbook stream or an unsupported BIFF version.
/// </remarks>
public sealed class Biff8FormatException
    : FormatException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Biff8FormatException" /> class.
    /// </summary>
    public Biff8FormatException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Biff8FormatException" /> class with the specified message.
    /// </summary>
    /// <param name="message">A message that describes the structural failure.</param>
    public Biff8FormatException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Biff8FormatException" /> class with the specified message and a
    /// reference to the underlying cause.
    /// </summary>
    /// <param name="message">A message that describes the structural failure.</param>
    /// <param name="innerException">The exception that is the cause of this exception.</param>
    public Biff8FormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
