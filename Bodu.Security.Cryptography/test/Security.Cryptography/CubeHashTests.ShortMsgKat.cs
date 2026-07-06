// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CubeHashTests.ShortMsgKat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Infrastructure;

namespace Bodu.Security.Cryptography;

public partial class CubeHashTests
{
    // ── CubeHash SHA-3 competition ShortMsgKAT tests (CubeHash16+16/32+32) ─────────────────────
    //
    // Loaded dynamically from the embedded NIST SHA-3 Round 2 ShortMsgKAT files for CubeHash
    // (D. J. Bernstein), one per output size. These pin the standard parameterization Bodu builds by
    // default — initialization 16, per-block 16, finalization 32 rounds, 32-byte block — at 224/256/
    // 384/512-bit output. Only byte-aligned records are consumed. Bodu's non-standard round configs
    // (CubeHash80/160/300, 10-round, …) have no published vectors and are covered separately.

    /// <summary>The embedded ShortMsgKAT resource for each supported CubeHash output size.</summary>
    private static readonly (int HashSize, string Resource)[] ShortMsgKatFiles =
    [
        (224, "Bodu.Security.Cryptography.CubeHash.ShortMsgKAT_224.txt"),
        (256, "Bodu.Security.Cryptography.CubeHash.ShortMsgKAT_256.txt"),
        (384, "Bodu.Security.Cryptography.CubeHash.ShortMsgKAT_384.txt"),
        (512, "Bodu.Security.Cryptography.CubeHash.ShortMsgKAT_512.txt"),
    ];

    /// <summary>
    /// Loads every byte-aligned CubeHash ShortMsgKAT vector across all four output sizes and yields them with the
    /// output size that selects the algorithm variant.
    /// </summary>
    /// <returns>One row per vector; each row is <c>{ int hashSize, MessageDigestKnownAnswer vector }</c>.</returns>
    /// <exception cref="InvalidOperationException">An embedded ShortMsgKAT resource cannot be located.</exception>
    private static IEnumerable<object[]> CubeHashShortMsgKatVectors()
    {
        foreach ((int hashSize, string resource) in ShortMsgKatFiles)
        {
            using Stream stream = typeof(CubeHashTests).Assembly.GetManifestResourceStream(resource)
                ?? throw new InvalidOperationException(
                    $"Embedded resource '{resource}' is not present in the test assembly. " +
                    "Check the <EmbeddedResource> entry in Bodu.Security.Cryptography.Test.csproj.");

            foreach (MessageDigestKnownAnswer vector in Sha3ShortMsgKatReader.Read(stream, $"CubeHash-{hashSize}"))
                yield return new object[] { hashSize, vector };
        }
    }

    /// <summary>
    /// Produces a human-readable display name for a KAT row so failures trace back to the size and message length.
    /// </summary>
    /// <param name="methodInfo">The test method's reflection info (provided by the test runner).</param>
    /// <param name="data">The row data (<c>{ int hashSize, MessageDigestKnownAnswer vector }</c>).</param>
    /// <returns>A short label identifying this vector.</returns>
    public static string GetCubeHashKatVectorDisplayName(System.Reflection.MethodInfo methodInfo, object[] data) =>
        data[1] is MessageDigestKnownAnswer v ? v.Name : methodInfo.Name;

    /// <summary>
    /// Verifies that <see cref="CubeHash" />, in its default SHA-3-submission parameterization at the given output size,
    /// reproduces the exact digest from the NIST SHA-3 Round 2 ShortMsgKAT reference file for every byte-aligned message.
    /// </summary>
    /// <param name="hashSize">The CubeHash output size in bits (224, 256, 384, or 512).</param>
    /// <param name="vector">The ShortMsgKAT vector under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(CubeHashShortMsgKatVectors), DynamicDataDisplayName = nameof(GetCubeHashKatVectorDisplayName))]
    public void ComputeHash_WithSha3ShortMsgKatVector_ShouldMatchReferenceDigest(int hashSize, MessageDigestKnownAnswer vector)
    {
        // SHA-3 submission CubeHash16/32 = the default i = f = 10r convention: initialization 160, per-block 16,
        // 32-byte block, finalization 160 (not Bodu's faster 16/16/32 default).
        using var cubeHash = new CubeHash(initializationRounds: 160, rounds: 16, transformBlockSize: 32, finalizationRounds: 160, hashSize: hashSize);
        byte[] actual = cubeHash.ComputeHash(vector.Message);

        CollectionAssert.AreEqual(vector.Digest, actual,
            $"CubeHash-{hashSize} {vector.Name}: must match the SHA-3 ShortMsgKAT reference digest.");
    }
}
