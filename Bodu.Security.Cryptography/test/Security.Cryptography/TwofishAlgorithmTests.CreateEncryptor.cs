// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TwofishAlgorithmTests.CreateEncryptor.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public sealed partial class TwofishAlgorithmTests
{
    /// <summary>
    /// Verifies that <see cref="Twofish.CreateEncryptor(byte[], byte[])" /> with an IV that is shorter than
    /// the block size reports the IV bit-length (not the key bit-length) in the exception message. Parameterised
    /// over all three supported key sizes to guard against confusion between key length and IV length in
    /// validation diagnostics.
    /// </summary>
    [TestMethod]
    [DataRow(128)]
    [DataRow(192)]
    [DataRow(256)]
    public void CreateEncryptor_WhenIvIsTooShort_ShouldReportIvBitLength(int keySizeBits)
    {
        using Twofish algorithm = CreateAlgorithm();
        algorithm.KeySize = keySizeBits;
        algorithm.GenerateKey();

        var blockSizeBytes = algorithm.BlockSize / 8;
        var badIv = new byte[blockSizeBytes - 1];
        var expectedBitLength = badIv.Length * 8;

        CryptographicException ex = Assert.ThrowsExactly<CryptographicException>(() =>
        {
            using ICryptoTransform _ = algorithm.CreateEncryptor(algorithm.Key, badIv);
        });

        Assert.IsTrue(
            ex.Message.Contains(expectedBitLength.ToString()),
            $"Expected IV bit-length {expectedBitLength} in message but got: {ex.Message}");
    }

    /// <summary>
    /// Verifies that <see cref="Twofish.CreateDecryptor(byte[], byte[])" /> with an IV that is shorter than
    /// the block size reports the IV bit-length (not the key bit-length) in the exception message. Parameterised
    /// over all three supported key sizes to guard against confusion between key length and IV length in
    /// validation diagnostics.
    /// </summary>
    [TestMethod]
    [DataRow(128)]
    [DataRow(192)]
    [DataRow(256)]
    public void CreateDecryptor_WhenIvIsTooShort_ShouldReportIvBitLength(int keySizeBits)
    {
        using Twofish algorithm = CreateAlgorithm();
        algorithm.KeySize = keySizeBits;
        algorithm.GenerateKey();

        var blockSizeBytes = algorithm.BlockSize / 8;
        var badIv = new byte[blockSizeBytes - 1];
        var expectedBitLength = badIv.Length * 8;

        CryptographicException ex = Assert.ThrowsExactly<CryptographicException>(() =>
        {
            using ICryptoTransform _ = algorithm.CreateDecryptor(algorithm.Key, badIv);
        });

        Assert.IsTrue(
            ex.Message.Contains(expectedBitLength.ToString()),
            $"Expected IV bit-length {expectedBitLength} in message but got: {ex.Message}");
    }

    /// <summary>
    /// Verifies that <see cref="Twofish.CreateEncryptor(byte[], byte[])" /> throws
    /// <see cref="CryptographicException" /> when the supplied key length does not match the algorithm's
    /// configured key size. Parameterised over all three supported key sizes.
    /// </summary>
    [TestMethod]
    [DataRow(128)]
    [DataRow(192)]
    [DataRow(256)]
    public void CreateEncryptor_WhenKeyLengthMismatch_ShouldThrowCryptographicException(int keySizeBits)
    {
        using Twofish algorithm = CreateAlgorithm();
        algorithm.KeySize = keySizeBits;

        var wrongKey = new byte[keySizeBits / 8 + 1];
        var validIv = new byte[algorithm.BlockSize / 8];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            using ICryptoTransform _ = algorithm.CreateEncryptor(wrongKey, validIv);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Twofish.CreateEncryptor(byte[], byte[])" /> succeeds and returns a non-null
    /// transform for each of the three supported key sizes (128, 192, and 256 bits).
    /// </summary>
    [TestMethod]
    [DataRow(128)]
    [DataRow(192)]
    [DataRow(256)]
    public void CreateEncryptor_WithEachValidKeySize_ShouldSucceed(int keySizeBits)
    {
        using Twofish algorithm = CreateAlgorithm();
        algorithm.KeySize = keySizeBits;
        algorithm.GenerateKey();
        algorithm.GenerateIV();

        using ICryptoTransform transform = algorithm.CreateEncryptor(algorithm.Key, algorithm.IV);
        Assert.IsNotNull(transform);
    }

    /// <summary>
    /// Verifies that <see cref="Twofish.CreateDecryptor(byte[], byte[])" /> succeeds and returns a non-null
    /// transform for each of the three supported key sizes (128, 192, and 256 bits).
    /// </summary>
    [TestMethod]
    [DataRow(128)]
    [DataRow(192)]
    [DataRow(256)]
    public void CreateDecryptor_WithEachValidKeySize_ShouldSucceed(int keySizeBits)
    {
        using Twofish algorithm = CreateAlgorithm();
        algorithm.KeySize = keySizeBits;
        algorithm.GenerateKey();
        algorithm.GenerateIV();

        using ICryptoTransform transform = algorithm.CreateDecryptor(algorithm.Key, algorithm.IV);
        Assert.IsNotNull(transform);
    }
}
