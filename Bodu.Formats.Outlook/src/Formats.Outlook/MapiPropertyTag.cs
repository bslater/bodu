// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MapiPropertyTag.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Formats.Outlook;

/// <summary>
/// Represents a 32-bit MAPI property tag: a 16-bit property identifier in the high word and a 16-bit property type in
/// the low word.
/// </summary>
/// <remarks>
/// <para>
/// The tag layout follows MS-OXCDATA §2.11.4. The low word may carry the multi-valued flag (<c>0x1000</c>) in addition
/// to a base <see cref="MapiPropertyType" /> code; <see cref="Type" /> always reports the base code and
/// <see cref="IsMultiValued" /> reports the flag, while <see cref="Value" /> preserves the raw combination.
/// </para>
/// <para>
/// Identifiers at or above <c>0x8000</c> denote named properties whose meaning is defined by a
/// <see cref="MapiNamedProperty" /> mapping rather than by the identifier itself.
/// </para>
/// </remarks>
public readonly struct MapiPropertyTag
    : IEquatable<MapiPropertyTag>
{
    /// <summary>The property-type flag marking a multi-valued property.</summary>
    private const ushort MultiValuedFlag = 0x1000;

    /// <summary>The lowest property identifier reserved for named properties.</summary>
    private const ushort NamedPropertyIdFloor = 0x8000;

    /// <summary>
    /// Initializes a new instance of the <see cref="MapiPropertyTag" /> struct from a property identifier and type.
    /// </summary>
    /// <param name="id">The 16-bit property identifier.</param>
    /// <param name="type">
    /// The property type. A value combining a base code with the multi-valued flag is preserved verbatim.
    /// </param>
    public MapiPropertyTag(ushort id, MapiPropertyType type) =>
        Value = ((uint)id << 16) | (ushort)type;

    /// <summary>
    /// Initializes a new instance of the <see cref="MapiPropertyTag" /> struct from a raw 32-bit tag value.
    /// </summary>
    /// <param name="value">
    /// The raw tag: identifier in the high word, type (including any flags) in the low word.
    /// </param>
    public MapiPropertyTag(uint value) =>
        Value = value;

    /// <summary>
    /// Creates the tag of a multi-valued property from its identifier and element type.
    /// </summary>
    /// <param name="id">The 16-bit property identifier.</param>
    /// <param name="elementType">The base type of each element.</param>
    /// <returns>The tag whose type word carries <paramref name="elementType" /> and the multi-valued flag.</returns>
    public static MapiPropertyTag ForMultiValue(ushort id, MapiPropertyType elementType) =>
        new(((uint)id << 16) | (ushort)elementType | MultiValuedFlag);

    /// <summary>
    /// Gets the raw 32-bit tag value.
    /// </summary>
    /// <value>
    /// The identifier in the high word and the type word — including the multi-valued flag — in the low word.
    /// </value>
    public uint Value { get; }

    /// <summary>
    /// Gets the 16-bit property identifier.
    /// </summary>
    /// <value>The identifier from the high word of <see cref="Value" />.</value>
    public ushort Id =>
        (ushort)(Value >> 16);

    /// <summary>
    /// Gets the base property type, with the multi-valued flag stripped.
    /// </summary>
    /// <value>The base <see cref="MapiPropertyType" /> code from the low word of <see cref="Value" />.</value>
    public MapiPropertyType Type =>
        (MapiPropertyType)(Value & 0xFFFF & ~(uint)MultiValuedFlag);

    /// <summary>
    /// Gets a value indicating whether the tag denotes a multi-valued property.
    /// </summary>
    /// <value><see langword="true" /> when the type word carries the <c>0x1000</c> multi-valued flag.</value>
    public bool IsMultiValued =>
        (Value & MultiValuedFlag) != 0;

    /// <summary>
    /// Gets a value indicating whether the tag denotes a named property.
    /// </summary>
    /// <value><see langword="true" /> when <see cref="Id" /> is at or above <c>0x8000</c>.</value>
    public bool IsNamed =>
        Id >= NamedPropertyIdFloor;

    /// <summary>
    /// Determines whether two tags carry the same raw value.
    /// </summary>
    /// <param name="left">The first tag.</param>
    /// <param name="right">The second tag.</param>
    /// <returns><see langword="true" /> when the tags are equal.</returns>
    public static bool operator ==(MapiPropertyTag left, MapiPropertyTag right) =>
        left.Equals(right);

    /// <summary>
    /// Determines whether two tags carry different raw values.
    /// </summary>
    /// <param name="left">The first tag.</param>
    /// <param name="right">The second tag.</param>
    /// <returns><see langword="true" /> when the tags differ.</returns>
    public static bool operator !=(MapiPropertyTag left, MapiPropertyTag right) =>
        !left.Equals(right);

    /// <summary>
    /// Determines whether this tag carries the same raw value as another.
    /// </summary>
    /// <param name="other">The tag to compare with.</param>
    /// <returns><see langword="true" /> when both tags have the same <see cref="Value" />.</returns>
    public bool Equals(MapiPropertyTag other) =>
        Value == other.Value;

    /// <summary>
    /// Determines whether this tag equals another object.
    /// </summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="obj" /> is an equal <see cref="MapiPropertyTag" />.
    /// </returns>
    public override bool Equals(object? obj) =>
        obj is MapiPropertyTag other && Equals(other);

    /// <summary>
    /// Returns a hash code consistent with <see cref="Equals(MapiPropertyTag)" />.
    /// </summary>
    /// <returns>A hash code computed over the raw tag value.</returns>
    public override int GetHashCode() =>
        Value.GetHashCode();

    /// <summary>
    /// Returns the canonical hexadecimal form of the tag.
    /// </summary>
    /// <returns>
    /// The raw value as <c>0x</c> followed by eight upper-case hexadecimal digits, for example <c>0x0037001F</c>.
    /// </returns>
    public override string ToString() =>
        "0x" + Value.ToString("X8", CultureInfo.InvariantCulture);
}
