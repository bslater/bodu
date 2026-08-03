// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstCrc.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst.Internal;

/// <summary>
/// Computes the MS-PST §5.3 checksum: a table-driven reflected CRC-32 (polynomial <c>0xEDB88320</c>) with a zero
/// initial value and no final exclusive-or.
/// </summary>
/// <remarks>
/// The parameters differ from the catalogued CRC-32 in <c>Bodu.IO.Hashing</c> (which applies the standard all-ones
/// initialization and final complement), so the implementation is local — the same decision the compressed-RTF decoder
/// took for the identical checksum in MS-OXRTFCP.
/// </remarks>
internal static class PstCrc
{
    /// <summary>The CRC-32 lookup table.</summary>
    private static readonly uint[] s_table = BuildTable();

    /// <summary>
    /// Computes the checksum of a byte span.
    /// </summary>
    /// <param name="data">The bytes to sum.</param>
    /// <returns>The checksum.</returns>
    internal static uint Compute(ReadOnlySpan<byte> data)
    {
        uint crc = 0;
        foreach (byte value in data)
            crc = s_table[(crc ^ value) & 0xFF] ^ (crc >> 8);

        return crc;
    }

    /// <summary>
    /// Builds the reflected CRC-32 lookup table.
    /// </summary>
    /// <returns>The 256-entry table for polynomial <c>0xEDB88320</c>.</returns>
    private static uint[] BuildTable()
    {
        var table = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            uint entry = i;
            for (int bit = 0; bit < 8; bit++)
                entry = (entry & 1) != 0 ? 0xEDB88320 ^ (entry >> 1) : entry >> 1;

            table[i] = entry;
        }

        return table;
    }
}
