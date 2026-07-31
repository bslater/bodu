// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MapiPropertyCollection.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Formats.Outlook;

/// <summary>
/// Provides a read-only, tag-addressed collection of decoded MAPI properties with typed convenience accessors.
/// </summary>
/// <remarks>
/// <para>
/// Lookup is keyed by the full 32-bit <see cref="MapiPropertyTag" /> — identifier and type together. The typed
/// accessors probe the plausible wire types for an identifier (for example, <see cref="GetString" /> probes
/// <see cref="MapiPropertyType.Unicode" /> and then <see cref="MapiPropertyType.String8" />) and return
/// <see langword="null" /> when the property is absent or its stored value is not of the requested CLR type — they
/// never throw for a missing or mismatched property.
/// </para>
/// <para>
/// When the constructor input carries the same tag more than once, the last occurrence wins. Enumeration preserves
/// first-occurrence order.
/// </para>
/// </remarks>
public sealed class MapiPropertyCollection
    : IReadOnlyCollection<MapiProperty>
{
    /// <summary>The properties in first-occurrence order.</summary>
    private readonly List<MapiProperty> _properties;

    /// <summary>Maps a raw tag value to its index in <see cref="_properties" />.</summary>
    private readonly Dictionary<uint, int> _indexByTag;

    /// <summary>
    /// Initializes a new instance of the <see cref="MapiPropertyCollection" /> class.
    /// </summary>
    /// <param name="properties">The decoded properties. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="properties" /> or one of its elements is <see langword="null" />.
    /// </exception>
    public MapiPropertyCollection(IEnumerable<MapiProperty> properties)
    {
        ThrowHelper.ThrowIfNull(properties);

        _properties = new List<MapiProperty>();
        _indexByTag = new Dictionary<uint, int>();
        foreach (MapiProperty property in properties)
        {
            ThrowHelper.ThrowIfNull(property, nameof(properties));

            if (_indexByTag.TryGetValue(property.Tag.Value, out int index))
            {
                _properties[index] = property;
            }
            else
            {
                _indexByTag.Add(property.Tag.Value, _properties.Count);
                _properties.Add(property);
            }
        }
    }

    /// <summary>
    /// Gets an empty property collection.
    /// </summary>
    /// <value>A shared collection containing no properties.</value>
    public static MapiPropertyCollection Empty { get; } = new(Array.Empty<MapiProperty>());

    /// <summary>
    /// Gets the number of properties in the collection.
    /// </summary>
    /// <value>The property count.</value>
    public int Count =>
        _properties.Count;

    /// <summary>
    /// Determines whether the collection contains a property with the given tag.
    /// </summary>
    /// <param name="tag">The full property tag to look up.</param>
    /// <returns><see langword="true" /> when a property with the tag is present.</returns>
    public bool Contains(MapiPropertyTag tag) =>
        _indexByTag.ContainsKey(tag.Value);

    /// <summary>
    /// Attempts to retrieve the property with the given tag.
    /// </summary>
    /// <param name="tag">The full property tag to look up.</param>
    /// <param name="property">When this method returns, the matching property when one is present.</param>
    /// <returns><see langword="true" /> when a property with the tag is present.</returns>
    public bool TryGetValue(MapiPropertyTag tag, [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out MapiProperty property)
    {
        if (_indexByTag.TryGetValue(tag.Value, out int index))
        {
            property = _properties[index];
            return true;
        }

        property = null;
        return false;
    }

    /// <summary>
    /// Returns the string value of a property, probing the Unicode and then the code-page string type.
    /// </summary>
    /// <param name="id">The 16-bit property identifier.</param>
    /// <returns>The string value, or <see langword="null" /> when absent or not a string.</returns>
    public string? GetString(ushort id) =>
        GetValue(id, MapiPropertyType.Unicode) as string ?? GetValue(id, MapiPropertyType.String8) as string;

    /// <summary>
    /// Returns the 32-bit integer value of a property.
    /// </summary>
    /// <param name="id">The 16-bit property identifier.</param>
    /// <returns>The value, or <see langword="null" /> when absent or not a 32-bit integer.</returns>
    public int? GetInt32(ushort id) =>
        GetValue(id, MapiPropertyType.Int32) as int?;

    /// <summary>
    /// Returns the 64-bit integer value of a property.
    /// </summary>
    /// <param name="id">The 16-bit property identifier.</param>
    /// <returns>The value, or <see langword="null" /> when absent or not a 64-bit integer.</returns>
    public long? GetInt64(ushort id) =>
        GetValue(id, MapiPropertyType.Int64) as long?;

    /// <summary>
    /// Returns the Boolean value of a property.
    /// </summary>
    /// <param name="id">The 16-bit property identifier.</param>
    /// <returns>The value, or <see langword="null" /> when absent or not a Boolean.</returns>
    public bool? GetBoolean(ushort id) =>
        GetValue(id, MapiPropertyType.Boolean) as bool?;

    /// <summary>
    /// Returns the floating-point value of a property, probing the 64-bit and then the 32-bit type.
    /// </summary>
    /// <param name="id">The 16-bit property identifier.</param>
    /// <returns>
    /// The value (a 32-bit value is widened), or <see langword="null" /> when absent or not floating-point.
    /// </returns>
    public double? GetDouble(ushort id) =>
        GetValue(id, MapiPropertyType.Double) is double d
            ? d
            : GetValue(id, MapiPropertyType.Float) as float?;

    /// <summary>
    /// Returns the time-stamp value of a property.
    /// </summary>
    /// <param name="id">The 16-bit property identifier.</param>
    /// <returns>The value, or <see langword="null" /> when absent or not a time stamp.</returns>
    public DateTimeOffset? GetDateTime(ushort id) =>
        GetValue(id, MapiPropertyType.SystemTime) as DateTimeOffset?;

    /// <summary>
    /// Returns the GUID value of a property.
    /// </summary>
    /// <param name="id">The 16-bit property identifier.</param>
    /// <returns>The value, or <see langword="null" /> when absent or not a GUID.</returns>
    public Guid? GetGuid(ushort id) =>
        GetValue(id, MapiPropertyType.Guid) as Guid?;

    /// <summary>
    /// Returns the binary value of a property.
    /// </summary>
    /// <param name="id">The 16-bit property identifier.</param>
    /// <returns>The payload bytes, or <see langword="null" /> when absent or not binary.</returns>
    public ReadOnlyMemory<byte>? GetBinary(ushort id) =>
        GetValue(id, MapiPropertyType.Binary) is byte[] bytes ? (ReadOnlyMemory<byte>?)bytes : null;

    /// <summary>
    /// Returns the multi-valued string value of a property, probing the Unicode and then the code-page string type.
    /// </summary>
    /// <param name="id">The 16-bit property identifier.</param>
    /// <returns>The string values, or <see langword="null" /> when absent or not a multi-valued string.</returns>
    public string[]? GetStringArray(ushort id) =>
        GetMultiValue(id, MapiPropertyType.Unicode) as string[] ?? GetMultiValue(id, MapiPropertyType.String8) as string[];

    /// <summary>
    /// Returns an enumerator over the properties in first-occurrence order.
    /// </summary>
    /// <returns>The property enumerator.</returns>
    public IEnumerator<MapiProperty> GetEnumerator() =>
        _properties.GetEnumerator();

    /// <summary>
    /// Returns a non-generic enumerator over the properties.
    /// </summary>
    /// <returns>The property enumerator.</returns>
    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();

    /// <summary>
    /// Returns the stored value for a single-valued tag.
    /// </summary>
    /// <param name="id">The 16-bit property identifier.</param>
    /// <param name="type">The base property type to probe.</param>
    /// <returns>The stored value, or <see langword="null" /> when the tag is absent.</returns>
    private object? GetValue(ushort id, MapiPropertyType type) =>
        TryGetValue(new MapiPropertyTag(id, type), out MapiProperty? property) ? property.Value : null;

    /// <summary>
    /// Returns the stored value for a multi-valued tag.
    /// </summary>
    /// <param name="id">The 16-bit property identifier.</param>
    /// <param name="elementType">The base element type to probe; the multi-valued flag is applied to it.</param>
    /// <returns>The stored value, or <see langword="null" /> when the tag is absent.</returns>
    private object? GetMultiValue(ushort id, MapiPropertyType elementType) =>
        TryGetValue(new MapiPropertyTag(((uint)id << 16) | (ushort)elementType | 0x1000u), out MapiProperty? property) ? property.Value : null;
}
