// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CrcLookupTableBuilder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Generates the precomputed lookup tables that drive the byte-at-a-time inner loop of <see cref="Crc" />, given the
/// width, polynomial, and input-reflection mode of a <see cref="CrcStandard" />.
/// </summary>
/// <remarks>
/// <para>
/// The naive bit-by-bit CRC algorithm is straightforward but processes only one input bit per iteration. The standard
/// production technique is to precompute a small table of partial CRCs — one entry per possible input byte under the
/// chosen polynomial — so the per-byte cost collapses to a single XOR plus a table lookup.
/// <see cref="CrcLookupTableBuilder" /> is the routine that builds that table; <see cref="Crc" /> consumes it and
/// <see cref="CrcLookupTableCache" /> caches it across instances so the construction cost is paid at most once per
/// <c>(width, polynomial, reflectIn)</c> tuple.
/// </para>
/// <para>
/// <strong>When you would call this directly.</strong> Most callers should not — <see cref="Crc" /> resolves its lookup
/// table automatically through <see cref="Crc.GlobalCache" />. Reach for
/// <see cref="BuildLookupTable(int, ulong, bool)" /> directly when implementing a custom CRC engine outside the
/// <see cref="Crc" /> hierarchy, when running diagnostics against a known-good table, or when populating a hand-rolled
/// cache for an unusual polynomial that the global cache is not the right home for.
/// </para>
/// <para>
/// <strong>Output shape.</strong> The returned table contains <c>1 &lt;&lt; min(size, 8)</c> entries (256 for any width
/// of 8 bits or more, smaller for sub-byte widths). Each entry is masked to <c>size</c> bits and is suitable for direct
/// XOR into a CRC register of the corresponding width. Output bit reflection (the <c>ReflectOut</c> step) is <em>not
/// </em> applied here — that is a finalization step performed by the engine after the table-driven loop completes.
/// </para>
/// <para>
/// The method is pure, deterministic, and allocation-bounded by the table size; results are safe to cache and share.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Build the lookup table for CRC-32/ISO-HDLC (the canonical CRC-32 used by Ethernet, PKZIP, gzip).
/// CrcStandard isoHdlc = CrcStandard.CRC32_ISOHDLC;
/// ulong[] table = CrcLookupTableBuilder.BuildLookupTable(
///     size:       isoHdlc.Size,        // 32
///     polynomial: isoHdlc.Polynomial,  // 0x04C11DB7
///     reflectIn:  isoHdlc.ReflectIn);  // true
///
/// // table has 256 entries; each value is masked to 32 bits and suitable for
/// // direct XOR into the running CRC register.
/// Console.WriteLine(table[0xFF]); // 0x...  the precomputed contribution for byte 0xFF
///]]>
/// </code>
/// </example>
/// <seealso cref="Crc"/> <seealso cref="CrcStandard"/> <seealso cref="CrcLookupTableCache"/>
public static class CrcLookupTableBuilder
{
    /// <summary>
    /// Generates a CRC lookup table for a given bit size, polynomial, and reflection mode.
    /// </summary>
    /// <param name="size">The number of bits in the CRC (e.g., 8, 16, 32, 64).</param>
    /// <param name="polynomial">The CRC polynomial represented as an unsigned integer.</param>
    /// <param name="reflectIn">
    /// If <see langword="true" />, input bytes are reflected (bit-reversed) before CRC processing; otherwise, bits are
    /// used as-is.
    /// </param>
    /// <returns>
    /// An array of <see cref="ulong" /> values representing the CRC lookup table, with
    /// <c><![CDATA[1 << min(size, 8)]]></c> entries.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="size" /> is less than 1 or greater than 64.
    /// </exception>
    /// <remarks>
    /// This method is typically used to precompute a table of CRC values for efficient byte-wise CRC calculation. The
    /// reflection setting determines whether the bits of the input byte are reversed prior to processing, which is
    /// common in some CRC variants.
    /// </remarks>
    public static ulong[] BuildLookupTable(int size, ulong polynomial, bool reflectIn)
    {
        ThrowHelper.ThrowIfOutOfRange(size, 1, 64);

        // Determine number of bits to process per lookup (typically 8 for byte-wise processing)
        int bitsPerTableEntry = size < 8 ? 1 : 8;
        int tableSize = 1 << bitsPerTableEntry;

        ulong[] table = new ulong[tableSize];
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

    /// <summary>
    /// Builds the eight interleaved 256-entry lookup tables that drive the slicing-by-8 inner loop for a
    /// <strong>reflected</strong> CRC of the given width and polynomial.
    /// </summary>
    /// <param name="size">The CRC width, in bits. Must be a byte-aligned reflected width (32 or 64) for the engine's
    /// slicing path; the tables are still well-formed for other widths but the engine only consumes 32/64.</param>
    /// <param name="polynomial">The CRC polynomial.</param>
    /// <returns>
    /// An array of eight <c>ulong[256]</c> tables. Table 0 is the ordinary byte-wise reflected table; each subsequent
    /// table <c>T_k</c> is derived from its predecessor by folding one more byte position of latency into the register.
    /// </returns>
    /// <remarks>
    /// Uses the standard reflected recurrence <c>T_k[i] = (T_{k-1}[i] >> 8) ^ T_0[T_{k-1}[i] &amp; 0xFF]</c>. Only valid
    /// for reflected CRCs; the non-reflected slicing recurrence differs and is intentionally not built because the
    /// engine falls back to the byte-wise loop for non-reflected standards.
    /// </remarks>
    internal static ulong[][] BuildReflectedSlicingTables(int size, ulong polynomial)
    {
        ulong[] t0 = BuildLookupTable(size, polynomial, reflectIn: true);
        ulong mask = size == 64 ? ulong.MaxValue : (ulong.MaxValue >> (64 - size));

        var tables = new ulong[8][];
        tables[0] = t0;

        for (int k = 1; k < 8; k++)
        {
            ulong[] prev = tables[k - 1];
            ulong[] cur = new ulong[256];
            for (int i = 0; i < 256; i++)
                cur[i] = ((prev[i] >> 8) ^ t0[(int)(prev[i] & 0xFF)]) & mask;

            tables[k] = cur;
        }

        return tables;
    }
}
