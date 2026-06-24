// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OlePropertySection.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Bodu.IO.Compound.PropertySets;

/// <summary>
/// Represents a single section of an OLE property set — a format-identified group of properties keyed by property
/// identifier (PID).
/// </summary>
/// <remarks>
/// A property set contains one or two sections. The first carries the well-known properties (such as summary
/// information); an optional second, user-defined section carries custom properties whose human-readable names are held
/// in the section's <see cref="PropertyNames" /> dictionary.
/// </remarks>
public sealed class OlePropertySection
{
    /// <summary>The decoded properties keyed by property identifier.</summary>
    private readonly Dictionary<int, OlePropertyValue> _properties;

    /// <summary>The optional property-name dictionary keyed by property identifier.</summary>
    private readonly Dictionary<int, string> _propertyNames;

    /// <summary>
    /// Initializes a new instance of the <see cref="OlePropertySection" /> class for authoring, with no properties.
    /// </summary>
    /// <param name="formatId">The section format identifier (FMTID).</param>
    /// <param name="codePage">The code page used to encode ANSI strings in the section.</param>
    public OlePropertySection(Guid formatId, int codePage)
    {
        FormatId = formatId;
        CodePage = codePage;
        _properties = new Dictionary<int, OlePropertyValue>();
        _propertyNames = new Dictionary<int, string>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OlePropertySection" /> class.
    /// </summary>
    /// <param name="formatId">The section format identifier (FMTID).</param>
    /// <param name="codePage">The code page used to decode ANSI strings in the section.</param>
    /// <param name="properties">The decoded properties keyed by property identifier.</param>
    /// <param name="propertyNames">The optional property-name dictionary keyed by property identifier.</param>
    internal OlePropertySection(
        Guid formatId,
        int codePage,
        Dictionary<int, OlePropertyValue> properties,
        Dictionary<int, string> propertyNames)
    {
        FormatId = formatId;
        CodePage = codePage;
        _properties = properties;
        _propertyNames = propertyNames;
    }

    /// <summary>
    /// Gets the format identifier (FMTID) of the section.
    /// </summary>
    /// <value>The section's format identifier.</value>
    public Guid FormatId { get; }

    /// <summary>
    /// Gets the code page used to decode ANSI (<c>VT_LPSTR</c>) strings in the section.
    /// </summary>
    /// <value>The section code page; <c>1252</c> when none is declared.</value>
    public int CodePage { get; }

    /// <summary>
    /// Gets the decoded properties keyed by property identifier.
    /// </summary>
    /// <value>A read-only dictionary of property identifier to value.</value>
    public IReadOnlyDictionary<int, OlePropertyValue> Properties => _properties;

    /// <summary>
    /// Gets the property-name dictionary mapping property identifiers to human-readable names.
    /// </summary>
    /// <value>
    /// A read-only dictionary populated from the section's dictionary property; empty when the section declares no
    /// names.
    /// </value>
    public IReadOnlyDictionary<int, string> PropertyNames => _propertyNames;

    /// <summary>
    /// Attempts to get the value of the property with the specified identifier.
    /// </summary>
    /// <param name="propertyId">The property identifier (PID).</param>
    /// <param name="value">
    /// When this method returns <see langword="true" />, the property value; otherwise <see langword="null" />.
    /// </param>
    /// <returns><see langword="true" /> when the property exists; otherwise <see langword="false" />.</returns>
    public bool TryGetValue(int propertyId, [MaybeNullWhen(false)] out OlePropertyValue value) =>
        _properties.TryGetValue(propertyId, out value);

    /// <summary>
    /// Gets the value of the property with the specified identifier.
    /// </summary>
    /// <param name="propertyId">The property identifier (PID).</param>
    /// <returns>The property value, or <see langword="null" /> when no such property exists.</returns>
    public OlePropertyValue? this[int propertyId] =>
        _properties.TryGetValue(propertyId, out OlePropertyValue? value) ? value : null;

    /// <summary>
    /// Sets the value of the property with the specified identifier.
    /// </summary>
    /// <param name="propertyId">The property identifier (PID).</param>
    /// <param name="value">The value to assign.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    public void Set(int propertyId, OlePropertyValue value)
    {
        ThrowHelper.ThrowIfNull(value);

        _properties[propertyId] = value;
    }

    /// <summary>
    /// Assigns a human-readable name to a property identifier in this section's dictionary.
    /// </summary>
    /// <param name="propertyId">The property identifier (PID).</param>
    /// <param name="name">The human-readable name.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    public void SetName(int propertyId, string name)
    {
        ThrowHelper.ThrowIfNull(name);

        _propertyNames[propertyId] = name;
    }

    /// <summary>
    /// Removes the property with the specified identifier.
    /// </summary>
    /// <param name="propertyId">The property identifier (PID).</param>
    /// <returns><see langword="true" /> when a property was removed; otherwise <see langword="false" />.</returns>
    public bool Remove(int propertyId) =>
        _properties.Remove(propertyId);

    /// <summary>
    /// Builds a dictionary of the section's named properties, joining the property-name dictionary with the decoded
    /// values.
    /// </summary>
    /// <returns>
    /// A dictionary keyed by property name; only properties that have both a name and a value are included.
    /// </returns>
    public IReadOnlyDictionary<string, OlePropertyValue> GetNamedProperties()
    {
        Dictionary<string, OlePropertyValue> named = new(StringComparer.Ordinal);
        foreach (KeyValuePair<int, string> entry in _propertyNames)
        {
            if (_properties.TryGetValue(entry.Key, out OlePropertyValue? value))
                named[entry.Value] = value;
        }

        return named;
    }
}
