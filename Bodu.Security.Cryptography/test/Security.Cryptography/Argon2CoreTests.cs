// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2CoreTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Infrastructure;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Tests for <see cref="Argon2Core" />, the engine behind the public Argon2 types, through its internal seams, grouped
/// into member-named partial files. The public surface and the vectors are covered by <see cref="Argon2Tests" />.
/// </summary>
[TestClass]
public sealed partial class Argon2CoreTests
{
    /// <summary>
    /// Maps a known-answer vector's variant to the engine's variant code.
    /// </summary>
    /// <param name="vector">The vector.</param>
    /// <returns>The variant the vector names.</returns>
    private static Argon2Type ToType(KdfKnownAnswer vector) => vector.Variant switch
    {
        "d" => Argon2Type.Argon2d,
        "i" => Argon2Type.Argon2i,
        _ => Argon2Type.Argon2id,
    };

    /// <summary>
    /// Builds the parameters a known-answer vector describes.
    /// </summary>
    /// <param name="vector">The vector.</param>
    /// <returns>The parameters.</returns>
    private static Argon2Parameters ToParameters(KdfKnownAnswer vector) => new()
    {
        MemoryKiB = vector.Memory,
        Iterations = vector.Iterations,
        Parallelism = vector.Parallelism,
        TagLength = vector.OutputLength,
        Version = vector.Version,
        Secret = vector.Secret,
        AssociatedData = vector.AssociatedData,
    };

    /// <summary>
    /// Returns the number of blocks, <c>m'</c>, a vector's matrix holds: its memory rounded down to a multiple of four
    /// blocks per lane.
    /// </summary>
    /// <param name="vector">The vector.</param>
    /// <returns>The number of blocks.</returns>
    private static int MemoryBlocks(KdfKnownAnswer vector) =>
        4 * vector.Parallelism * (vector.Memory / (4 * vector.Parallelism));

    /// <summary>
    /// Creates fill options that divide every slice among threads however short its segments, so the smallest vectors
    /// take the threaded path too.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">The bound on the derivation's threads.</param>
    /// <returns>The options.</returns>
    private static Argon2Core.FillOptions OnThreads(int maxDegreeOfParallelism) =>
        new(maxDegreeOfParallelism, minimumParallelSegmentLength: 1);

    /// <summary>
    /// Derives the tag a known-answer vector describes through the engine, filling the matrix as the options describe.
    /// </summary>
    /// <param name="vector">The vector whose inputs and cost parameters are derived.</param>
    /// <param name="options">How the matrix is filled.</param>
    /// <returns>The derived tag, as lowercase hex.</returns>
    private static string DeriveHex(KdfKnownAnswer vector, Argon2Core.FillOptions options)
    {
        byte[] tag = new byte[vector.OutputLength];
        Argon2Core.DeriveTag(ToType(vector), ToParameters(vector), vector.Password, vector.Salt, tag, options);
        return Convert.ToHexString(tag).ToLowerInvariant();
    }
}
