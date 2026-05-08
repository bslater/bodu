// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NumericExtensions.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

/// <summary>
/// Provides bit-level and byte-level transformations over the integral primitive types — endian conversion, byte and word swaps,
/// bit reversal and rotation, and unchecked arithmetic helpers — for hashing, serialisation, and protocol code.
/// </summary>
/// <remarks>
/// <para>
/// Hash algorithms, network protocols, and binary file formats routinely require operations that the BCL either does not expose
/// (bit reversal, word swap), exposes only on a single type (<see cref="System.Buffers.Binary.BinaryPrimitives"/> covers
/// endianness but not rotation), or hides behind generic-math abstractions. This class collects those operations as a single,
/// type-overloaded surface so the same call site can target <see cref="byte"/>, <see cref="ushort"/>, <see cref="uint"/>, or
/// <see cref="ulong"/> without wrapping the value in a generic helper.
/// </para>
/// <para>
/// The API surface is grouped into: byte-order conversion (<c>GetBytes</c>, <c>ReverseBytes</c>), within-value rearrangement
/// (<c>ReverseWords</c>, <c>ReverseBits</c>), bit rotation (<c>RotateBitsLeft</c>, <c>RotateBitsRight</c>), and unchecked
/// arithmetic helpers under <c>Unchecked</c>. <c>ReverseBits</c> additionally accepts a <c>bitLength</c> overload for callers
/// reversing only the low-order portion of a value, and <c>GetBytes</c> accepts a flag to control endianness explicitly rather
/// than depending on host order.
/// </para>
/// <para>
/// All operations are pure, branch-light, allocation-free for the value-typed overloads, and deterministic. Where a
/// <c>byte[]</c> is returned (for example, <c>GetBytes</c> or <c>ReverseBits(byte[])</c>) a fresh array is allocated each call —
/// callers wanting buffer reuse should slice into a pre-allocated <see cref="Span{T}"/> via the BCL primitives instead.
/// </para>
/// <example>
/// <code language="csharp">
/// // Swap byte order for big-endian wire encoding.
/// uint host = 0x11_22_33_44u;
/// uint network = host.ReverseBytes();
/// // => 0x44_33_22_11
///
/// // Bit reversal — useful in CRC and DSP code.
/// byte mask = 0b1011_0001;
/// byte mirrored = mask.ReverseBits();
/// // => 0b1000_1101
///
/// // Cyclic bit rotation — common in hashing primitives.
/// uint rotated = 0x0000_00FFu.RotateBitsLeft(8);
/// // => 0x0000_FF00
/// </code>
/// </example>
/// </remarks>
public static partial class NumericExtensions
{ }
