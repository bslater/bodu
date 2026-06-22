// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DocumentSummaryInformationBuilder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound.PropertySets;

/// <summary>
/// Authors a document-summary-information property set, producing an <see cref="OlePropertySet" /> that can be embedded
/// as the <c>\x05DocumentSummaryInformation</c> stream of a compound file, including an optional user-defined section
/// of custom named properties.
/// </summary>
/// <remarks>
/// Each property is optional; only the properties that are set are written. The result reads back through
/// <see cref="DocumentSummaryInformation" />.
/// </remarks>
public sealed class DocumentSummaryInformationBuilder
{
    /// <summary>The first user-defined custom property identifier.</summary>
    private const int FirstCustomPropertyId = 2;

    /// <summary>The custom named properties, in insertion order.</summary>
    private readonly Dictionary<string, OlePropertyValue> _customProperties = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets or sets the code page used to encode string properties.
    /// </summary>
    /// <returns>The code page; <c>1252</c> by default.</returns>
    public int CodePage { get; set; } = 1252;

    /// <summary>
    /// Gets or sets the document category.
    /// </summary>
    /// <returns>The category, or <see langword="null" /> when unset.</returns>
    public string? Category { get; set; }

    /// <summary>
    /// Gets or sets the document manager.
    /// </summary>
    /// <returns>The manager, or <see langword="null" /> when unset.</returns>
    public string? Manager { get; set; }

    /// <summary>
    /// Gets or sets the document company.
    /// </summary>
    /// <returns>The company, or <see langword="null" /> when unset.</returns>
    public string? Company { get; set; }

    /// <summary>
    /// Gets or sets the line count.
    /// </summary>
    /// <returns>The line count, or <see langword="null" /> when unset.</returns>
    public int? LineCount { get; set; }

    /// <summary>
    /// Gets or sets the paragraph count.
    /// </summary>
    /// <returns>The paragraph count, or <see langword="null" /> when unset.</returns>
    public int? ParagraphCount { get; set; }

    /// <summary>
    /// Gets or sets the slide count.
    /// </summary>
    /// <returns>The slide count, or <see langword="null" /> when unset.</returns>
    public int? SlideCount { get; set; }

    /// <summary>
    /// Adds a user-defined custom property.
    /// </summary>
    /// <param name="name">The custom property name.</param>
    /// <param name="value">The custom property value.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> or <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    public void AddCustomProperty(string name, OlePropertyValue value)
    {
        ThrowHelper.ThrowIfNull(name);
        ThrowHelper.ThrowIfNull(value);

        _customProperties[name] = value;
    }

    /// <summary>
    /// Builds the property set from the assigned properties.
    /// </summary>
    /// <returns>An <see cref="OlePropertySet" /> ready to serialize.</returns>
    public OlePropertySet ToPropertySet()
    {
        var builtIn = new OlePropertySection(WellKnownFormatIds.DocumentSummaryInformation, CodePage);
        SetString(builtIn, 2, Category);
        SetString(builtIn, 14, Manager);
        SetString(builtIn, 15, Company);
        SetInt(builtIn, 5, LineCount);
        SetInt(builtIn, 6, ParagraphCount);
        SetInt(builtIn, 7, SlideCount);

        var set = new OlePropertySet(WellKnownFormatIds.DocumentSummaryInformation, Guid.Empty, CodePage);
        set.AddSection(builtIn);

        if (_customProperties.Count > 0)
            set.AddSection(BuildUserDefinedSection());

        return set;
    }

    /// <summary>
    /// Builds the property set and serializes it to its byte form.
    /// </summary>
    /// <returns>The serialized document-summary-information bytes.</returns>
    public byte[] ToArray() =>
        ToPropertySet().ToArray();

    /// <summary>
    /// Builds the property set and writes its serialized byte form to the supplied stream.
    /// </summary>
    /// <param name="stream">The stream to write the document-summary-information bytes to.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="stream" /> is <see langword="null" />.
    /// </exception>
    public void WriteTo(Stream stream)
    {
        ThrowHelper.ThrowIfNull(stream);

        ToPropertySet().WriteTo(stream);
    }

    /// <summary>
    /// Builds the user-defined section carrying the custom named properties.
    /// </summary>
    /// <returns>The user-defined <see cref="OlePropertySection" />.</returns>
    private OlePropertySection BuildUserDefinedSection()
    {
        var section = new OlePropertySection(WellKnownFormatIds.UserDefinedProperties, CodePage);
        int pid = FirstCustomPropertyId;
        foreach (KeyValuePair<string, OlePropertyValue> custom in _customProperties)
        {
            section.SetName(pid, custom.Key);
            section.Set(pid, custom.Value);
            pid++;
        }

        return section;
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
}
