// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TwofishTests.CreateEncryptor.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public sealed partial class TwofishTests
{
    /// <summary>
    /// Verifies that <see cref="Twofish.CreateEncryptor(byte[], byte[])" /> with an IV that is shorter than
    /// the block size reports the IV bit-length (not the key bit-length) in the exception message. Parameterised
    /// over all three supported key sizes to guard against confusion between key length and IV length in
    /// validation diagnostics. Twofish-specific because the message format is not part of the shared
    /// <see cref="SymmetricAlgorithmTests{TTest, TAlgorithm}" /> contract.
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
    /// over all three supported key sizes. Twofish-specific for the same reason as
    /// <see cref="CreateEncryptor_WhenIvIsTooShort_ShouldReportIvBitLength" />.
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
    /// configured key size. Twofish-specific because Twofish has three discrete legal key sizes (128 / 192 /
    /// 256 bits) — a one-byte adjustment from any legal size lands on an illegal size. Ciphers with a
    /// continuous legal-key-size range (e.g. Blowfish 32–448 bits) do not satisfy that property and are not
    /// covered by this test.
    /// </summary>
    [TestMethod]
    [DataRow(128)]
    [DataRow(192)]
    [DataRow(256)]
    public void CreateEncryptor_WhenKeyLengthMismatch_ShouldThrowExactly(int keySizeBits)
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
}
