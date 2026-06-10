// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodedInteger.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Text.Bencode;

/// <summary>
/// Represents a bencoded integer value.
/// </summary>
/// <remarks>
/// <para>
/// Bencoded integers are encoded as <c>i</c>&lt;decimal&gt;<c>e</c>, where <c>&lt;decimal&gt;</c> is an ASCII signed
/// decimal with no leading zeros (other than the literal <c>0</c>) and no leading <c>+</c>. The format itself permits
/// arbitrary-precision integers; this type narrows the supported range to <see cref="long" />, which is sufficient for
/// every value found in BitTorrent metadata and the vast majority of bencoded payloads in the wild.
/// </para>
/// <para>
/// Instances are immutable and implement value equality through <see cref="IEquatable{T}" />.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// var piece = new BencodedInteger(262144);
/// Console.WriteLine(piece.Value);                     // 262144
/// Console.WriteLine(piece.Kind);                      // Integer
/// Console.WriteLine(piece);                           // "262144"
///]]>
/// </example>
public sealed class BencodedInteger
    : BencodedValue,
    IEquatable<BencodedInteger>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BencodedInteger" /> class.
    /// </summary>
    /// <param name="value">The integer value.</param>
    public BencodedInteger(long value)
    {
        Value = value;
    }

    /// <inheritdoc />
    public override BencodedValueKind Kind => BencodedValueKind.Integer;

    /// <summary>
    /// Gets the integer value.
    /// </summary>
    public long Value { get; }

    /// <summary>
    /// Determines whether this instance and another <see cref="BencodedInteger" /> have the same value.
    /// </summary>
    /// <param name="other">The value to compare with this instance.</param>
    /// <returns><see langword="true" /> when both values are equal; otherwise, <see langword="false" />.</returns>
    public bool Equals(BencodedInteger? other) =>
        other is not null && Value == other.Value;

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is BencodedInteger other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() =>
        Value.GetHashCode();

    /// <inheritdoc />
    public override string ToString() =>
        Value.ToString(CultureInfo.InvariantCulture);
}
