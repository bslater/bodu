// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Biff8WorkbookProperties.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Compound.PropertySets;

namespace Bodu.Formats.Excel.Binary;

/// <summary>
/// Exposes the document properties of a workbook, read from the compound file's summary-information property sets.
/// </summary>
/// <remarks>
/// The values are sourced from the <c>SummaryInformation</c> and <c>DocumentSummaryInformation</c> streams. A workbook
/// that omits a stream, or whose property set cannot be parsed, yields <see langword="null" /> for the affected members
/// rather than failing the workbook read.
/// </remarks>
public sealed class Biff8WorkbookProperties
{
    /// <summary>The parsed summary-information property set, when present.</summary>
    private readonly SummaryInformation? _summary;

    /// <summary>The parsed document-summary-information property set, when present.</summary>
    private readonly DocumentSummaryInformation? _document;

    /// <summary>
    /// Initializes a new instance of the <see cref="Biff8WorkbookProperties" /> class.
    /// </summary>
    /// <param name="summary">
    /// The parsed summary-information property set, or <see langword="null" /> when absent.
    /// </param>
    /// <param name="document">
    /// The parsed document-summary-information property set, or <see langword="null" /> when absent.
    /// </param>
    internal Biff8WorkbookProperties(SummaryInformation? summary, DocumentSummaryInformation? document)
    {
        _summary = summary;
        _document = document;
    }

    /// <summary>
    /// Gets the document title.
    /// </summary>
    /// <returns>The title, or <see langword="null" /> when not recorded.</returns>
    public string? Title => _summary?.Title;

    /// <summary>
    /// Gets the document subject.
    /// </summary>
    /// <returns>The subject, or <see langword="null" /> when not recorded.</returns>
    public string? Subject => _summary?.Subject;

    /// <summary>
    /// Gets the document author.
    /// </summary>
    /// <returns>The author, or <see langword="null" /> when not recorded.</returns>
    public string? Author => _summary?.Author;

    /// <summary>
    /// Gets the document keywords.
    /// </summary>
    /// <returns>The keywords, or <see langword="null" /> when not recorded.</returns>
    public string? Keywords => _summary?.Keywords;

    /// <summary>
    /// Gets the document comments.
    /// </summary>
    /// <returns>The comments, or <see langword="null" /> when not recorded.</returns>
    public string? Comments => _summary?.Comments;

    /// <summary>
    /// Gets the name of the user who last saved the workbook.
    /// </summary>
    /// <returns>The last author, or <see langword="null" /> when not recorded.</returns>
    public string? LastSavedBy => _summary?.LastAuthor;

    /// <summary>
    /// Gets the name of the application that created the workbook.
    /// </summary>
    /// <returns>The application name, or <see langword="null" /> when not recorded.</returns>
    public string? ApplicationName => _summary?.ApplicationName;

    /// <summary>
    /// Gets the time at which the workbook was created.
    /// </summary>
    /// <returns>The creation time, or <see langword="null" /> when not recorded.</returns>
    public DateTimeOffset? Created => _summary?.CreateTime;

    /// <summary>
    /// Gets the time at which the workbook was last saved.
    /// </summary>
    /// <returns>The last-saved time, or <see langword="null" /> when not recorded.</returns>
    public DateTimeOffset? LastSaved => _summary?.LastSaveTime;

    /// <summary>
    /// Gets the time at which the workbook was last printed.
    /// </summary>
    /// <returns>The last-printed time, or <see langword="null" /> when not recorded.</returns>
    public DateTimeOffset? LastPrinted => _summary?.LastPrinted;

    /// <summary>
    /// Gets the company associated with the workbook.
    /// </summary>
    /// <returns>The company, or <see langword="null" /> when not recorded.</returns>
    public string? Company => _document?.Company;

    /// <summary>
    /// Gets the manager associated with the workbook.
    /// </summary>
    /// <returns>The manager, or <see langword="null" /> when not recorded.</returns>
    public string? Manager => _document?.Manager;

    /// <summary>
    /// Gets the document category.
    /// </summary>
    /// <returns>The category, or <see langword="null" /> when not recorded.</returns>
    public string? Category => _document?.Category;

    /// <summary>
    /// Gets the underlying summary-information property set, when present.
    /// </summary>
    /// <returns>The parsed summary information, or <see langword="null" /> when the stream is absent.</returns>
    public SummaryInformation? Summary => _summary;

    /// <summary>
    /// Gets the underlying document-summary-information property set, when present.
    /// </summary>
    /// <returns>
    /// The parsed document summary information, or <see langword="null" /> when the stream is absent.
    /// </returns>
    public DocumentSummaryInformation? Document => _document;
}
