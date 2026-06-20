// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundMalformedFixtures.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.IO.Compound;

/// <summary>
/// A known-answer row describing the reader's expected handling of one malformed reference fixture.
/// </summary>
/// <param name="RelativePath">The corpus-relative fixture path, for example <c>invalid/invalid_magic.dat</c>.</param>
/// <param name="Category">
/// The expected <see cref="CompoundFileError" /> when the reader rejects the fixture, or <see langword="null" /> when the
/// reader tolerates the defect and opens the fixture.
/// </param>
public sealed record CompoundMalformedKat(string RelativePath, CompoundFileError? Category)
    : IKat
{
    /// <inheritdoc />
    public string Name => RelativePath["invalid/".Length..];
}

/// <summary>
/// Catalogues the malformed reference fixtures and the reader's expected handling of each, used to prove that broken
/// input is always handled safely — rejected with a stable category or tolerated, never crashing or hanging.
/// </summary>
internal static class CompoundMalformedFixtures
{
    /// <summary>The complete malformed-fixture catalogue.</summary>
    private static readonly CompoundMalformedKat[] s_all =
    [
        // Hard-rejected: the reader detects the structural defect and throws a categorized exception.
        new("invalid/invalid_magic.dat", CompoundFileError.InvalidSignature),
        new("invalid/invalid_bom.dat", CompoundFileError.InvalidByteOrder),
        new("invalid/invalid_big_endian_bom.dat", CompoundFileError.InvalidByteOrder),
        new("invalid/invalid_sector_size.dat", CompoundFileError.InvalidSectorSize),
        new("invalid/strange_sector_size_v3.dat", CompoundFileError.InvalidSectorSize),
        new("invalid/strange_mini_sector_size.dat", CompoundFileError.InvalidSectorSize),
        new("invalid/invalid_truncated.dat", CompoundFileError.SectorOutOfRange),
        new("invalid/invalid_fat_loop.dat", CompoundFileError.FatCycle),
        new("invalid/invalid_master_loop.dat", CompoundFileError.InvalidDifat),
        new("invalid/invalid_master_eof.dat", CompoundFileError.SectorOutOfRange),
        new("invalid/invalid_master_ext_eof.dat", CompoundFileError.SectorOutOfRange),
        new("invalid/invalid_mini_eof.dat", CompoundFileError.SectorOutOfRange),
        new("invalid/invalid_mini_free.dat", CompoundFileError.SectorOutOfRange),
        new("invalid/invalid_dir_loop.dat", CompoundFileError.DirectoryCycle),
        new("invalid/invalid_dir_indexes2.dat", CompoundFileError.DirectoryCycle),
        new("invalid/invalid_dir_indexes3.dat", CompoundFileError.DirectoryCycle),
        new("invalid/invalid_root_type.dat", CompoundFileError.InvalidRootStorage),

        // Tolerated: the defect is recoverable, so the reader opens the fixture and enumeration is still safe.
        new("invalid/invalid_dir_indexes1.dat", null),
        new("invalid/invalid_dir_misc.dat", null),
        new("invalid/invalid_dir_sector_count.dat", null),
        new("invalid/invalid_dir_size1.dat", null),
        new("invalid/invalid_dir_size2.dat", null),
        new("invalid/invalid_fat_len.dat", null),
        new("invalid/invalid_fat_types.dat", null),
        new("invalid/invalid_header_misc.dat", null),
        new("invalid/invalid_master_ext_count.dat", null),
        new("invalid/invalid_master_ext_free.dat", null),
        new("invalid/invalid_master_overrun.dat", null),
        new("invalid/invalid_master_special.dat", null),
        new("invalid/invalid_master_underrun.dat", null),
        new("invalid/invalid_name1.dat", null),
        new("invalid/invalid_name2.dat", null),
        new("invalid/invalid_stream_type.dat", null),
    ];

    /// <summary>
    /// Gets the fixtures the reader rejects, as <c>[DynamicData]</c> argument arrays.
    /// </summary>
    /// <returns>A sequence of single-element argument arrays wrapping rejected-fixture rows.</returns>
    public static IEnumerable<object[]> RejectedFixtures()
    {
        foreach (CompoundMalformedKat kat in s_all)
        {
            if (kat.Category is not null)
                yield return [kat];
        }
    }

    /// <summary>
    /// Gets the fixtures the reader tolerates, as <c>[DynamicData]</c> argument arrays.
    /// </summary>
    /// <returns>A sequence of single-element argument arrays wrapping tolerated-fixture rows.</returns>
    public static IEnumerable<object[]> ToleratedFixtures()
    {
        foreach (CompoundMalformedKat kat in s_all)
        {
            if (kat.Category is null)
                yield return [kat];
        }
    }
}
