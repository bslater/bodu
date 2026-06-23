// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelBinaryFormatException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel;

/// <summary>
/// The exception thrown when the content of a workbook stream does not conform to the BIFF8 record structure.
/// </summary>
/// <remarks>
/// Reports a structural failure of the BIFF record stream — a truncated record, an inconsistent length, trailing bytes,
/// or a malformed shared string table — as opposed to a missing workbook stream or an unsupported BIFF version.
/// </remarks>
public sealed class ExcelBinaryFormatException
    : FormatException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExcelBinaryFormatException" /> class.
    /// </summary>
    public ExcelBinaryFormatException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExcelBinaryFormatException" /> class with the specified message.
    /// </summary>
    /// <param name="message">A message that describes the structural failure.</param>
    public ExcelBinaryFormatException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExcelBinaryFormatException" /> class with the specified message and
    /// a reference to the underlying cause.
    /// </summary>
    /// <param name="message">A message that describes the structural failure.</param>
    /// <param name="innerException">The exception that is the cause of this exception.</param>
    public ExcelBinaryFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
