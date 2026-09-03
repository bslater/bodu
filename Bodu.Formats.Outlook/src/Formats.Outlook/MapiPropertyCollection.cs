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

    /// <summary>Maps a 16-bit identifier to the index of its first property in <see cref="_properties" />.</summary>
    private readonly Dictionary<ushort, int> _firstIndexById;

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
        _firstIndexById = new Dictionary<ushort, int>();
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
                _firstIndexById.TryAdd(property.Tag.Id, _properties.Count);
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
    /// Attempts to retrieve the first property carrying a 16-bit identifier, whatever its type — the lookup for
    /// callers that know which property they want but not which wire type the writer chose for it.
    /// </summary>
    /// <param name="id">The 16-bit property identifier.</param>
    /// <param name="property">When this method returns, the first matching property when one is present.</param>
    /// <returns><see langword="true" /> when a property with the identifier is present.</returns>
    public bool TryGetValue(ushort id, [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out MapiProperty property)
    {
        if (_firstIndexById.TryGetValue(id, out int index))
        {
            property = _properties[index];
            return true;
        }

        property = null;
        return false;
    }

    /// <summary>
    /// Enumerates every property carrying a 16-bit identifier, in first-occurrence order — one per distinct type the
    /// writer stored it under.
    /// </summary>
    /// <param name="id">The 16-bit property identifier.</param>
    /// <returns>The matching properties; empty when none is present.</returns>
    public IEnumerable<MapiProperty> GetAll(ushort id)
    {
        if (!_firstIndexById.TryGetValue(id, out int first))
            yield break;

        for (int i = first; i < _properties.Count; i++)
        {
            if (_properties[i].Tag.Id == id)
                yield return _properties[i];
        }
    }

    /// <summary>
    /// Returns the string value of a property, probing the Unicode and then the code-page string type.
    /// </summary>
    /// <param name="id">The 16-bit property identifier.</param>
    /// <returns>The string value, or <see langword="null" /> when absent or not a string.</returns>
    public string? GetString(ushort id) =>
        GetValue(id, MapiPropertyType.Unicode) as string ?? GetValue(id, MapiPropertyType.String8) as string;

    /// <summary>
    /// Gets a 32-bit integer property value.
    /// </summary>
    /// <param name="id">The property identifier.</param>
    /// <returns>
    /// The <see cref="MapiPropertyType.Int32" /> value, or the <see cref="MapiPropertyType.Int16" /> value widened
    /// when the writer stored the property in the narrower type; <see langword="null" /> when neither is present.
    /// </returns>
    public int? GetInt32(ushort id) =>
        GetValue(id, MapiPropertyType.Int32) is int i32
            ? i32
            : GetValue(id, MapiPropertyType.Int16) as short?;


    /// <summary>
    /// Gets a 64-bit integer property value.
    /// </summary>
    /// <param name="id">The property identifier.</param>
    /// <returns>
    /// The <see cref="MapiPropertyType.Int64" /> value, or the <see cref="MapiPropertyType.Int32" /> or
    /// <see cref="MapiPropertyType.Int16" /> value widened when the writer stored the property in a narrower type;
    /// <see langword="null" /> when none is present.
    /// </returns>
    public long? GetInt64(ushort id) =>
        GetValue(id, MapiPropertyType.Int64) is long i64
            ? i64
            : GetValue(id, MapiPropertyType.Int32) is int i32
                ? i32
                : GetValue(id, MapiPropertyType.Int16) as short?;


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
