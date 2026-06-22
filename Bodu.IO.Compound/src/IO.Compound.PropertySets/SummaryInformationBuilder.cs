// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SummaryInformationBuilder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound.PropertySets;

/// <summary>
/// Authors a standard summary-information property set, producing an <see cref="OlePropertySet" /> that can be embedded
/// as the <c>\x05SummaryInformation</c> stream of a compound file.
/// </summary>
/// <remarks>
/// Each property is optional; only the properties that are set are written. The result reads back through
/// <see cref="SummaryInformation" />.
/// </remarks>
public sealed class SummaryInformationBuilder
{
    /// <summary>
    /// Gets or sets the code page used to encode string properties.
    /// </summary>
    /// <returns>The code page; <c>1252</c> by default.</returns>
    public int CodePage { get; set; } = 1252;

    /// <summary>
    /// Gets or sets the document title.
    /// </summary>
    /// <returns>The title, or <see langword="null" /> when unset.</returns>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the document subject.
    /// </summary>
    /// <returns>The subject, or <see langword="null" /> when unset.</returns>
    public string? Subject { get; set; }

    /// <summary>
    /// Gets or sets the document author.
    /// </summary>
    /// <returns>The author, or <see langword="null" /> when unset.</returns>
    public string? Author { get; set; }

    /// <summary>
    /// Gets or sets the document keywords.
    /// </summary>
    /// <returns>The keywords, or <see langword="null" /> when unset.</returns>
    public string? Keywords { get; set; }

    /// <summary>
    /// Gets or sets the document comments.
    /// </summary>
    /// <returns>The comments, or <see langword="null" /> when unset.</returns>
    public string? Comments { get; set; }

    /// <summary>
    /// Gets or sets the name of the user who last saved the document.
    /// </summary>
    /// <returns>The last author, or <see langword="null" /> when unset.</returns>
    public string? LastAuthor { get; set; }

    /// <summary>
    /// Gets or sets the document revision number.
    /// </summary>
    /// <returns>The revision number, or <see langword="null" /> when unset.</returns>
    public string? RevisionNumber { get; set; }

    /// <summary>
    /// Gets or sets the name of the application that created the document.
    /// </summary>
    /// <returns>The application name, or <see langword="null" /> when unset.</returns>
    public string? ApplicationName { get; set; }

    /// <summary>
    /// Gets or sets the time the document was created.
    /// </summary>
    /// <returns>The creation time, or <see langword="null" /> when unset.</returns>
    public DateTimeOffset? CreateTime { get; set; }

    /// <summary>
    /// Gets or sets the time the document was last saved.
    /// </summary>
    /// <returns>The last-saved time, or <see langword="null" /> when unset.</returns>
    public DateTimeOffset? LastSaveTime { get; set; }

    /// <summary>
    /// Gets or sets the page count.
    /// </summary>
    /// <returns>The page count, or <see langword="null" /> when unset.</returns>
    public int? PageCount { get; set; }

    /// <summary>
    /// Gets or sets the word count.
    /// </summary>
    /// <returns>The word count, or <see langword="null" /> when unset.</returns>
    public int? WordCount { get; set; }

    /// <summary>
    /// Gets or sets the character count.
    /// </summary>
    /// <returns>The character count, or <see langword="null" /> when unset.</returns>
    public int? CharacterCount { get; set; }

    /// <summary>
    /// Builds the property set from the assigned properties.
    /// </summary>
    /// <returns>An <see cref="OlePropertySet" /> ready to serialize.</returns>
    public OlePropertySet ToPropertySet()
    {
        var section = new OlePropertySection(WellKnownFormatIds.SummaryInformation, CodePage);
        SetString(section, 2, Title);
        SetString(section, 3, Subject);
        SetString(section, 4, Author);
        SetString(section, 5, Keywords);
        SetString(section, 6, Comments);
        SetString(section, 8, LastAuthor);
        SetString(section, 9, RevisionNumber);
        SetString(section, 18, ApplicationName);
        SetDate(section, 12, CreateTime);
        SetDate(section, 13, LastSaveTime);
        SetInt(section, 14, PageCount);
        SetInt(section, 15, WordCount);
        SetInt(section, 16, CharacterCount);

        var set = new OlePropertySet(WellKnownFormatIds.SummaryInformation, Guid.Empty, CodePage);
        set.AddSection(section);
        return set;
    }

    /// <summary>
    /// Builds the property set and serializes it to its byte form.
    /// </summary>
    /// <returns>The serialized summary-information bytes.</returns>
    public byte[] ToArray() =>
        ToPropertySet().ToArray();

    /// <summary>
    /// Builds the property set and writes its serialized byte form to the supplied stream.
    /// </summary>
    /// <param name="stream">The stream to write the summary-information bytes to.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="stream" /> is <see langword="null" />.
    /// </exception>
    public void WriteTo(Stream stream)
    {
        ThrowHelper.ThrowIfNull(stream);

        ToPropertySet().WriteTo(stream);
    }

    /// <summary>
    /// Sets a string property when the value is present.
    /// </summary>
    /// <param name="section">The section to populate.</param>
    /// <param name="pid">The property identifier.</param>
    /// <param name="value">The optional string value.</param>
    private static void SetString(OlePropertySection section, int pid, string? value)
    {
        if (value is not null)
            section.Set(pid, OlePropertyValue.Create(value));
    }

    /// <summary>
    /// Sets an integer property when the value is present.
    /// </summary>
    /// <param name="section">The section to populate.</param>
    /// <param name="pid">The property identifier.</param>
    /// <param name="value">The optional integer value.</param>
    private static void SetInt(OlePropertySection section, int pid, int? value)
    {
        if (value is { } v)
            section.Set(pid, OlePropertyValue.Create(v));
    }

    /// <summary>
    /// Sets a date property when the value is present.
    /// </summary>
    /// <param name="section">The section to populate.</param>
    /// <param name="pid">The property identifier.</param>
    /// <param name="value">The optional date value.</param>
    private static void SetDate(OlePropertySection section, int pid, DateTimeOffset? value)
    {
        if (value is { } v)
            section.Set(pid, OlePropertyValue.Create(v));
    }
}
