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
/// initialization and final complement), so the variant is not expressible as a catalogue entry; this type applies the
/// MS-PST parameters over the shared <see cref="CrcCore" /> engine source-compiled from <c>Bodu.IO.Hashing/shared</c> —
/// no package dependency, and the slicing-by-8 loop accelerates the Strict-mode page and block validation.
/// </remarks>
internal static class PstCrc
{
    /// <summary>The eight interleaved slicing-by-8 lookup tables for the reflected CRC-32 polynomial (<c>0x04C11DB7</c> in normal form, equivalent to the specification's pre-reflected <c>0xEDB88320</c>).</summary>
    private static readonly ulong[][] s_tables = CrcCore.BuildReflectedSlicingTables(32, 0x04C11DB7);

    /// <summary>
    /// Computes the checksum of a byte span.
    /// </summary>
    /// <param name="data">The bytes to sum.</param>
    /// <returns>The checksum.</returns>
    internal static uint Compute(ReadOnlySpan<byte> data) =>
        (uint)CrcCore.UpdateReflectedSlicing(data, 0, s_tables, s_tables[0], 32);
}
