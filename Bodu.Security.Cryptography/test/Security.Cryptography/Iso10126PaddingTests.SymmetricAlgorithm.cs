// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Iso10126PaddingTests.SymmetricAlgorithm.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// ISO 10126 round-trip tests driven across every concrete <see cref="SymmetricAlgorithm" />
/// declared in the library. ISO 10126 is a length-recoverable padding scheme — every input
/// (including empty and already-block-aligned) produces ciphertext that is one block longer
/// than the plaintext, padded out with random bytes followed by a length byte. Because the
/// interior pad bytes are random, the only round-trip guarantee is that decrypt strips
/// exactly the number of bytes encoded in the trailing length byte.
/// </summary>
public sealed partial class Iso10126PaddingTests
{
    /// <summary>
    /// Verifies that an empty plaintext encrypted through a <see cref="CryptoStream" /> under
    /// <see cref="PaddingMode.ISO10126" /> produces exactly one block of ciphertext (the
    /// all-pad block) and round-trips back to an empty plaintext on decrypt.
    /// </summary>
    /// <param name="algorithmType">The concrete <see cref="SymmetricAlgorithm" /> type under test.</param>
    [TestMethod]
    [DynamicData(nameof(SymmetricAlgorithmTestDataSource.SymmetricAlgorithmTestData), typeof(SymmetricAlgorithmTestDataSource), DynamicDataDisplayName = nameof(SymmetricAlgorithmTestDataSource.GetSymmetricAlgorithmDisplayName), DynamicDataDisplayNameDeclaringType = typeof(SymmetricAlgorithmTestDataSource))]
    public void CryptoStream_WhenEmptyPlaintext_ShouldEmitOnePaddedBlockAndRoundTrip(System.Type algorithmType)
    {
        using SymmetricAlgorithm algorithm = CreateConfiguredAlgorithm(algorithmType, PaddingMode.ISO10126);
        var blockBytes = algorithm.BlockSize / 8;

        var cipherText = EncryptThroughCryptoStream(algorithm, System.Array.Empty<byte>());

        Assert.AreEqual(blockBytes, cipherText.Length,
            $"Empty plaintext under ISO10126 on {algorithmType.Name} should produce exactly one block of padded ciphertext.");

        var recovered = DecryptThroughCryptoStream(algorithm, cipherText);

        Assert.AreEqual(0, recovered.Length,
            $"Decrypting one ISO10126-padded block of empty plaintext on {algorithmType.Name} should recover an empty array.");
    }

    /// <summary>
    /// Verifies that a block-aligned plaintext round-trips through <see cref="CryptoStream" />
    /// under <see cref="PaddingMode.ISO10126" /> — ISO 10126 always appends a full block of
    /// padding (since the input is already aligned), and the decrypt pipeline strips it back off.
    /// </summary>
    /// <param name="algorithmType">The concrete <see cref="SymmetricAlgorithm" /> type under test.</param>
    [TestMethod]
    [DynamicData(nameof(SymmetricAlgorithmTestDataSource.SymmetricAlgorithmTestData), typeof(SymmetricAlgorithmTestDataSource), DynamicDataDisplayName = nameof(SymmetricAlgorithmTestDataSource.GetSymmetricAlgorithmDisplayName), DynamicDataDisplayNameDeclaringType = typeof(SymmetricAlgorithmTestDataSource))]
    public void CryptoStream_WhenBlockAlignedPlaintext_ShouldRoundTrip(System.Type algorithmType)
    {
        using SymmetricAlgorithm algorithm = CreateConfiguredAlgorithm(algorithmType, PaddingMode.ISO10126);
        var blockBytes = algorithm.BlockSize / 8;
        var plaintext = Enumerable.Range(0, blockBytes * 2).Select(i => (byte)(i + 1)).ToArray();

        var cipherText = EncryptThroughCryptoStream(algorithm, plaintext);

        Assert.AreEqual(plaintext.Length + blockBytes, cipherText.Length,
            $"Block-aligned plaintext under ISO10126 on {algorithmType.Name} should produce ciphertext one block longer than the plaintext.");

        var recovered = DecryptThroughCryptoStream(algorithm, cipherText);

        CollectionAssert.AreEqual(plaintext, recovered,
            $"Block-aligned plaintext under ISO10126 on {algorithmType.Name} did not round-trip through CryptoStream.");
    }

    /// <summary>
    /// Verifies that a sub-block plaintext round-trips through <see cref="CryptoStream" /> under
    /// <see cref="PaddingMode.ISO10126" /> — the trailing residual is padded out with random
    /// bytes plus a length byte, and the decrypt pipeline recovers the original prefix using
    /// the length-byte alone.
    /// </summary>
    /// <param name="algorithmType">The concrete <see cref="SymmetricAlgorithm" /> type under test.</param>
    [TestMethod]
    [DynamicData(nameof(SymmetricAlgorithmTestDataSource.SymmetricAlgorithmTestData), typeof(SymmetricAlgorithmTestDataSource), DynamicDataDisplayName = nameof(SymmetricAlgorithmTestDataSource.GetSymmetricAlgorithmDisplayName), DynamicDataDisplayNameDeclaringType = typeof(SymmetricAlgorithmTestDataSource))]
    public void CryptoStream_WhenResidualPlaintext_ShouldRoundTrip(System.Type algorithmType)
    {
        using SymmetricAlgorithm algorithm = CreateConfiguredAlgorithm(algorithmType, PaddingMode.ISO10126);
        var blockBytes = algorithm.BlockSize / 8;
        var plaintext = Enumerable.Range(0, blockBytes + 3).Select(i => (byte)(i + 1)).ToArray();

        var cipherText = EncryptThroughCryptoStream(algorithm, plaintext);

        Assert.AreEqual(0, cipherText.Length % blockBytes,
            $"ISO10126 ciphertext on {algorithmType.Name} must be block-aligned.");
        Assert.IsTrue(cipherText.Length > plaintext.Length,
            $"Residual plaintext under ISO10126 on {algorithmType.Name} should grow to the next block boundary.");

        var recovered = DecryptThroughCryptoStream(algorithm, cipherText);

        CollectionAssert.AreEqual(plaintext, recovered,
            $"Residual plaintext under ISO10126 on {algorithmType.Name} did not round-trip through CryptoStream.");
    }

    /// <summary>
    /// Verifies that a multi-write <see cref="CryptoStream" /> session — calling
    /// <see cref="Stream.Write(byte[], int, int)" /> several times with chunks of varying sizes
    /// that do not align with the cipher's block boundary — still produces ciphertext that
    /// decrypts to the concatenated plaintext under <see cref="PaddingMode.ISO10126" />.
    /// </summary>
    /// <param name="algorithmType">The concrete <see cref="SymmetricAlgorithm" /> type under test.</param>
    [TestMethod]
    [DynamicData(nameof(SymmetricAlgorithmTestDataSource.SymmetricAlgorithmTestData), typeof(SymmetricAlgorithmTestDataSource), DynamicDataDisplayName = nameof(SymmetricAlgorithmTestDataSource.GetSymmetricAlgorithmDisplayName), DynamicDataDisplayNameDeclaringType = typeof(SymmetricAlgorithmTestDataSource))]
    public void CryptoStream_WhenWritingInUnalignedChunks_ShouldRoundTrip(System.Type algorithmType)
    {
        using SymmetricAlgorithm algorithm = CreateConfiguredAlgorithm(algorithmType, PaddingMode.ISO10126);
        var plaintext = Enumerable.Range(0, 41).Select(i => (byte)(i * 7 + 11)).ToArray();
        var chunkSizes = new[] { 1, 3, 5, 7, 11, 13 };

        byte[] cipherText;
        using (var output = new MemoryStream())
        {
            using (ICryptoTransform encryptor = algorithm.CreateEncryptor())
            using (var crypto = new CryptoStream(output, encryptor, CryptoStreamMode.Write, leaveOpen: true))
            {
                var offset = 0;
                var chunkIndex = 0;
                while (offset < plaintext.Length)
                {
                    var len = System.Math.Min(chunkSizes[chunkIndex % chunkSizes.Length], plaintext.Length - offset);
                    crypto.Write(plaintext, offset, len);
                    offset += len;
                    chunkIndex++;
                }
            }

            cipherText = output.ToArray();
        }

        var recovered = DecryptThroughCryptoStream(algorithm, cipherText);

        CollectionAssert.AreEqual(plaintext, recovered,
            $"Chunked write under ISO10126 on {algorithmType.Name} did not round-trip through CryptoStream.");
    }
}
