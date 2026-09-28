// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Whirlpool.Tables.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class Whirlpool
{
    /// <summary>The lazily built and cached tables for the <see cref="WhirlpoolVersion.WhirlpoolInfo1" /> variant.</summary>
    private static VariantTables? s_tablesInfo1;

    /// <summary>The lazily built and cached tables for the <see cref="WhirlpoolVersion.WhirlpoolInfo2" /> variant.</summary>
    private static VariantTables? s_tablesInfo2;

    /// <summary>The lazily built and cached tables for the <see cref="WhirlpoolVersion.WhirlpoolInfo3" /> variant.</summary>
    private static VariantTables? s_tablesInfo3;

    /// <summary>
    /// Returns the precomputed multiplication table and round keys for the requested <paramref name="version" />,
    /// constructing them on first use.
    /// </summary>
    /// <param name="version">The Whirlpool revision whose tables are required.</param>
    /// <returns>The cached <see cref="VariantTables" /> instance for <paramref name="version" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="version" /> is not a defined <see cref="WhirlpoolVersion" /> member.
    /// </exception>
    /// <remarks>
    /// Each variant owns an independent multiplication table and round-key schedule derived from its own S-box and
    /// diffusion matrix. The tables are immutable once built, so safe publication via a simple
    /// <see cref="Interlocked.CompareExchange{T}(ref T, T, T)" /> is sufficient.
    /// </remarks>
    private static VariantTables GetTables(WhirlpoolVersion version)
    {
        switch (version)
        {
            case WhirlpoolVersion.WhirlpoolInfo1:
                return s_tablesInfo1 ?? BuildAndCacheTables(ref s_tablesInfo1, s_sBoxWhirlpool0, s_mdsOriginal);

            case WhirlpoolVersion.WhirlpoolInfo2:
                return s_tablesInfo2 ?? BuildAndCacheTables(ref s_tablesInfo2, BuildMiniBoxSBox(), s_mdsOriginal);

            case WhirlpoolVersion.WhirlpoolInfo3:
                return s_tablesInfo3 ?? BuildAndCacheTables(ref s_tablesInfo3, BuildMiniBoxSBox(), s_mdsFinal);

            default:
                throw new ArgumentOutOfRangeException(nameof(version), version, null);
        }
    }

    /// <summary>
    /// Builds the multiplication table and round-key schedule for a variant and publishes the result to the supplied
    /// cache slot.
    /// </summary>
    /// <param name="slot">The static cache slot that receives the constructed tables.</param>
    /// <param name="sbox">The S-box selected for the variant.</param>
    /// <param name="mds">The diffusion row selected for the variant.</param>
    /// <returns>The freshly constructed or already-cached <see cref="VariantTables" /> instance.</returns>
    private static VariantTables BuildAndCacheTables(ref VariantTables? slot, byte[] sbox, byte[] mds)
    {
        VariantTables tables = new(BuildMultiplicationTable(sbox, mds), BuildRoundConstants(sbox));
        return Interlocked.CompareExchange(ref slot, tables, null) ?? tables;
    }

    /// <summary>
    /// Immutable carrier for the per-variant multiplication table and round constants.
    /// </summary>
    private sealed class VariantTables
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VariantTables" /> class.
        /// </summary>
        /// <param name="multiplication">The flat 8 × 256 multiplication table.</param>
        /// <param name="roundConstants">The ten round constants, column 0 of each round key.</param>
        public VariantTables(ulong[] multiplication, ulong[] roundConstants)
        {
            Multiplication = multiplication;
            RoundConstants = roundConstants;
        }

        /// <summary>
        /// Gets the flat 8 × 256 multiplication table consumed by the round function.
        /// </summary>
        /// <value>The shared multiplication table for the variant.</value>
        public ulong[] Multiplication { get; }

        /// <summary>
        /// Gets the ten round constants driving the <c>W</c> key schedule: column 0 of each round key, whose other
        /// columns are zero.
        /// </summary>
        /// <value>The shared round constants for the variant.</value>
        public ulong[] RoundConstants { get; }
    }
}
