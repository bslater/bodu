// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ZeroPaddingTests.SymmetricAlgorithm.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;
using System.Security.Cryptography;
using Bodu.Security.Cryptography.Extensions;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Zero-padding round-trip tests driven across every concrete <see cref="SymmetricAlgorithm" />
/// declared in the library. Zero padding is non-self-describing — when the trailing plaintext
/// bytes happen to be zero, <see cref="ZeroPadding.Unpad" /> cannot tell padding from data — so
/// these tests focus on the contract for already-aligned inputs (no padding added, pure
/// round-trip) and the documented partial-block behaviour where the prefix is preserved but
/// trailing zero bytes are appended to make the input block-aligned.
/// </summary>
public sealed partial class ZeroPaddingTests
{
    /// <summary>
    /// Verifies that an empty plaintext under <see cref="PaddingMode.Zeros" /> produces an
    /// empty ciphertext through a <see cref="CryptoStream" /> — zero padding adds nothing
    /// when the input is already at a block boundary, and zero bytes is trivially aligned.
    /// </summary>
    /// <param name="algorithmType">The concrete <see cref="SymmetricAlgorithm" /> type under test.</param>
    [TestMethod]
    [DynamicData(nameof(SymmetricAlgorithmTestDataSource.SymmetricAlgorithmTestData), typeof(SymmetricAlgorithmTestDataSource), DynamicDataDisplayName = nameof(SymmetricAlgorithmTestDataSource.GetSymmetricAlgorithmDisplayName), DynamicDataDisplayNameDeclaringType = typeof(SymmetricAlgorithmTestDataSource))]
    public void CryptoStream_WhenEmptyPlaintext_ShouldEmitEmptyCiphertext(System.Type algorithmType)
    {
        using SymmetricAlgorithm algorithm = CreateConfiguredAlgorithm(algorithmType, PaddingMode.Zeros);

        byte[] cipherText = EncryptThroughCryptoStream(algorithm, System.Array.Empty<byte>());

        Assert.AreEqual(0, cipherText.Length,
            $"Empty plaintext under Zeros on {algorithmType.Name} should produce an empty ciphertext (no padding added).");

        byte[] recovered = DecryptThroughCryptoStream(algorithm, cipherText);

        Assert.AreEqual(0, recovered.Length,
            $"Decrypting empty ciphertext under Zeros on {algorithmType.Name} should recover an empty array.");
    }

    /// <summary>
    /// Verifies that a block-aligned plaintext round-trips through <see cref="CryptoStream" />
    /// under <see cref="PaddingMode.Zeros" /> — already-aligned data needs no padding, so the
    /// ciphertext keeps the original length and the recovered plaintext matches byte-for-byte.
    /// </summary>
    /// <param name="algorithmType">The concrete <see cref="SymmetricAlgorithm" /> type under test.</param>
    [TestMethod]
    [DynamicData(nameof(SymmetricAlgorithmTestDataSource.SymmetricAlgorithmTestData), typeof(SymmetricAlgorithmTestDataSource), DynamicDataDisplayName = nameof(SymmetricAlgorithmTestDataSource.GetSymmetricAlgorithmDisplayName), DynamicDataDisplayNameDeclaringType = typeof(SymmetricAlgorithmTestDataSource))]
    public void CryptoStream_WhenBlockAlignedPlaintext_ShouldRoundTrip(System.Type algorithmType)
    {
        using SymmetricAlgorithm algorithm = CreateConfiguredAlgorithm(algorithmType, PaddingMode.Zeros);
        int blockBytes = algorithm.BlockSize / 8;
        byte[] plaintext = Enumerable.Range(0, blockBytes * 2).Select(i => (byte)(i + 1)).ToArray();

        byte[] cipherText = EncryptThroughCryptoStream(algorithm, plaintext);

        Assert.AreEqual(plaintext.Length, cipherText.Length,
            $"Block-aligned plaintext under Zeros on {algorithmType.Name} should produce ciphertext of the same length.");

        byte[] recovered = DecryptThroughCryptoStream(algorithm, cipherText);

        CollectionAssert.AreEqual(plaintext, recovered,
            $"Block-aligned plaintext under Zeros on {algorithmType.Name} did not round-trip through CryptoStream.");
    }

    /// <summary>
    /// Verifies that a sub-block plaintext under <see cref="PaddingMode.Zeros" /> preserves
    /// the original prefix on decrypt, even though <see cref="ZeroPadding.Unpad" /> cannot
    /// strip the trailing zero bytes — they are indistinguishable from legitimate plaintext
    /// zeros, so the recovered output is the original prefix plus padding zeros up to the
    /// next block boundary.
    /// </summary>
    /// <param name="algorithmType">The concrete <see cref="SymmetricAlgorithm" /> type under test.</param>
    [TestMethod]
    [DynamicData(nameof(SymmetricAlgorithmTestDataSource.SymmetricAlgorithmTestData), typeof(SymmetricAlgorithmTestDataSource), DynamicDataDisplayName = nameof(SymmetricAlgorithmTestDataSource.GetSymmetricAlgorithmDisplayName), DynamicDataDisplayNameDeclaringType = typeof(SymmetricAlgorithmTestDataSource))]
    public void CryptoStream_WhenResidualPlaintext_ShouldRecoverPrefixWithTrailingZeros(System.Type algorithmType)
    {
        using SymmetricAlgorithm algorithm = CreateConfiguredAlgorithm(algorithmType, PaddingMode.Zeros);
        int blockBytes = algorithm.BlockSize / 8;
        byte[] plaintext = Enumerable.Range(0, blockBytes + 3).Select(i => (byte)(i + 1)).ToArray();

        byte[] cipherText = EncryptThroughCryptoStream(algorithm, plaintext);
        byte[] recovered = DecryptThroughCryptoStream(algorithm, cipherText);

        Assert.AreEqual(0, cipherText.Length % blockBytes,
            $"Zeros-padded ciphertext on {algorithmType.Name} must be block-aligned.");
        Assert.IsTrue(recovered.Length >= plaintext.Length,
            $"Zeros-padded recovered output on {algorithmType.Name} must be at least as long as the original plaintext.");
        CollectionAssert.AreEqual(
            plaintext,
            recovered.Take(plaintext.Length).ToArray(),
            $"Zeros-padded plaintext prefix on {algorithmType.Name} must survive round-trip through CryptoStream.");
        Assert.IsTrue(
            recovered.Skip(plaintext.Length).All(b => b == 0),
            $"Trailing bytes in Zeros-padded recovered output on {algorithmType.Name} must all be zero.");
    }
}
