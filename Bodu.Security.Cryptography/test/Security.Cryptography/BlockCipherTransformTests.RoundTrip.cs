// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockCipherTransformTests.RoundTrip.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class BlockCipherTransformTests<TTest, TCryptoTransform>
{
    /// <summary>
    /// Verifies that encrypting a single-block plaintext via
    /// <see cref="ICryptoTransform.TransformFinalBlock(byte[], int, int)" /> and then decrypting the
    /// resulting ciphertext through the paired decryptor recovers the original plaintext byte-for-byte.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenEncryptingSingleBlock_ShouldRecoverPlaintext()
    {
        byte[] plaintext;
        byte[] ciphertext;

        using (TCryptoTransform encryptor = CreateEncryptor())
        {
            plaintext = BuildIncrementingPlaintext(encryptor.InputBlockSize);
            ciphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
        }

        byte[] recovered;
        using (TCryptoTransform decryptor = CreateDecryptor())
        {
            recovered = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
        }

        CollectionAssert.AreEqual(plaintext, recovered);
    }

    /// <summary>
    /// Verifies that encrypting a multi-block plaintext and then decrypting the resulting ciphertext
    /// through the paired decryptor recovers the original plaintext byte-for-byte.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenEncryptingMultipleBlocks_ShouldRecoverPlaintext()
    {
        byte[] plaintext;
        byte[] ciphertext;

        using (TCryptoTransform encryptor = CreateEncryptor())
        {
            plaintext = BuildIncrementingPlaintext(encryptor.InputBlockSize * 4);
            ciphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
        }

        byte[] recovered;
        using (TCryptoTransform decryptor = CreateDecryptor())
        {
            recovered = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
        }

        CollectionAssert.AreEqual(plaintext, recovered);
    }

    /// <summary>
    /// Verifies that encrypting an empty plaintext via
    /// <see cref="ICryptoTransform.TransformFinalBlock(byte[], int, int)" /> and then decrypting the
    /// padding-only ciphertext through the paired decryptor recovers an empty plaintext.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenEncryptingEmptyInput_ShouldRecoverEmptyPlaintext()
    {
        byte[] ciphertext;
        using (TCryptoTransform encryptor = CreateEncryptor())
        {
            ciphertext = encryptor.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        }

        byte[] recovered;
        using (TCryptoTransform decryptor = CreateDecryptor())
        {
            recovered = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
        }

        Assert.AreEqual(0, recovered.Length);
    }

    /// <summary>
    /// Verifies that encrypting a partial final block — input length is one byte shy of
    /// <see cref="ICryptoTransform.InputBlockSize" /> — and decrypting the padded ciphertext recovers
    /// the partial plaintext exactly.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenEncryptingPartialFinalBlock_ShouldRecoverPlaintext()
    {
        byte[] plaintext;
        byte[] ciphertext;

        using (TCryptoTransform encryptor = CreateEncryptor())
        {
            if (encryptor.InputBlockSize <= 1)
            {
                Assert.Inconclusive("The transform has a one-byte input block size and cannot produce a shorter partial block.");
                return;
            }

            plaintext = BuildIncrementingPlaintext(encryptor.InputBlockSize - 1);
            ciphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
        }

        byte[] recovered;
        using (TCryptoTransform decryptor = CreateDecryptor())
        {
            recovered = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
        }

        CollectionAssert.AreEqual(plaintext, recovered);
    }

    /// <summary>
    /// Verifies that a streaming encrypt — <see cref="ICryptoTransform.TransformBlock(byte[], int, int, byte[], int)" />
    /// for the leading aligned chunk followed by <see cref="ICryptoTransform.TransformFinalBlock(byte[], int, int)" />
    /// for the residual final block — round-trips through the paired decryptor.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenStreamingEncryptThenDecrypt_ShouldRecoverPlaintext()
    {
        byte[] plaintext;
        byte[] ciphertext;

        using (TCryptoTransform encryptor = CreateEncryptor())
        {
            int blockSize = encryptor.InputBlockSize;
            plaintext = BuildIncrementingPlaintext(blockSize * 3);

            var streamed = new byte[plaintext.Length + blockSize];
            int written = encryptor.TransformBlock(plaintext, 0, blockSize * 2, streamed, 0);
            byte[] tail = encryptor.TransformFinalBlock(plaintext, blockSize * 2, blockSize);
            Buffer.BlockCopy(tail, 0, streamed, written, tail.Length);

            ciphertext = new byte[written + tail.Length];
            Buffer.BlockCopy(streamed, 0, ciphertext, 0, ciphertext.Length);
        }

        byte[] recovered;
        using (TCryptoTransform decryptor = CreateDecryptor())
        {
            recovered = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
        }

        CollectionAssert.AreEqual(plaintext, recovered);
    }

    private static byte[] BuildIncrementingPlaintext(int length)
    {
        var buffer = new byte[length];
        for (int i = 0; i < length; i++)
            buffer[i] = (byte)i;
        return buffer;
    }
}
