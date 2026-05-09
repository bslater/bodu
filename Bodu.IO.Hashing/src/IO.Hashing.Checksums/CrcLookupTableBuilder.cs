// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CrcLookupTableBuilder.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Checksums;

using Bodu.Extensions;

/// <summary>
/// Generates the precomputed lookup tables that drive the byte-at-a-time inner loop of <see cref="Crc"/>, given the
/// width, polynomial, and input-reflection mode of a <see cref="CrcStandard"/>.
/// </summary>
/// <remarks>
/// <para>
/// The naive bit-by-bit CRC algorithm is straightforward but processes only one input bit per iteration. The standard
/// production technique is to precompute a small table of partial CRCs — one entry per possible input byte under the
/// chosen polynomial — so the per-byte cost collapses to a single XOR plus a table lookup. <see cref="CrcLookupTableBuilder"/>
/// is the routine that builds that table; <see cref="Crc"/> consumes it and <see cref="CrcLookupTableCache"/> caches it
/// across instances so the construction cost is paid at most once per <c>(width, polynomial, reflectIn)</c> tuple.
/// </para>
/// <para>
/// <strong>When you would call this directly.</strong> Most callers should not — <see cref="Crc"/> resolves its lookup
/// table automatically through <see cref="Crc.GlobalCache"/>. Reach for <see cref="BuildLookupTable(int, ulong, bool)"/>
/// directly when implementing a custom CRC engine outside the <see cref="Crc"/> hierarchy, when running diagnostics
/// against a known-good table, or when populating a hand-rolled cache for an unusual polynomial that the global cache
/// is not the right home for.
/// </para>
/// <para>
/// <strong>Output shape.</strong> The returned table contains <c>1 &lt;&lt; min(size, 8)</c> entries (256 for any width
/// of 8 bits or more, smaller for sub-byte widths). Each entry is masked to <c>size</c> bits and is suitable for direct
/// XOR into a CRC register of the corresponding width. Output bit reflection (the <c>ReflectOut</c> step) is <em>not</em>
/// applied here — that is a finalisation step performed by the engine after the table-driven loop completes.
/// </para>
/// <para>The method is pure, deterministic, and allocation-bounded by the table size; results are safe to cache and share.</para>
/// </remarks>
/// <seealso cref="Crc"/>
/// <seealso cref="CrcStandard"/>
/// <seealso cref="CrcLookupTableCache"/>
public static class CrcLookupTableBuilder
{
    /// <summary>
    /// Generates a CRC lookup table for a given bit size, polynomial, and reflection mode.
    /// </summary>
    /// <param name="size">The number of bits in the CRC (e.g., 8, 16, 32, 64).</param>
    /// <param name="polynomial">The CRC polynomial represented as an unsigned integer.</param>
    /// <param name="reflectIn">
    /// If <see langword="true" />, input bytes are reflected (bit-reversed) before CRC processing; otherwise, bits are used as-is.
    /// </param>
    /// <returns>
    /// An array of <see cref="ulong" /> values representing the CRC lookup table, with <c><![CDATA[1 << min(size, 8)]]></c> entries.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="size" /> is less than 1 or
    /// greater than 64.</exception>
    /// <remarks>
    /// This method is typically used to precompute a table of CRC values for efficient byte-wise CRC calculation. The reflection
    /// setting determines whether the bits of the input byte are reversed prior to processing, which is common in some CRC variants.
    /// </remarks>
    public static ulong[] BuildLookupTable(int size, ulong polynomial, bool reflectIn)
    {
        ThrowHelper.ThrowIfOutOfRange(size, 1, 64);

        // Determine number of bits to process per lookup (typically 8 for byte-wise processing)
        int bitsPerTableEntry = size < 8 ? 1 : 8;
        int tableSize = 1 << bitsPerTableEntry;

        var table = new ulong[tableSize];
        ulong significantBitMask = 1UL << (size - 1);

        for (uint i = 0; i < tableSize; i++)
        {
            // Start with the input byte value
            ulong value = i;

            // Optionally reflect the bits of the input value
            if (reflectIn)
                value = NumericExtensions.ReverseBitsUnchecked(value, bitsPerTableEntry);

            // Left-align the value to match the CRC size
            value <<= size - bitsPerTableEntry;

            // Apply the polynomial for each bit in the byte
            for (int bit = 0; bit < bitsPerTableEntry; bit++)
            {
                bool msbSet = (value & significantBitMask) != 0;
                value = msbSet ? (value << 1) ^ polynomial : value << 1;
            }

            // Optionally reflect the result and truncate to the desired CRC size
            if (reflectIn)
                value = NumericExtensions.ReverseBitsUnchecked(value, size);

            // Mask off any bits beyond the desired CRC size
            value &= ulong.MaxValue >> (64 - size);

            table[i] = value;
        }

        return table;
    }
}
