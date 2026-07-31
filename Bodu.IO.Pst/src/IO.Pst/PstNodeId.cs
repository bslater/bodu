// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstNodeId.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.IO.Pst;

/// <summary>
/// Represents a 32-bit node identifier: five type bits in the low positions and a 27-bit index above them (MS-PST
/// §2.2.2.1).
/// </summary>
/// <remarks>
/// A handful of fixed identifiers anchor every file — <see cref="MessageStore" /> (<c>0x21</c>),
/// <see cref="NameToIdMap" /> (<c>0x61</c>), and <see cref="RootFolder" /> (<c>0x122</c>) — and are exposed here as
/// well-known values.
/// </remarks>
public readonly struct PstNodeId
    : IEquatable<PstNodeId>
{
    /// <summary>The mask selecting the five type bits.</summary>
    private const uint TypeMask = 0x1F;

    /// <summary>
    /// Initializes a new instance of the <see cref="PstNodeId" /> struct from a raw 32-bit identifier.
    /// </summary>
    /// <param name="value">The raw identifier.</param>
    public PstNodeId(uint value) =>
        Value = value;

    /// <summary>
    /// Initializes a new instance of the <see cref="PstNodeId" /> struct from a type and index.
    /// </summary>
    /// <param name="type">The node type (the five low bits).</param>
    /// <param name="index">The 27-bit index.</param>
    public PstNodeId(PstNodeType type, uint index) =>
        Value = ((uint)type & TypeMask) | (index << 5);

    /// <summary>
    /// Gets the message-store node identifier (<c>NID_MESSAGE_STORE</c>, <c>0x21</c>).
    /// </summary>
    /// <value>The well-known identifier.</value>
    public static PstNodeId MessageStore { get; } = new(0x21);

    /// <summary>
    /// Gets the named-property mapping node identifier (<c>NID_NAME_TO_ID_MAP</c>, <c>0x61</c>).
    /// </summary>
    /// <value>The well-known identifier.</value>
    public static PstNodeId NameToIdMap { get; } = new(0x61);

    /// <summary>
    /// Gets the root folder node identifier (<c>NID_ROOT_FOLDER</c>, <c>0x122</c>).
    /// </summary>
    /// <value>The well-known identifier.</value>
    public static PstNodeId RootFolder { get; } = new(0x122);

    /// <summary>
    /// Gets the raw 32-bit identifier.
    /// </summary>
    /// <value>The raw value: type in the five low bits, index above.</value>
    public uint Value { get; }

    /// <summary>
    /// Gets the node type carried in the five low bits.
    /// </summary>
    /// <value>The <see cref="PstNodeType" /> of the node.</value>
    public PstNodeType Type =>
        (PstNodeType)(Value & TypeMask);

    /// <summary>
    /// Gets the 27-bit index.
    /// </summary>
    /// <value>The index above the type bits.</value>
    public uint Index =>
        Value >> 5;

    /// <summary>
    /// Determines whether two identifiers carry the same raw value.
    /// </summary>
    /// <param name="left">The first identifier.</param>
    /// <param name="right">The second identifier.</param>
    /// <returns><see langword="true" /> when the identifiers are equal.</returns>
    public static bool operator ==(PstNodeId left, PstNodeId right) =>
        left.Equals(right);

    /// <summary>
    /// Determines whether two identifiers carry different raw values.
    /// </summary>
    /// <param name="left">The first identifier.</param>
    /// <param name="right">The second identifier.</param>
    /// <returns><see langword="true" /> when the identifiers differ.</returns>
    public static bool operator !=(PstNodeId left, PstNodeId right) =>
        !left.Equals(right);

    /// <summary>
    /// Determines whether this identifier carries the same raw value as another.
    /// </summary>
    /// <param name="other">The identifier to compare with.</param>
    /// <returns><see langword="true" /> when both carry the same <see cref="Value" />.</returns>
    public bool Equals(PstNodeId other) =>
        Value == other.Value;

    /// <summary>
    /// Determines whether this identifier equals another object.
    /// </summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns><see langword="true" /> when <paramref name="obj" /> is an equal <see cref="PstNodeId" />.</returns>
    public override bool Equals(object? obj) =>
        obj is PstNodeId other && Equals(other);

    /// <summary>
    /// Returns a hash code consistent with <see cref="Equals(PstNodeId)" />.
    /// </summary>
    /// <returns>A hash code computed over the raw value.</returns>
    public override int GetHashCode() =>
        Value.GetHashCode();

    /// <summary>
    /// Returns the canonical hexadecimal form of the identifier.
    /// </summary>
    /// <returns>The raw value as <c>0x</c> followed by eight upper-case hexadecimal digits.</returns>
    public override string ToString() =>
        "0x" + Value.ToString("X8", CultureInfo.InvariantCulture);
}
