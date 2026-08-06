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
/// The expected <see cref="CompoundFileError" /> when the reader rejects the fixture while walking its directory
/// metadata, or <see langword="null" /> when the reader tolerates the defect and opens the fixture.
/// </param>
/// <param name="PayloadCategory">
/// The expected <see cref="CompoundFileError" /> when every stream payload is also read, or <see langword="null" />
/// when the fixture reads through cleanly. Defaults to <see cref="Category" />: a fixture rejected during the
/// metadata walk is rejected the same way before any payload is reached.
/// </param>
/// <param name="MinimalCategory">
/// The expected <see cref="CompoundFileError" /> when the fixture is opened and fully read at
/// <see cref="CompoundValidationLevel.Minimal" />, or <see langword="null" /> when the tolerant level recovers.
/// </param>
/// <remarks>
/// The three columns are three distinct contracts, and a fixture can pass one and fail another. A defect in a
/// directory entry surfaces during the metadata walk; a defect in a sector chain surfaces only once the payload it
/// describes is actually read; and the tolerant level is expected to recover from a strict subset of both.
/// </remarks>
public sealed record CompoundMalformedKat(
    string RelativePath,
    CompoundFileError? Category,
    CompoundFileError? PayloadCategory = null,
    CompoundFileError? MinimalCategory = null)
    : IKat
{
    /// <inheritdoc />
    public string Name => RelativePath["invalid/".Length..];

    /// <summary>
    /// Gets the category expected when every stream payload is read at the default validation level.
    /// </summary>
    /// <value>
    /// <see cref="PayloadCategory" /> when set; otherwise <see cref="Category" />, since a fixture rejected during
    /// the metadata walk never reaches a payload.
    /// </value>
    public CompoundFileError? ExpectedPayloadCategory => PayloadCategory ?? Category;
}

/// <summary>
/// Catalogues the malformed reference fixtures and the reader's expected handling of each, used to prove that broken
/// input is always handled safely — rejected with a stable category or tolerated, never crashing or hanging.
/// </summary>
internal static class CompoundMalformedFixtures
{
    /// <summary>The complete malformed-fixture catalogue.</summary>
    /// <remarks>
    /// The header defects are unconditional - a container whose signature, byte order or sector size cannot be
    /// interpreted has no recoverable reading, so every level rejects them alike. The same holds for a sector
    /// identifier that points outside the file and for a root entry that is not a root storage: those are checked
    /// before the validation level is consulted. Everything else is a judgement call the level decides, which is why
    /// the chain-cycle and directory-cycle fixtures recover at Minimal and the header ones do not.
    /// </remarks>
    private static readonly CompoundMalformedKat[] s_all =
    [
        // Hard-rejected: the reader detects the structural defect and throws a categorized exception.
        new("invalid/invalid_magic.dat", CompoundFileError.InvalidSignature, MinimalCategory: CompoundFileError.InvalidSignature),
        new("invalid/invalid_bom.dat", CompoundFileError.InvalidByteOrder, MinimalCategory: CompoundFileError.InvalidByteOrder),
        new("invalid/invalid_big_endian_bom.dat", CompoundFileError.InvalidByteOrder, MinimalCategory: CompoundFileError.InvalidByteOrder),
        new("invalid/invalid_sector_size.dat", CompoundFileError.InvalidSectorSize, MinimalCategory: CompoundFileError.InvalidSectorSize),
        new("invalid/strange_sector_size_v3.dat", CompoundFileError.InvalidSectorSize, MinimalCategory: CompoundFileError.InvalidSectorSize),
        new("invalid/strange_mini_sector_size.dat", CompoundFileError.InvalidSectorSize, MinimalCategory: CompoundFileError.InvalidSectorSize),
        new("invalid/invalid_truncated.dat", CompoundFileError.SectorOutOfRange, MinimalCategory: CompoundFileError.SectorOutOfRange),
        new("invalid/invalid_master_eof.dat", CompoundFileError.SectorOutOfRange, MinimalCategory: CompoundFileError.SectorOutOfRange),
        new("invalid/invalid_master_ext_eof.dat", CompoundFileError.SectorOutOfRange, MinimalCategory: CompoundFileError.SectorOutOfRange),
        new("invalid/invalid_mini_eof.dat", CompoundFileError.SectorOutOfRange, MinimalCategory: CompoundFileError.SectorOutOfRange),
        new("invalid/invalid_root_type.dat", CompoundFileError.InvalidRootStorage, MinimalCategory: CompoundFileError.InvalidRootStorage),

        // Rejected at the default level, recovered at Minimal: the defect is a chain or tree the reader can prune.
        new("invalid/invalid_fat_loop.dat", CompoundFileError.FatCycle),
        new("invalid/invalid_master_loop.dat", CompoundFileError.InvalidDifat),
        new("invalid/invalid_mini_free.dat", CompoundFileError.SectorOutOfRange),
        new("invalid/invalid_dir_loop.dat", CompoundFileError.DirectoryCycle),
        new("invalid/invalid_dir_indexes2.dat", CompoundFileError.DirectoryCycle),
        new("invalid/invalid_dir_indexes3.dat", CompoundFileError.DirectoryCycle),

        // Tolerated during the metadata walk, but the declared stream sizes outrun their chains, so reading the
        // payloads at the default level is rejected. Minimal zero-pads the short chain instead.
        new("invalid/invalid_dir_size1.dat", null, PayloadCategory: CompoundFileError.StreamChainTooShort),
        new("invalid/invalid_dir_size2.dat", null, PayloadCategory: CompoundFileError.StreamChainTooShort),

        // Tolerated: the defect is recoverable, so the reader opens the fixture and reading it is still safe.
        new("invalid/invalid_dir_indexes1.dat", null),
        new("invalid/invalid_dir_misc.dat", null),
        new("invalid/invalid_dir_sector_count.dat", null),
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

    /// <summary>
    /// Gets the fixtures whose payloads can be read through at the default validation level.
    /// </summary>
    /// <returns>A sequence of single-element argument arrays wrapping readable-fixture rows.</returns>
    public static IEnumerable<object[]> PayloadReadableFixtures()
    {
        foreach (CompoundMalformedKat kat in s_all)
        {
            if (kat.ExpectedPayloadCategory is null)
                yield return [kat];
        }
    }

    /// <summary>
    /// Gets the fixtures rejected once their payloads are read at the default validation level.
    /// </summary>
    /// <returns>A sequence of single-element argument arrays wrapping payload-rejected rows.</returns>
    public static IEnumerable<object[]> PayloadRejectedFixtures()
    {
        foreach (CompoundMalformedKat kat in s_all)
        {
            if (kat.ExpectedPayloadCategory is not null)
                yield return [kat];
        }
    }

    /// <summary>
    /// Gets the fixtures the tolerant level recovers, as <c>[DynamicData]</c> argument arrays.
    /// </summary>
    /// <returns>A sequence of single-element argument arrays wrapping Minimal-recoverable rows.</returns>
    public static IEnumerable<object[]> MinimalRecoveredFixtures()
    {
        foreach (CompoundMalformedKat kat in s_all)
        {
            if (kat.MinimalCategory is null)
                yield return [kat];
        }
    }

    /// <summary>
    /// Gets the fixtures the tolerant level still rejects, as <c>[DynamicData]</c> argument arrays.
    /// </summary>
    /// <returns>A sequence of single-element argument arrays wrapping Minimal-rejected rows.</returns>
    public static IEnumerable<object[]> MinimalRejectedFixtures()
    {
        foreach (CompoundMalformedKat kat in s_all)
        {
            if (kat.MinimalCategory is not null)
                yield return [kat];
        }
    }
}
