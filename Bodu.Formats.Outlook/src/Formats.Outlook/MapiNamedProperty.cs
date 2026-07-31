// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MapiNamedProperty.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook;

/// <summary>
/// Represents the identity of a MAPI named property: a property-set GUID combined with either a numeric identifier or a
/// string name.
/// </summary>
/// <remarks>
/// Named properties occupy the tag identifier range at or above <c>0x8000</c>; the identifier a given name maps to is
/// file-specific, so the durable identity is the (<see cref="PropertySetId" />, <see cref="Id" /> or
/// <see cref="Name" />) pair this type carries. Exactly one of <see cref="Id" /> and <see cref="Name" /> is set,
/// mirroring the two kinds defined by MS-OXCDATA §2.6.1.
/// </remarks>
public readonly struct MapiNamedProperty
    : IEquatable<MapiNamedProperty>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MapiNamedProperty" /> struct for a numeric named property.
    /// </summary>
    /// <param name="propertySetId">The property-set GUID that scopes the identifier.</param>
    /// <param name="id">The numeric name identifier.</param>
    public MapiNamedProperty(Guid propertySetId, uint id)
    {
        PropertySetId = propertySetId;
        Id = id;
        Name = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MapiNamedProperty" /> struct for a string named property.
    /// </summary>
    /// <param name="propertySetId">The property-set GUID that scopes the name.</param>
    /// <param name="name">The property name. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="name" /> is empty or whitespace.</exception>
    public MapiNamedProperty(Guid propertySetId, string name)
    {
        ThrowHelper.ThrowIfNull(name);
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException(OutlookResourceStrings.Arg_Invalid_MapiNamedPropertyIdentity, nameof(name));

        PropertySetId = propertySetId;
        Id = null;
        Name = name;
    }

    /// <summary>
    /// Gets the property-set GUID that scopes the name or identifier.
    /// </summary>
    /// <value>The property-set GUID.</value>
    public Guid PropertySetId { get; }

    /// <summary>
    /// Gets the numeric name identifier.
    /// </summary>
    /// <value>The identifier, or <see langword="null" /> for a string named property.</value>
    public uint? Id { get; }

    /// <summary>
    /// Gets the string name.
    /// </summary>
    /// <value>The name, or <see langword="null" /> for a numeric named property.</value>
    public string? Name { get; }

    /// <summary>
    /// Determines whether two named-property identities are equal.
    /// </summary>
    /// <param name="left">The first identity.</param>
    /// <param name="right">The second identity.</param>
    /// <returns><see langword="true" /> when the identities are equal.</returns>
    public static bool operator ==(MapiNamedProperty left, MapiNamedProperty right) =>
        left.Equals(right);

    /// <summary>
    /// Determines whether two named-property identities differ.
    /// </summary>
    /// <param name="left">The first identity.</param>
    /// <param name="right">The second identity.</param>
    /// <returns><see langword="true" /> when the identities differ.</returns>
    public static bool operator !=(MapiNamedProperty left, MapiNamedProperty right) =>
        !left.Equals(right);

    /// <summary>
    /// Determines whether this identity equals another.
    /// </summary>
    /// <param name="other">The identity to compare with.</param>
    /// <returns>
    /// <see langword="true" /> when the property-set GUIDs match and the identities carry the same identifier or the
    /// same name (ordinal comparison).
    /// </returns>
    public bool Equals(MapiNamedProperty other) =>
        PropertySetId == other.PropertySetId && Id == other.Id && string.Equals(Name, other.Name, StringComparison.Ordinal);

    /// <summary>
    /// Determines whether this identity equals another object.
    /// </summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="obj" /> is an equal <see cref="MapiNamedProperty" />.
    /// </returns>
    public override bool Equals(object? obj) =>
        obj is MapiNamedProperty other && Equals(other);

    /// <summary>
    /// Returns a hash code consistent with <see cref="Equals(MapiNamedProperty)" />.
    /// </summary>
    /// <returns>A hash code computed over the property-set GUID and the identifier or name.</returns>
    public override int GetHashCode() =>
        HashCode.Combine(PropertySetId, Id, Name is null ? 0 : StringComparer.Ordinal.GetHashCode(Name));

    /// <summary>
    /// Returns a textual form of the identity for diagnostics.
    /// </summary>
    /// <returns>The property-set GUID followed by the numeric identifier or the name.</returns>
    public override string ToString() =>
        Id is uint id
            ? $"{PropertySetId:B}:0x{id:X8}"
            : $"{PropertySetId:B}:{Name}";
}
