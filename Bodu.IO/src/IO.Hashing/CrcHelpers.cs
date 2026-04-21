// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CrcHelpers.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

using System.Runtime.CompilerServices;

/// <summary>
/// Internal utility helpers shared between <see cref="Crc" /> and <see cref="CrcLookupTableBuilder" />.
/// </summary>
internal static class CrcHelpers
{
    /// <summary>
    /// Reverses the order of the least significant <paramref name="bitLength" /> bits in the specified <paramref name="value" />.
    /// </summary>
    /// <param name="value">The 64-bit unsigned integer to reflect.</param>
    /// <param name="bitLength">The number of least significant bits to reverse.</param>
    /// <returns>A <see cref="ulong" /> value whose least significant <paramref name="bitLength" /> bits have been reversed.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong ReflectBits(ulong value, int bitLength)
    {
        ulong result = 0;

        for (int i = 0; i < bitLength; i++)
        {
            result = (result << 1) | (value & 1);
            value >>= 1;
        }

        return result;
    }
}
