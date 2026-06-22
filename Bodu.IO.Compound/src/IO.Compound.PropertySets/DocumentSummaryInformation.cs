// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DocumentSummaryInformation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound.PropertySets;

/// <summary>
/// Provides a strongly-typed view over the document-summary-information property set (<c>\x05DocumentSummaryInformation</c>)
/// of a compound file, including any user-defined custom properties.
/// </summary>
/// <remarks>
/// The document-summary-information set carries extended document metadata such as the category, company, and slide
/// count, and may include a second, user-defined section whose properties are named by a dictionary. Each property is
/// optional; a property that is absent or of an unexpected type is surfaced as <see langword="null" />.
/// </remarks>
public sealed class DocumentSummaryInformation
{
    /// <summary>The directory name of the document-summary-information stream, including its leading control prefix.</summary>
    public const string StreamName = "\u0005DocumentSummaryInformation";

    /// <summary>The underlying parsed property set.</summary>
    private readonly OlePropertySet _propertySet;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentSummaryInformation" /> class over a parsed property set.
    /// </summary>
    /// <param name="propertySet">The parsed document-summary-information property set.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="propertySet" /> is <see langword="null" />.
    /// </exception>
    public DocumentSummaryInformation(OlePropertySet propertySet)
    {
        ThrowHelper.ThrowIfNull(propertySet);

        _propertySet = propertySet;
    }

    /// <summary>
    /// Reads and parses a document-summary-information property set from a stream, consuming it to the end.
    /// </summary>
    /// <param name="stream">The stream containing the document-summary-information property-set bytes.</param>
    /// <returns>A <see cref="DocumentSummaryInformation" /> view over the parsed property set.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="stream" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when the data is not a well-formed property set.
    /// </exception>
    public static DocumentSummaryInformation Read(Stream stream) =>
        new(OlePropertySet.Read(stream));

    /// <summary>
    /// Gets the underlying parsed property set.
    /// </summary>
    /// <returns>The document-summary-information <see cref="OlePropertySet" />.</returns>
    public OlePropertySet PropertySet => _propertySet;

    /// <summary>
    /// Gets the document category.
    /// </summary>
    /// <returns>The category, or <see langword="null" /> when absent.</returns>
    public string? Category => _propertySet[2]?.AsString();

    /// <summary>
    /// Gets the presentation target format.
    /// </summary>
    /// <returns>The presentation target, or <see langword="null" /> when absent.</returns>
    public string? PresentationTarget => _propertySet[3]?.AsString();

    /// <summary>
    /// Gets the document size in bytes.
    /// </summary>
    /// <returns>The byte count, or <see langword="null" /> when absent.</returns>
    public int? Bytes => _propertySet[4]?.AsInt32();

    /// <summary>
    /// Gets the line count.
    /// </summary>
    /// <returns>The line count, or <see langword="null" /> when absent.</returns>
    public int? LineCount => _propertySet[5]?.AsInt32();

    /// <summary>
    /// Gets the paragraph count.
    /// </summary>
    /// <returns>The paragraph count, or <see langword="null" /> when absent.</returns>
    public int? ParagraphCount => _propertySet[6]?.AsInt32();

    /// <summary>
    /// Gets the slide count.
    /// </summary>
    /// <returns>The slide count, or <see langword="null" /> when absent.</returns>
    public int? SlideCount => _propertySet[7]?.AsInt32();

    /// <summary>
    /// Gets the note count.
    /// </summary>
    /// <returns>The note count, or <see langword="null" /> when absent.</returns>
    public int? NoteCount => _propertySet[8]?.AsInt32();

    /// <summary>
    /// Gets the hidden-slide count.
    /// </summary>
    /// <returns>The hidden-slide count, or <see langword="null" /> when absent.</returns>
    public int? HiddenCount => _propertySet[9]?.AsInt32();

    /// <summary>
    /// Gets the multimedia-clip count.
    /// </summary>
    /// <returns>The multimedia-clip count, or <see langword="null" /> when absent.</returns>
    public int? MultimediaClipCount => _propertySet[10]?.AsInt32();

    /// <summary>
    /// Gets a value indicating whether the document is scaled to crop.
    /// </summary>
    /// <returns>The scale-crop flag, or <see langword="null" /> when absent.</returns>
    public bool? ScaleCrop => _propertySet[11]?.AsBoolean();

    /// <summary>
    /// Gets the document manager.
    /// </summary>
    /// <returns>The manager, or <see langword="null" /> when absent.</returns>
    public string? Manager => _propertySet[14]?.AsString();

    /// <summary>
    /// Gets the document company.
    /// </summary>
    /// <returns>The company, or <see langword="null" /> when absent.</returns>
    public string? Company => _propertySet[15]?.AsString();

    /// <summary>
    /// Gets a value indicating whether the document's links are up to date.
    /// </summary>
    /// <returns>The links-up-to-date flag, or <see langword="null" /> when absent.</returns>
    public bool? LinksUpToDate => _propertySet[16]?.AsBoolean();

    /// <summary>
    /// Gets the user-defined custom properties, keyed by their human-readable names.
    /// </summary>
    /// <returns>
    /// A dictionary of custom property name to value drawn from the user-defined section; empty when the property set
    /// has no user-defined section.
    /// </returns>
    public IReadOnlyDictionary<string, OlePropertyValue> CustomProperties
    {
        get
        {
            foreach (OlePropertySection section in _propertySet.Sections)
            {
                if (section.FormatId == WellKnownFormatIds.UserDefinedProperties)
                    return section.GetNamedProperties();
            }

            return new Dictionary<string, OlePropertyValue>(StringComparer.Ordinal);
        }
    }
}
