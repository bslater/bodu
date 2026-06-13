// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Biff8UnsupportedRecordException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel.Binary;

/// <summary>
/// The exception thrown when a workbook declares a BIFF version or feature this reader does not support.
/// </summary>
/// <remarks>
/// The reader targets BIFF8 (Excel 97-2003). A workbook written in an earlier BIFF version, identified by the version
/// field of its beginning-of-file record, is reported through this exception rather than mis-parsed.
/// </remarks>
public sealed class Biff8UnsupportedRecordException
    : NotSupportedException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Biff8UnsupportedRecordException" /> class.
    /// </summary>
    public Biff8UnsupportedRecordException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Biff8UnsupportedRecordException" /> class with the specified
    /// message.
    /// </summary>
    /// <param name="message">A message that describes the unsupported feature.</param>
    public Biff8UnsupportedRecordException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Biff8UnsupportedRecordException" /> class with the specified
    /// message and a reference to the underlying cause.
    /// </summary>
    /// <param name="message">A message that describes the unsupported feature.</param>
    /// <param name="innerException">The exception that is the cause of this exception.</param>
    public Biff8UnsupportedRecordException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
