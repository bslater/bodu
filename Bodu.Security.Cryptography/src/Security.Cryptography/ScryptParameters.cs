// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ScryptParameters.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Specifies the cost parameters for a scrypt key-derivation or password-hashing operation, as defined by RFC 7914.
/// </summary>
/// <remarks>
/// The peak memory cost of scrypt is approximately <c>128 * N * r</c> bytes. RFC 7914, Section 2 suggests
/// <c>N = 16384</c>, <c>r = 8</c>, <c>p = 1</c> for interactive logins (about 16 MiB), scaling <c>N</c> up for more
/// sensitive applications.
/// </remarks>
public sealed record ScryptParameters
{
    /// <summary>
    /// Gets the CPU/memory cost parameter <c>N</c> — a power of two greater than one.
    /// </summary>
    /// <returns>The cost parameter <c>N</c>.</returns>
    public required int CostN { get; init; }

    /// <summary>
    /// Gets the block-size parameter <c>r</c>, which tunes memory usage relative to <see cref="CostN" />.
    /// </summary>
    /// <returns>The block-size parameter <c>r</c>.</returns>
    public required int BlockSizeR { get; init; }

    /// <summary>
    /// Gets the parallelization parameter <c>p</c>.
    /// </summary>
    /// <returns>The parallelization parameter <c>p</c>.</returns>
    public required int Parallelization { get; init; }

    /// <summary>
    /// Validates the cost parameters against the constraints of RFC 7914, Section 6.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// A cost parameter falls outside its permitted range, or <see cref="CostN" /> is not a power of two greater than
    /// one.
    /// </exception>
    internal void Validate()
    {
        if (BlockSizeR < 1)
            throw new ArgumentOutOfRangeException(
                nameof(BlockSizeR), BlockSizeR, CryptoResourceStrings.Arg_OutOfRange_ScryptBlockSize);

        if (CostN <= 1 || (CostN & (CostN - 1)) != 0)
            throw new ArgumentOutOfRangeException(
                nameof(CostN), CostN, CryptoResourceStrings.Arg_OutOfRange_ScryptCostNotPowerOfTwo);

        // p <= ((2^32 - 1) * 32) / (128 * r) (RFC 7914 Section 6).
        long maxParallelization = ((long)uint.MaxValue * 32) / (128L * BlockSizeR);
        if (Parallelization < 1 || Parallelization > maxParallelization)
            throw new ArgumentOutOfRangeException(
                nameof(Parallelization),
                Parallelization,
                string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Arg_OutOfRange_ScryptParallelization, maxParallelization));
    }
}
