// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelWorkbookProperties.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Compound.PropertySets;

namespace Bodu.Formats.Excel;

/// <summary>
/// Exposes the flattened document properties of a workbook, read from the compound file's summary-information property
/// sets.
/// </summary>
/// <remarks>
/// The values are sourced from the <c>SummaryInformation</c> and <c>DocumentSummaryInformation</c> streams. A workbook
/// that omits a stream, or whose property set cannot be parsed, yields <see langword="null" /> for the affected members
/// rather than failing the workbook read. The lower-level property-set model is intentionally not exposed here; the
/// Excel surface presents only the flattened document fields.
/// </remarks>
public sealed class ExcelWorkbookProperties
{
    /// <summary>An empty property view used when neither property-set stream is present.</summary>
    internal static readonly ExcelWorkbookProperties s_empty = new(null, null);

    /// <summary>The parsed summary-information property set, when present.</summary>
    private readonly SummaryInformation? _summary;

    /// <summary>The parsed document-summary-information property set, when present.</summary>
    private readonly DocumentSummaryInformation? _document;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExcelWorkbookProperties" /> class.
    /// </summary>
    /// <param name="summary">
    /// The parsed summary-information property set, or <see langword="null" /> when absent.
    /// </param>
    /// <param name="document">
    /// The parsed document-summary-information property set, or <see langword="null" /> when absent.
    /// </param>
    internal ExcelWorkbookProperties(SummaryInformation? summary, DocumentSummaryInformation? document)
    {
        _summary = summary;
        _document = document;
    }

    /// <summary>
    /// Gets the document title.
    /// </summary>
    /// <value>The title, or <see langword="null" /> when not recorded.</value>
    public string? Title => _summary?.Title;

    /// <summary>
    /// Gets the document subject.
    /// </summary>
    /// <value>The subject, or <see langword="null" /> when not recorded.</value>
    public string? Subject => _summary?.Subject;

    /// <summary>
    /// Gets the document author.
    /// </summary>
    /// <value>The author, or <see langword="null" /> when not recorded.</value>
    public string? Author => _summary?.Author;

    /// <summary>
    /// Gets the document keywords.
    /// </summary>
    /// <value>The keywords, or <see langword="null" /> when not recorded.</value>
    public string? Keywords => _summary?.Keywords;

    /// <summary>
    /// Gets the document comments.
    /// </summary>
    /// <value>The comments, or <see langword="null" /> when not recorded.</value>
    public string? Comments => _summary?.Comments;

    /// <summary>
    /// Gets the name of the user who last saved the workbook.
    /// </summary>
    /// <value>The last author, or <see langword="null" /> when not recorded.</value>
    public string? LastSavedBy => _summary?.LastAuthor;

    /// <summary>
    /// Gets the name of the application that created the workbook.
    /// </summary>
    /// <value>The application name, or <see langword="null" /> when not recorded.</value>
    public string? ApplicationName => _summary?.ApplicationName;

    /// <summary>
    /// Gets the time at which the workbook was created.
    /// </summary>
    /// <value>The creation time, or <see langword="null" /> when not recorded.</value>
    public DateTimeOffset? Created => _summary?.CreateTime;

    /// <summary>
    /// Gets the time at which the workbook was last saved.
    /// </summary>
    /// <value>The last-saved time, or <see langword="null" /> when not recorded.</value>
    public DateTimeOffset? LastSaved => _summary?.LastSaveTime;

    /// <summary>
    /// Gets the time at which the workbook was last printed.
    /// </summary>
    /// <value>The last-printed time, or <see langword="null" /> when not recorded.</value>
    public DateTimeOffset? LastPrinted => _summary?.LastPrinted;

    /// <summary>
    /// Gets the company associated with the workbook.
    /// </summary>
    /// <value>The company, or <see langword="null" /> when not recorded.</value>
    public string? Company => _document?.Company;

    /// <summary>
    /// Gets the manager associated with the workbook.
    /// </summary>
    /// <value>The manager, or <see langword="null" /> when not recorded.</value>
    public string? Manager => _document?.Manager;

    /// <summary>
    /// Gets the document category.
    /// </summary>
    /// <value>The category, or <see langword="null" /> when not recorded.</value>
    public string? Category => _document?.Category;
}
