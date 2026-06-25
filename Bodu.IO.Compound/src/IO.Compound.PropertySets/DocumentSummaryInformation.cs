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
/// <example>
/// The following example reads the extended metadata and enumerates any user-defined custom properties.
/// <code language="csharp">
///<![CDATA[
/// using Bodu.IO.Compound;
/// using Bodu.IO.Compound.PropertySets;
///
/// using CompoundFile file = CompoundFile.OpenRead("report.doc");
/// if (file.TryGetDocumentSummaryInformation(out DocumentSummaryInformation? info))
/// {
///     Console.WriteLine($"Company:  {info.Company}");
///     Console.WriteLine($"Category: {info.Category}");
///     foreach (KeyValuePair<string, OlePropertyValue> custom in info.CustomProperties)
///         Console.WriteLine($"{custom.Key} = {custom.Value.Value}");
/// }
///]]>
/// </code>
/// </example>
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
    /// <value>The document-summary-information <see cref="OlePropertySet" />.</value>
    public OlePropertySet PropertySet => _propertySet;

    /// <summary>
    /// Gets the document category.
    /// </summary>
    /// <value>The category, or <see langword="null" /> when absent.</value>
    public string? Category => _propertySet[2]?.AsString();

    /// <summary>
    /// Gets the presentation target format.
    /// </summary>
    /// <value>The presentation target, or <see langword="null" /> when absent.</value>
    public string? PresentationTarget => _propertySet[3]?.AsString();

    /// <summary>
    /// Gets the document size in bytes.
    /// </summary>
    /// <value>The byte count, or <see langword="null" /> when absent.</value>
    public int? Bytes => _propertySet[4]?.AsInt32();

    /// <summary>
    /// Gets the line count.
    /// </summary>
    /// <value>The line count, or <see langword="null" /> when absent.</value>
    public int? LineCount => _propertySet[5]?.AsInt32();

    /// <summary>
    /// Gets the paragraph count.
    /// </summary>
    /// <value>The paragraph count, or <see langword="null" /> when absent.</value>
    public int? ParagraphCount => _propertySet[6]?.AsInt32();

    /// <summary>
    /// Gets the slide count.
    /// </summary>
    /// <value>The slide count, or <see langword="null" /> when absent.</value>
    public int? SlideCount => _propertySet[7]?.AsInt32();

    /// <summary>
    /// Gets the note count.
    /// </summary>
    /// <value>The note count, or <see langword="null" /> when absent.</value>
    public int? NoteCount => _propertySet[8]?.AsInt32();

    /// <summary>
    /// Gets the hidden-slide count.
    /// </summary>
    /// <value>The hidden-slide count, or <see langword="null" /> when absent.</value>
    public int? HiddenCount => _propertySet[9]?.AsInt32();

    /// <summary>
    /// Gets the multimedia-clip count.
    /// </summary>
    /// <value>The multimedia-clip count, or <see langword="null" /> when absent.</value>
    public int? MultimediaClipCount => _propertySet[10]?.AsInt32();

    /// <summary>
    /// Gets a value indicating whether the document is scaled to crop.
    /// </summary>
    /// <value>The scale-crop flag, or <see langword="null" /> when absent.</value>
    public bool? ScaleCrop => _propertySet[11]?.AsBoolean();

    /// <summary>
    /// Gets the document manager.
    /// </summary>
    /// <value>The manager, or <see langword="null" /> when absent.</value>
    public string? Manager => _propertySet[14]?.AsString();

    /// <summary>
    /// Gets the document company.
    /// </summary>
    /// <value>The company, or <see langword="null" /> when absent.</value>
    public string? Company => _propertySet[15]?.AsString();

    /// <summary>
    /// Gets a value indicating whether the document's links are up to date.
    /// </summary>
    /// <value>The links-up-to-date flag, or <see langword="null" /> when absent.</value>
    public bool? LinksUpToDate => _propertySet[16]?.AsBoolean();

    /// <summary>
    /// Gets the user-defined custom properties, keyed by their human-readable names.
    /// </summary>
    /// <value>
    /// A dictionary of custom property name to value drawn from the user-defined section; empty when the property set
    /// has no user-defined section.
    /// </value>
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
