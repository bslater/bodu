// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelBinaryEncryptedWorkbookException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel;

/// <summary>
/// The exception thrown when a workbook is encrypted and therefore cannot be read by this reader.
/// </summary>
/// <remarks>
/// The presence of a file-pass record indicates the workbook's contents are protected with a password. This reader does
/// not decrypt workbooks; it reports the condition through this exception rather than returning corrupt values.
/// </remarks>
public sealed class ExcelBinaryEncryptedWorkbookException
    : NotSupportedException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExcelBinaryEncryptedWorkbookException" /> class.
    /// </summary>
    public ExcelBinaryEncryptedWorkbookException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExcelBinaryEncryptedWorkbookException" /> class with the specified
    /// message.
    /// </summary>
    /// <param name="message">A message that describes the encryption condition.</param>
    public ExcelBinaryEncryptedWorkbookException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExcelBinaryEncryptedWorkbookException" /> class with the specified
    /// message and a reference to the underlying cause.
    /// </summary>
    /// <param name="message">A message that describes the encryption condition.</param>
    /// <param name="innerException">The exception that is the cause of this exception.</param>
    public ExcelBinaryEncryptedWorkbookException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
