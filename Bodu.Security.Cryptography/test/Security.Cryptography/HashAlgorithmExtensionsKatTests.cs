// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashAlgorithmExtensionsKatTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using Bodu.Security.Cryptography.Extensions;
using Bodu.Security.Cryptography.Infrastructure;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Drives <see cref="HashExtensionKat" /> rows against the synchronous and asynchronous extension
/// methods exposed by <see cref="HashAlgorithmExtensions" />. Each KAT row is exercised through both
/// <c>VerifyHash</c> (byte array) and <c>VerifyHashAsync</c> (stream), then a corruption probe
/// confirms that flipping a digest bit causes the verifier to reject the payload.
/// </summary>
[TestClass]
public sealed class HashAlgorithmExtensionsKatTests
{
    // SHA-256 known answers — public test vectors from FIPS 180-4 / RFC 6234.
    private static IReadOnlyList<HashExtensionKat> Kats { get; } =
    [
        new(
            Name: "SHA-256 empty",
            Input: Array.Empty<byte>(),
            ExpectedHash: Convert.FromHexString("E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855")),

        new(
            Name: "SHA-256 'abc'",
            Input: System.Text.Encoding.ASCII.GetBytes("abc"),
            ExpectedHash: Convert.FromHexString("BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD")),
    ];

    /// <summary>
    /// Verifies that every <see cref="HashExtensionKat" /> row passes
    /// <see cref="HashAlgorithmExtensions.VerifyHash(HashAlgorithm, byte[], byte[])" /> with the
    /// expected digest.
    /// </summary>
    [TestMethod]
    public void VerifyHash_Sync_WhenInputMatches_ShouldReturnTrue()
    {
        foreach (HashExtensionKat kat in Kats)
        {
            using SHA256 algorithm = SHA256.Create();
            bool verified = algorithm.VerifyHash(kat.Input, kat.ExpectedHash);

            Assert.IsTrue(verified, $"Hash-extension KAT '{kat.Name}': sync VerifyHash must succeed for matching digest.");
        }
    }

    /// <summary>
    /// Verifies that every <see cref="HashExtensionKat" /> row passes
    /// <see cref="HashAlgorithmExtensions.VerifyHashAsync(HashAlgorithm, Stream, byte[], CancellationToken)" />
    /// when the payload is streamed through a <see cref="MemoryStream" />.
    /// </summary>
    [TestMethod]
    public async Task VerifyHash_Async_WhenInputMatches_ShouldReturnTrue()
    {
        foreach (HashExtensionKat kat in Kats)
        {
            using SHA256 algorithm = SHA256.Create();
            using MemoryStream stream = new(kat.Input);

            bool verified = await algorithm.VerifyHashAsync(stream, kat.ExpectedHash).ConfigureAwait(false);

            Assert.IsTrue(verified, $"Hash-extension KAT '{kat.Name}': async VerifyHash must succeed for matching digest.");
        }
    }

    /// <summary>
    /// Verifies that flipping the first byte of every expected digest causes
    /// <see cref="HashAlgorithmExtensions.VerifyHash(HashAlgorithm, byte[], byte[])" /> to return
    /// <see langword="false" />. Documents that the verifier is genuinely load-bearing.
    /// </summary>
    [TestMethod]
    public void VerifyHash_Sync_WhenDigestIsCorrupted_ShouldReturnFalse()
    {
        foreach (HashExtensionKat kat in Kats)
        {
            using SHA256 algorithm = SHA256.Create();
            byte[] corruptedHash = (byte[])kat.ExpectedHash.Clone();
            corruptedHash[0] ^= 0x01;

            bool verified = algorithm.VerifyHash(kat.Input, corruptedHash);

            Assert.IsFalse(verified, $"Hash-extension KAT '{kat.Name}': sync VerifyHash must reject corrupted digest.");
        }
    }
}
