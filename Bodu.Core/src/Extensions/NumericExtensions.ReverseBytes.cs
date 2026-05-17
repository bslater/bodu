// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NumericExtensions.ReverseBytes.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Bodu.Extensions;

public static partial class NumericExtensions
{

    /// <summary>
    /// Reverses the byte order of the specified <see cref="ushort" /> value.
    /// </summary>
    /// <param name="value">The 16-bit unsigned integer whose byte order is to be reversed.</param>
    /// <returns>
    /// A <see cref="ushort" /> whose bytes are in the reverse order of <paramref name="value" />, converting between
    /// big-endian and little-endian representations.
    /// </returns>
    /// <remarks>
    /// Delegates to <see cref="BinaryPrimitives.ReverseEndianness(ushort)" />, allowing the JIT to lower the operation
    /// to a single byte-swap instruction on supported platforms.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort ReverseBytes(this ushort value) =>
        BinaryPrimitives.ReverseEndianness(value);

    /// <summary>
    /// Reverses the byte order of the specified <see cref="uint" /> value.
    /// </summary>
    /// <param name="value">The 32-bit unsigned integer whose byte order is to be reversed.</param>
    /// <returns>
    /// A <see cref="uint" /> whose bytes are in the reverse order of <paramref name="value" />, converting between
    /// big-endian and little-endian representations.
    /// </returns>
    /// <remarks>
    /// Delegates to <see cref="BinaryPrimitives.ReverseEndianness(uint)" />, allowing the JIT to lower the operation to
    /// a single byte-swap instruction on supported platforms.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ReverseBytes(this uint value) =>
        BinaryPrimitives.ReverseEndianness(value);

    /// <summary>
    /// Reverses the byte order of the specified <see cref="ulong" /> value.
    /// </summary>
    /// <param name="value">The 64-bit unsigned integer whose byte order is to be reversed.</param>
    /// <returns>
    /// A <see cref="ulong" /> whose bytes are in the reverse order of <paramref name="value" />, converting between
    /// big-endian and little-endian representations.
    /// </returns>
    /// <remarks>
    /// Delegates to <see cref="BinaryPrimitives.ReverseEndianness(ulong)" />, allowing the JIT to lower the operation
    /// to a single byte-swap instruction on supported platforms.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong ReverseBytes(this ulong value) =>
        BinaryPrimitives.ReverseEndianness(value);

}
