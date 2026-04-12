// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NumericExtensions.ReverseBytes.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace Bodu.Extensions;

public static partial class NumericExtensions
{
    /// <summary>
    /// Reverses the byte order of the specified <see cref="ushort" /> value.
    /// </summary>
    /// <param name="value">The 16-bit unsigned integer whose byte order is to be reversed.</param>
    /// <returns>
    /// A <see cref="ushort" /> whose bytes are in the reverse order of <paramref name="value" />, converting
    /// between big-endian and little-endian representations.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort ReverseBytes(this ushort value) =>
        (ushort)((value & 0xFFU) << 8 | (value & 0xFF00U) >> 8);

    /// <summary>
    /// Reverses the byte order of the specified <see cref="uint" /> value.
    /// </summary>
    /// <param name="value">The 32-bit unsigned integer whose byte order is to be reversed.</param>
    /// <returns>
    /// A <see cref="uint" /> whose bytes are in the reverse order of <paramref name="value" />, converting
    /// between big-endian and little-endian representations.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ReverseBytes(this uint value) =>
        (value & 0x000000FFU) << 0x18 | (value & 0x0000FF00U) << 0x08 |
        (value & 0x00FF0000U) >> 0x08 | (value & 0xFF000000U) >> 0x18;

    /// <summary>
    /// Reverses the byte order of the specified <see cref="ulong" /> value.
    /// </summary>
    /// <param name="value">The 64-bit unsigned integer whose byte order is to be reversed.</param>
    /// <returns>
    /// A <see cref="ulong" /> whose bytes are in the reverse order of <paramref name="value" />, converting
    /// between big-endian and little-endian representations.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong ReverseBytes(this ulong value) =>
        (value & 0x00000000000000FFUL) << 0x38 | (value & 0x000000000000FF00UL) << 0x28 |
        (value & 0x0000000000FF0000UL) << 0x18 | (value & 0x00000000FF000000UL) << 0x08 |
        (value & 0x000000FF00000000UL) >> 0x08 | (value & 0x0000FF0000000000UL) >> 0x18 |
        (value & 0x00FF000000000000UL) >> 0x28 | (value & 0xFF00000000000000UL) >> 0x38;
}
