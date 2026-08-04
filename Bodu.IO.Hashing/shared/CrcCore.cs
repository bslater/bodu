// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CrcCore.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Runtime.CompilerServices;

using Bodu.Extensions;

#if PST
namespace Bodu.IO.Pst.Internal;
#elif MSG
namespace Bodu.Formats.Outlook.Msg;
#else
namespace Bodu.IO.Hashing.Checksums;
#endif

/// <summary>
/// Provides the shared CRC primitives — lookup-table construction and the reflected inner loops — consumed by the
/// <c>Bodu.IO.Hashing</c> CRC engine and source-compiled into the format readers that verify vendor CRC variants.
/// </summary>
/// <remarks>
/// <para>
/// This file lives in <c>Bodu.IO.Hashing/shared/</c> and is compiled into more than one assembly: it is the
/// implementation behind the public <c>CrcLookupTableBuilder</c> facade and the reflected inner loops of the <c>Crc</c>
/// engine in <c>Bodu.IO.Hashing</c>, and it is source-compiled (no package dependency) into <c>Bodu.IO.Pst</c> and
/// <c>Bodu.Formats.Outlook.Msg</c>, whose container formats specify a reflected CRC-32 with a zero initial value and no
/// final exclusive-or — parameters outside the published RevEng catalogue. The consuming project selects the namespace
/// with the <c>PST</c> / <c>MSG</c> preprocessor symbols, following the <c>Bodu.Text.Serialization/shared</c> pattern.
/// </para>
/// <para>
/// The helpers are parameter-driven and stateless: callers supply the width, polynomial, tables, initial accumulator,
/// and apply any final exclusive-or themselves. All methods are pure and safe for concurrent use.
/// </para>
/// </remarks>
internal static class CrcCore
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
    /// The reflection setting determines whether the bits of the input byte are reversed prior to processing, which is
    /// common in some CRC variants. Each entry is masked to <paramref name="size" /> bits and is suitable for direct
    /// XOR into a CRC register of the corresponding width.
    /// </remarks>
    internal static ulong[] BuildLookupTable(int size, ulong polynomial, bool reflectIn)
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
                value = value.ReverseBits(bitsPerTableEntry);

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
                value = value.ReverseBits(size);

            // Mask off any bits beyond the desired CRC size
            value &= ulong.MaxValue >> (64 - size);

            table[i] = value;
        }

        return table;
    }

    /// <summary>
    /// Builds the eight interleaved 256-entry lookup tables that drive the slicing-by-8 inner loop for a <strong>reflected</strong>
    /// CRC of the given width and polynomial.
    /// </summary>
    /// <param name="size">
    /// The CRC width, in bits. Must be a byte-aligned reflected width (32 or 64) for the slicing loop; the tables are
    /// still well-formed for other widths but <see cref="UpdateReflectedSlicing" /> only consumes 32/64.
    /// </param>
    /// <param name="polynomial">The CRC polynomial.</param>
    /// <returns>
    /// An array of eight <c>ulong[256]</c> tables. Table 0 is the ordinary byte-wise reflected table; each subsequent
    /// table <c>T_k</c> is derived from its predecessor by folding one more byte position of latency into the register.
    /// </returns>
    /// <remarks>
    /// Uses the standard reflected recurrence <c>T_k[i] = (T_{k-1}[i] &gt;&gt; 8) ^ T_0[T_{k-1}[i] &amp; 0xFF]</c>.
    /// Only valid for reflected CRCs; the non-reflected slicing recurrence differs and is intentionally not built
    /// because the engine falls back to the byte-wise loop for non-reflected standards.
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

    /// <summary>
    /// Updates <paramref name="crc" /> by feeding <paramref name="data" /> through a reflected byte-wise CRC step using
    /// the 256-entry lookup <paramref name="table" />.
    /// </summary>
    /// <param name="data">The input bytes.</param>
    /// <param name="crc">The current CRC accumulator.</param>
    /// <param name="table">The 256-entry byte-wise lookup table for the active polynomial.</param>
    /// <returns>The updated CRC accumulator.</returns>
    /// <remarks>
    /// The reflected recurrence keeps the register within the CRC width, so the loop is width-agnostic: callers supply
    /// the initial accumulator (already reflected when the standard's initial value is non-zero) and apply any final
    /// exclusive-or themselves.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong UpdateReflected(ReadOnlySpan<byte> data, ulong crc, ulong[] table)
    {
        foreach (byte b in data)
        {
            crc = (crc >> 8) ^ table[(byte)(crc ^ b)];
        }

        return crc;
    }

    /// <summary>
    /// Updates <paramref name="crc" /> by processing <paramref name="data" /> through the slicing-by-8 inner loop for a
    /// reflected 32 or 64-bit CRC, consuming eight input bytes per iteration and finishing any trailing bytes through
    /// the byte-wise reflected step.
    /// </summary>
    /// <param name="data">The input bytes to feed into the CRC accumulator.</param>
    /// <param name="crc">The CRC accumulator on entry.</param>
    /// <param name="tables">The eight interleaved slicing tables for the active reflected polynomial.</param>
    /// <param name="t0">The ordinary byte-wise reflected table (equal to <c>tables[0]</c>), used for the tail.</param>
    /// <param name="width">The CRC width in bits — either 32 or 64.</param>
    /// <returns>The CRC accumulator after consuming <paramref name="data" />.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong UpdateReflectedSlicing(ReadOnlySpan<byte> data, ulong crc, ulong[][] tables, ulong[] t0, int width)
    {
        ulong[] t1 = tables[1], t2 = tables[2], t3 = tables[3], t4 = tables[4], t5 = tables[5], t6 = tables[6], t7 = tables[7];
        int i = 0;

        if (width == 64)
        {
            while (i + 8 <= data.Length)
            {
                ulong c = crc ^ BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(i, 8));
                crc = t7[(int)(c & 0xFF)] ^ t6[(int)((c >> 8) & 0xFF)] ^ t5[(int)((c >> 16) & 0xFF)] ^ t4[(int)((c >> 24) & 0xFF)]
                    ^ t3[(int)((c >> 32) & 0xFF)] ^ t2[(int)((c >> 40) & 0xFF)] ^ t1[(int)((c >> 48) & 0xFF)] ^ t0[(int)((c >> 56) & 0xFF)];
                i += 8;
            }
        }
        else
        {
            // width == 32: only the low four bytes of the 8-byte window are folded into the 32-bit register; the
            // remaining four are indexed directly into the lower-latency tables.
            while (i + 8 <= data.Length)
            {
                uint c = (uint)crc ^ BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(i, 4));
                crc = t7[(int)(c & 0xFF)] ^ t6[(int)((c >> 8) & 0xFF)] ^ t5[(int)((c >> 16) & 0xFF)] ^ t4[(int)((c >> 24) & 0xFF)]
                    ^ t3[data[i + 4]] ^ t2[data[i + 5]] ^ t1[data[i + 6]] ^ t0[data[i + 7]];
                i += 8;
            }
        }

        for (; i < data.Length; i++)
            crc = (crc >> 8) ^ t0[(byte)(crc ^ data[i])];

        return crc;
    }
}
