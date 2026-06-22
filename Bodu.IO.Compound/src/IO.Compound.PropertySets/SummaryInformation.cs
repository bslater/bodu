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
    /// <returns>The summary-information <see cref="OlePropertySet" />.</returns>
    public OlePropertySet PropertySet => _propertySet;

    /// <summary>
    /// Gets the document title.
    /// </summary>
    /// <returns>The title, or <see langword="null" /> when absent.</returns>
    public string? Title => _propertySet[2]?.AsString();

    /// <summary>
    /// Gets the document subject.
    /// </summary>
    /// <returns>The subject, or <see langword="null" /> when absent.</returns>
    public string? Subject => _propertySet[3]?.AsString();

    /// <summary>
    /// Gets the document author.
    /// </summary>
    /// <returns>The author, or <see langword="null" /> when absent.</returns>
    public string? Author => _propertySet[4]?.AsString();

    /// <summary>
    /// Gets the document keywords.
    /// </summary>
    /// <returns>The keywords, or <see langword="null" /> when absent.</returns>
    public string? Keywords => _propertySet[5]?.AsString();

    /// <summary>
    /// Gets the document comments.
    /// </summary>
    /// <returns>The comments, or <see langword="null" /> when absent.</returns>
    public string? Comments => _propertySet[6]?.AsString();

    /// <summary>
    /// Gets the document template name.
    /// </summary>
    /// <returns>The template name, or <see langword="null" /> when absent.</returns>
    public string? Template => _propertySet[7]?.AsString();

    /// <summary>
    /// Gets the name of the user who last saved the document.
    /// </summary>
    /// <returns>The last author, or <see langword="null" /> when absent.</returns>
    public string? LastAuthor => _propertySet[8]?.AsString();

    /// <summary>
    /// Gets the document revision number.
    /// </summary>
    /// <returns>The revision number, or <see langword="null" /> when absent.</returns>
    public string? RevisionNumber => _propertySet[9]?.AsString();

    /// <summary>
    /// Gets the total editing time.
    /// </summary>
    /// <returns>The total editing time, or <see langword="null" /> when absent.</returns>
    public TimeSpan? TotalEditTime => _propertySet[10]?.AsTimeSpan();

    /// <summary>
    /// Gets the time the document was last printed.
    /// </summary>
    /// <returns>The last-printed time, or <see langword="null" /> when absent.</returns>
    public DateTimeOffset? LastPrinted => _propertySet[11]?.AsDateTimeOffset();

    /// <summary>
    /// Gets the time the document was created.
    /// </summary>
    /// <returns>The creation time, or <see langword="null" /> when absent.</returns>
    public DateTimeOffset? CreateTime => _propertySet[12]?.AsDateTimeOffset();

    /// <summary>
    /// Gets the time the document was last saved.
    /// </summary>
    /// <returns>The last-saved time, or <see langword="null" /> when absent.</returns>
    public DateTimeOffset? LastSaveTime => _propertySet[13]?.AsDateTimeOffset();

    /// <summary>
    /// Gets the page count.
    /// </summary>
    /// <returns>The page count, or <see langword="null" /> when absent.</returns>
    public int? PageCount => _propertySet[14]?.AsInt32();

    /// <summary>
    /// Gets the word count.
    /// </summary>
    /// <returns>The word count, or <see langword="null" /> when absent.</returns>
    public int? WordCount => _propertySet[15]?.AsInt32();

    /// <summary>
    /// Gets the character count.
    /// </summary>
    /// <returns>The character count, or <see langword="null" /> when absent.</returns>
    public int? CharacterCount => _propertySet[16]?.AsInt32();

    /// <summary>
    /// Gets the name of the application that created the document.
    /// </summary>
    /// <returns>The application name, or <see langword="null" /> when absent.</returns>
    public string? ApplicationName => _propertySet[18]?.AsString();

    /// <summary>
    /// Gets the document security flags.
    /// </summary>
    /// <returns>The security value, or <see langword="null" /> when absent.</returns>
    public int? Security => _propertySet[19]?.AsInt32();
}
