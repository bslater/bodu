// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Biff8WorkbookStreamNotFoundException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel.Binary;

/// <summary>
/// The exception thrown when a compound file does not contain a workbook stream (named <c>Workbook</c> for BIFF8 or
/// <c>Book</c> for older BIFF versions).
/// </summary>
/// <remarks>
/// A compound file may be a valid OLE2 container yet not be a spreadsheet (for example, a Word document); in that case
/// the expected workbook stream is absent and this exception is raised.
/// </remarks>
public sealed class Biff8WorkbookStreamNotFoundException
    : KeyNotFoundException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Biff8WorkbookStreamNotFoundException" /> class.
    /// </summary>
    public Biff8WorkbookStreamNotFoundException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Biff8WorkbookStreamNotFoundException" /> class with the specified
    /// message.
    /// </summary>
    /// <param name="message">A message that describes the lookup failure.</param>
    public Biff8WorkbookStreamNotFoundException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Biff8WorkbookStreamNotFoundException" /> class with the specified
    /// message and a reference to the underlying cause.
    /// </summary>
    /// <param name="message">A message that describes the lookup failure.</param>
    /// <param name="innerException">The exception that is the cause of this exception.</param>
    public Biff8WorkbookStreamNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
