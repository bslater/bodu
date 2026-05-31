// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricAlgorithmTests.RoundTrip.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class SymmetricAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that an encryptor obtained from a freshly configured <typeparamref name="TAlgorithm" />
    /// via <see cref="SymmetricAlgorithm.CreateEncryptor()" /> produces ciphertext that the paired
    /// decryptor from <see cref="SymmetricAlgorithm.CreateDecryptor()" /> recovers byte-for-byte.
    /// </summary>
    /// <remarks>
    /// Exercises the public facade end-to-end: property setters (<see cref="SymmetricAlgorithm.Mode" />,
    /// <see cref="SymmetricAlgorithm.Padding" />), key / IV generation, and the transform factories.
    /// Crypto-correctness against published vectors is validated at the block-cipher tier; this test
    /// pins the algorithm-tier plumbing.
    /// </remarks>
    [TestMethod]
    public void RoundTrip_WhenEncryptingThenDecryptingViaAlgorithm_ShouldRecoverPlaintext()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        SetEcbMode(algorithm);
        algorithm.Padding = PaddingMode.PKCS7;
        algorithm.GenerateKey();
        algorithm.GenerateIV();

        var plaintext = BuildIncrementingPlaintext(algorithm.BlockSize / 8 * 4);

        byte[] ciphertext;
        using (ICryptoTransform encryptor = algorithm.CreateEncryptor())
        {
            ciphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
        }

        byte[] recovered;
        using (ICryptoTransform decryptor = algorithm.CreateDecryptor())
        {
            recovered = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
        }

        CollectionAssert.AreEqual(plaintext, recovered);
    }

    /// <summary>
    /// Verifies that encrypting an empty plaintext through the algorithm's encryptor and then decrypting
    /// the resulting padding-only ciphertext through the paired decryptor recovers an empty plaintext.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenEncryptingEmptyInputViaAlgorithm_ShouldRecoverEmptyPlaintext()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        SetEcbMode(algorithm);
        algorithm.Padding = PaddingMode.PKCS7;
        algorithm.GenerateKey();
        algorithm.GenerateIV();

        byte[] ciphertext;
        using (ICryptoTransform encryptor = algorithm.CreateEncryptor())
        {
            ciphertext = encryptor.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        }

        byte[] recovered;
        using (ICryptoTransform decryptor = algorithm.CreateDecryptor())
        {
            recovered = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
        }

        Assert.AreEqual(0, recovered.Length);
    }

    /// <summary>
    /// Verifies that encrypting a partial final block — input length is one byte shy of
    /// <see cref="SymmetricAlgorithm.BlockSize" /> — through the algorithm's encryptor and then
    /// decrypting the padded ciphertext recovers the partial plaintext exactly.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenEncryptingPartialFinalBlockViaAlgorithm_ShouldRecoverPlaintext()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        SetEcbMode(algorithm);
        algorithm.Padding = PaddingMode.PKCS7;
        algorithm.GenerateKey();
        algorithm.GenerateIV();

        var blockBytes = algorithm.BlockSize / 8;
        if (blockBytes <= 1)
        {
            Assert.Inconclusive("The algorithm has a one-byte block size and cannot produce a shorter partial block.");
            return;
        }

        var plaintext = BuildIncrementingPlaintext(blockBytes - 1);

        byte[] ciphertext;
        using (ICryptoTransform encryptor = algorithm.CreateEncryptor())
        {
            ciphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
        }

        byte[] recovered;
        using (ICryptoTransform decryptor = algorithm.CreateDecryptor())
        {
            recovered = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
        }

        CollectionAssert.AreEqual(plaintext, recovered);
    }

    private static byte[] BuildIncrementingPlaintext(int length)
    {
        var buffer = new byte[length];
        for (var i = 0; i < length; i++)
            buffer[i] = (byte)i;
        return buffer;
    }
}
