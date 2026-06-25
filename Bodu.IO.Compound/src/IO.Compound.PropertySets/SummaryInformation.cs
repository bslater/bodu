// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SummaryInformation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound.PropertySets;

/// <summary>
/// Provides a strongly-typed view over the standard summary-information property set (<c>\x05SummaryInformation</c>) of
/// a compound file.
/// </summary>
/// <remarks>
/// The summary-information set carries document metadata such as the title, author, and revision times. Each property
/// is optional; a property that is absent or of an unexpected type is surfaced as <see langword="null" />.
/// <example>
/// The following example reads the standard summary-information set from a compound file and prints a few fields.
/// <code language="csharp">
///<![CDATA[
/// using Bodu.IO.Compound;
/// using Bodu.IO.Compound.PropertySets;
///
/// using CompoundFile file = CompoundFile.OpenRead("book.xls");
/// if (file.TryGetSummaryInformation(out SummaryInformation? summary))
/// {
///     Console.WriteLine($"Title:   {summary.Title}");
///     Console.WriteLine($"Author:  {summary.Author}");
///     Console.WriteLine($"Created: {summary.CreateTime:u}");
/// }
///]]>
/// </code>
/// </example>
/// </remarks>
public sealed class SummaryInformation
{
    /// <summary>The directory name of the summary-information stream, including its control prefix.</summary>
    public const string StreamName = "\u0005SummaryInformation";

    /// <summary>The underlying parsed property set.</summary>
    private readonly OlePropertySet _propertySet;

    /// <summary>
    /// Initializes a new instance of the <see cref="SummaryInformation" /> class over a parsed property set.
    /// </summary>
    /// <param name="propertySet">The parsed summary-information property set.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="propertySet" /> is <see langword="null" />.
    /// </exception>
    public SummaryInformation(OlePropertySet propertySet)
    {
        ThrowHelper.ThrowIfNull(propertySet);

        _propertySet = propertySet;
    }

    /// <summary>
    /// Reads and parses a summary-information property set from a stream, consuming it to the end.
    /// </summary>
    /// <param name="stream">The stream containing the summary-information property-set bytes.</param>
    /// <returns>A <see cref="SummaryInformation" /> view over the parsed property set.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="stream" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when the data is not a well-formed property set.
    /// </exception>
    public static SummaryInformation Read(Stream stream) =>
        new(OlePropertySet.Read(stream));

    /// <summary>
    /// Gets the underlying parsed property set.
    /// </summary>
    /// <value>The summary-information <see cref="OlePropertySet" />.</value>
    public OlePropertySet PropertySet => _propertySet;

    /// <summary>
    /// Gets the document title.
    /// </summary>
    /// <value>The title, or <see langword="null" /> when absent.</value>
    public string? Title => _propertySet[2]?.AsString();

    /// <summary>
    /// Gets the document subject.
    /// </summary>
    /// <value>The subject, or <see langword="null" /> when absent.</value>
    public string? Subject => _propertySet[3]?.AsString();

    /// <summary>
    /// Gets the document author.
    /// </summary>
    /// <value>The author, or <see langword="null" /> when absent.</value>
    public string? Author => _propertySet[4]?.AsString();

    /// <summary>
    /// Gets the document keywords.
    /// </summary>
    /// <value>The keywords, or <see langword="null" /> when absent.</value>
    public string? Keywords => _propertySet[5]?.AsString();

    /// <summary>
    /// Gets the document comments.
    /// </summary>
    /// <value>The comments, or <see langword="null" /> when absent.</value>
    public string? Comments => _propertySet[6]?.AsString();

    /// <summary>
    /// Gets the document template name.
    /// </summary>
    /// <value>The template name, or <see langword="null" /> when absent.</value>
    public string? Template => _propertySet[7]?.AsString();

    /// <summary>
    /// Gets the name of the user who last saved the document.
    /// </summary>
    /// <value>The last author, or <see langword="null" /> when absent.</value>
    public string? LastAuthor => _propertySet[8]?.AsString();

    /// <summary>
    /// Gets the document revision number.
    /// </summary>
    /// <value>The revision number, or <see langword="null" /> when absent.</value>
    public string? RevisionNumber => _propertySet[9]?.AsString();

    /// <summary>
    /// Gets the total editing time.
    /// </summary>
    /// <value>The total editing time, or <see langword="null" /> when absent.</value>
    public TimeSpan? TotalEditTime => _propertySet[10]?.AsTimeSpan();

    /// <summary>
    /// Gets the time the document was last printed.
    /// </summary>
    /// <value>The last-printed time, or <see langword="null" /> when absent.</value>
    public DateTimeOffset? LastPrinted => _propertySet[11]?.AsDateTimeOffset();

    /// <summary>
    /// Gets the time the document was created.
    /// </summary>
    /// <value>The creation time, or <see langword="null" /> when absent.</value>
    public DateTimeOffset? CreateTime => _propertySet[12]?.AsDateTimeOffset();

    /// <summary>
    /// Gets the time the document was last saved.
    /// </summary>
    /// <value>The last-saved time, or <see langword="null" /> when absent.</value>
    public DateTimeOffset? LastSaveTime => _propertySet[13]?.AsDateTimeOffset();

    /// <summary>
    /// Gets the page count.
    /// </summary>
    /// <value>The page count, or <see langword="null" /> when absent.</value>
    public int? PageCount => _propertySet[14]?.AsInt32();

    /// <summary>
    /// Gets the word count.
    /// </summary>
    /// <value>The word count, or <see langword="null" /> when absent.</value>
    public int? WordCount => _propertySet[15]?.AsInt32();

    /// <summary>
    /// Gets the character count.
    /// </summary>
    /// <value>The character count, or <see langword="null" /> when absent.</value>
    public int? CharacterCount => _propertySet[16]?.AsInt32();

    /// <summary>
    /// Gets the name of the application that created the document.
    /// </summary>
    /// <value>The application name, or <see langword="null" /> when absent.</value>
    public string? ApplicationName => _propertySet[18]?.AsString();

    /// <summary>
    /// Gets the document security flags.
    /// </summary>
    /// <value>The security value, or <see langword="null" /> when absent.</value>
    public int? Security => _propertySet[19]?.AsInt32();
}
