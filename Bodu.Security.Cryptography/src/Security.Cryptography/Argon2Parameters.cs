// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2Parameters.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Specifies the cost and auxiliary inputs for an Argon2 key-derivation or password-hashing operation, as defined by
/// RFC 9106.
/// </summary>
/// <remarks>
/// <para>
/// The three cost parameters — <see cref="MemoryKiB" />, <see cref="Iterations" />, and <see cref="Parallelism" /> —
/// have no universally safe defaults and must be chosen for the deployment. RFC 9106, Section 4 gives recommended
/// settings; a common starting point for interactive password hashing is 64 MiB of memory, three iterations, and a
/// parallelism of four.
/// </para>
/// <para>
/// The optional <see cref="Secret" /> (a server-side "pepper") and <see cref="AssociatedData" /> are folded into the
/// pre-hashing digest. They are not stored in the PHC encoded string and must be supplied again at verification time.
/// </para>
/// </remarks>
public sealed record Argon2Parameters
{
    /// <summary>
    /// The minimum supported tag length, in bytes.
    /// </summary>
    internal const int MinTagLength = 4;

    /// <summary>
    /// The exclusive upper bound on the degree of parallelism (2^24).
    /// </summary>
    internal const int MaxParallelism = (1 << 24) - 1;

    /// <summary>
    /// The Argon2 version 1.0 code (0x10).
    /// </summary>
    internal const int Version10 = 0x10;

    /// <summary>
    /// The Argon2 version 1.3 code (0x13), mandated by RFC 9106.
    /// </summary>
    internal const int Version13 = 0x13;

    /// <summary>
    /// Gets the amount of memory to fill, in kibibytes (the RFC 9106 parameter <c>m</c>).
    /// </summary>
    /// <returns>The memory size in kibibytes.</returns>
    public required int MemoryKiB { get; init; }

    /// <summary>
    /// Gets the number of passes over memory (the RFC 9106 parameter <c>t</c>).
    /// </summary>
    /// <returns>The iteration count.</returns>
    public required int Iterations { get; init; }

    /// <summary>
    /// Gets the degree of parallelism — the number of independent lanes (the RFC 9106 parameter <c>p</c>).
    /// </summary>
    /// <returns>The number of lanes.</returns>
    public required int Parallelism { get; init; }

    /// <summary>
    /// Gets the length of the derived tag, in bytes (the RFC 9106 parameter <c>T</c>).
    /// </summary>
    /// <returns>The tag length in bytes. The default is 32.</returns>
    public int TagLength { get; init; } = 32;

    /// <summary>
    /// Gets the Argon2 version code folded into the pre-hashing digest.
    /// </summary>
    /// <returns>The version code; either 0x10 or 0x13. The default is 0x13.</returns>
    public int Version { get; init; } = Version13;

    /// <summary>
    /// Gets the optional secret key <c>K</c> (a server-side pepper), or <see langword="null" /> when none is used.
    /// </summary>
    /// <returns>The secret key bytes, or <see langword="null" />.</returns>
    public byte[]? Secret { get; init; }

    /// <summary>
    /// Gets the optional associated data <c>X</c>, or <see langword="null" /> when none is used.
    /// </summary>
    /// <returns>The associated data bytes, or <see langword="null" />.</returns>
    public byte[]? AssociatedData { get; init; }

    /// <summary>
    /// Validates the cost parameters against the ranges permitted by RFC 9106, Section 3.1.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">A cost parameter falls outside its permitted range.</exception>
    /// <exception cref="ArgumentException">The version code is neither 0x10 nor 0x13.</exception>
    internal void Validate()
    {
        if (Parallelism is < 1 or > MaxParallelism)
            throw new ArgumentOutOfRangeException(
                nameof(Parallelism),
                Parallelism,
                string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Arg_OutOfRange_Argon2Parallelism, MaxParallelism));

        if (Iterations < 1)
            throw new ArgumentOutOfRangeException(
                nameof(Iterations), Iterations, CryptoResourceStrings.Arg_OutOfRange_Argon2Iterations);

        if (TagLength < MinTagLength)
            throw new ArgumentOutOfRangeException(
                nameof(TagLength),
                TagLength,
                string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Arg_OutOfRange_Argon2TagLength, MinTagLength));

        // m must be at least 8 * p kibibytes (RFC 9106 Section 3.1).
        long minMemory = 8L * Parallelism;
        if (MemoryKiB < minMemory)
            throw new ArgumentOutOfRangeException(
                nameof(MemoryKiB),
                MemoryKiB,
                string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Arg_OutOfRange_Argon2Memory, minMemory));

        if (Version is not (Version10 or Version13))
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Arg_Invalid_Argon2Version, Version.ToString("X2", CultureInfo.InvariantCulture)),
                nameof(Version));
    }
}
