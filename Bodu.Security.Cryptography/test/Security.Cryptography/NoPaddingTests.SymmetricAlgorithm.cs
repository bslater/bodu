// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NoPaddingTests.SymmetricAlgorithm.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// <see cref="PaddingMode.None" /> round-trip tests driven across every concrete
/// <see cref="SymmetricAlgorithm" /> declared in the library. <see cref="PaddingMode.None" />
/// is a pass-through scheme that adds nothing: empty input produces empty ciphertext,
/// already-aligned input round-trips at its original length, and unaligned input is rejected
/// at finalisation time. These tests pin those contracts across every cipher in the library.
/// </summary>
public sealed partial class NoPaddingTests
{
    /// <summary>
    /// Verifies that an empty plaintext under <see cref="PaddingMode.None" /> produces an
    /// empty ciphertext through a <see cref="CryptoStream" /> — no padding adds nothing, and
    /// zero bytes is trivially block-aligned.
    /// </summary>
    /// <param name="algorithmType">The concrete <see cref="SymmetricAlgorithm" /> type under test.</param>
    [TestMethod]
    [DynamicData(nameof(SymmetricAlgorithmTestDataSource.SymmetricAlgorithmTestData), typeof(SymmetricAlgorithmTestDataSource), DynamicDataDisplayName = nameof(SymmetricAlgorithmTestDataSource.GetSymmetricAlgorithmDisplayName), DynamicDataDisplayNameDeclaringType = typeof(SymmetricAlgorithmTestDataSource))]
    public void CryptoStream_WhenEmptyPlaintext_ShouldEmitEmptyCiphertext(System.Type algorithmType)
    {
        using SymmetricAlgorithm algorithm = CreateConfiguredAlgorithm(algorithmType, PaddingMode.None);

        byte[] cipherText = EncryptThroughCryptoStream(algorithm, []);

        Assert.AreEqual(0, cipherText.Length,
            $"Empty plaintext under None on {algorithmType.Name} should produce an empty ciphertext (no padding added).");

        byte[] recovered = DecryptThroughCryptoStream(algorithm, cipherText);

        Assert.AreEqual(0, recovered.Length,
            $"Decrypting empty ciphertext under None on {algorithmType.Name} should recover an empty array.");
    }

    /// <summary>
    /// Verifies that a block-aligned plaintext round-trips through <see cref="CryptoStream" />
    /// under <see cref="PaddingMode.None" /> — already-aligned input requires no padding, so the
    /// ciphertext keeps the original length and the recovered plaintext matches byte-for-byte.
    /// </summary>
    /// <param name="algorithmType">The concrete <see cref="SymmetricAlgorithm" /> type under test.</param>
    [TestMethod]
    [DynamicData(nameof(SymmetricAlgorithmTestDataSource.SymmetricAlgorithmTestData), typeof(SymmetricAlgorithmTestDataSource), DynamicDataDisplayName = nameof(SymmetricAlgorithmTestDataSource.GetSymmetricAlgorithmDisplayName), DynamicDataDisplayNameDeclaringType = typeof(SymmetricAlgorithmTestDataSource))]
    public void CryptoStream_WhenBlockAlignedPlaintext_ShouldRoundTrip(System.Type algorithmType)
    {
        using SymmetricAlgorithm algorithm = CreateConfiguredAlgorithm(algorithmType, PaddingMode.None);
        int blockBytes = algorithm.BlockSize / 8;
        byte[] plaintext = Enumerable.Range(0, blockBytes * 2).Select(i => (byte)(i + 1)).ToArray();

        byte[] cipherText = EncryptThroughCryptoStream(algorithm, plaintext);

        Assert.AreEqual(plaintext.Length, cipherText.Length,
            $"Block-aligned plaintext under None on {algorithmType.Name} should produce ciphertext of the same length.");

        byte[] recovered = DecryptThroughCryptoStream(algorithm, cipherText);

        CollectionAssert.AreEqual(plaintext, recovered,
            $"Block-aligned plaintext under None on {algorithmType.Name} did not round-trip through CryptoStream.");
    }
}
