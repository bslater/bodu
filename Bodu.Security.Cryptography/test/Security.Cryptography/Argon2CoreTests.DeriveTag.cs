// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2CoreTests.DeriveTag.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Infrastructure;
using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography;

public sealed partial class Argon2CoreTests
{
    /// <summary>The largest memory, in KiB, of the recorded vectors the poisoned-matrix test replays.</summary>
    private const int PoisonedVectorMaxMemoryKiB = 4096;

    /// <summary>
    /// Gets the RFC 9106 vectors and the recorded 1.0.0 vectors of up to 4 MiB: every variant, both versions, and every
    /// lane count and segment shape the corpus covers.
    /// </summary>
    /// <returns>One row per vector, each holding a single <see cref="KdfKnownAnswer" />.</returns>
    public static IEnumerable<object[]> PoisonableVectors() =>
        Argon2Tests.Rfc9106Vectors().Concat(
            Argon2Tests.RecordedCorpusVectors().Where(row => ((KdfKnownAnswer)row[0]).Memory <= PoisonedVectorMaxMemoryKiB));

    /// <summary>
    /// Verifies that a derivation over a matrix pre-filled with garbage still produces the known tag, proving that no
    /// block is read before it is written — the invariant that lets a matrix come uninitialized from native memory or
    /// all-zero from the pool.
    /// </summary>
    /// <param name="vector">The vector replayed over the poisoned matrix.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(PoisonableVectors), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void DeriveTag_WhenMatrixStartsWithGarbage_ShouldMatchKnownTag(KdfKnownAnswer vector)
    {
        var pool = new Argon2MatrixPool(0, 0, TimeSpan.FromHours(1), TimeProvider.System);
        using Argon2Matrix matrix = Argon2Matrix.Rent(MemoryBlocks(vector), pool);
        for (int block = 0; block < matrix.BlockCount; block++)
            matrix.BlockSpan(block).Fill(0xDEAD_BEEF_A5A5_5A5AUL ^ ((ulong)block * 0x9E37_79B9_7F4A_7C15UL));

        byte[] tag = new byte[vector.OutputLength];
        Argon2Core.DeriveTag(ToType(vector), ToParameters(vector), vector.Password, vector.Salt, tag, matrix);

        Assert.AreEqual(vector.ExpectedHex, Convert.ToHexString(tag).ToLowerInvariant());
    }
}
