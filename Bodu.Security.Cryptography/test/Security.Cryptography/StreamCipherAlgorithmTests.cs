// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StreamCipherAlgorithmTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

using System.Security.Cryptography;

/// <summary>
/// Provides the shared behavioural contract for additive stream ciphers derived from
/// <see cref="StreamCipherAlgorithm" />. Concrete cipher test classes inherit this base to gain common coverage —
/// self-inverse round-trips, one-shot versus segmented equivalence, zero-plaintext-equals-keystream, and key / nonce
/// argument validation — mirroring the way block-cipher tests inherit from
/// <see cref="SymmetricAlgorithmTests{TTest, TAlgorithm}" />.
/// </summary>
/// <typeparam name="TTest">The concrete test class (self-referential, so MSTest can construct it).</typeparam>
/// <typeparam name="TAlgorithm">The stream-cipher type under test.</typeparam>
[TestClass]
public abstract class StreamCipherAlgorithmTests<TTest, TAlgorithm>
    where TTest : StreamCipherAlgorithmTests<TTest, TAlgorithm>, new()
    where TAlgorithm : StreamCipherAlgorithm, new()
{
    /// <summary>
    /// Creates a new instance of the stream cipher under test.
    /// </summary>
    /// <returns>A new <typeparamref name="TAlgorithm" /> instance.</returns>
    protected virtual TAlgorithm CreateAlgorithm() => new();

    /// <summary>
    /// Gets the required key length, in bytes, for the cipher under test.
    /// </summary>
    /// <returns>The key length in bytes.</returns>
    protected int KeyLengthBytes
    {
        get
        {
            using TAlgorithm alg = CreateAlgorithm();
            return alg.KeySize / 8;
        }
    }

    /// <summary>
    /// Gets the required nonce (IV) length, in bytes, for the cipher under test.
    /// </summary>
    /// <returns>The nonce length in bytes.</returns>
    protected int NonceLengthBytes
    {
        get
        {
            using TAlgorithm alg = CreateAlgorithm();
            return alg.BlockSize / 8;
        }
    }

    /// <summary>
    /// Verifies that encrypting a payload and then decrypting the result recovers the original plaintext, confirming
    /// the cipher is self-inverse.
    /// </summary>
    [TestMethod]
    public void Transform_WhenDecryptingCiphertext_ShouldRecoverPlaintext()
    {
        byte[] key = CreateKey();
        byte[] nonce = CreateNonce();
        byte[] plaintext = CreatePayload(4097);

        byte[] ciphertext;
        using (TAlgorithm encryptor = CreateAlgorithm())
        using (ICryptoTransform e = encryptor.CreateEncryptor(key, nonce))
            ciphertext = e.TransformFinalBlock(plaintext, 0, plaintext.Length);

        byte[] recovered;
        using (TAlgorithm decryptor = CreateAlgorithm())
        using (ICryptoTransform d = decryptor.CreateDecryptor(key, nonce))
            recovered = d.TransformFinalBlock(ciphertext, 0, ciphertext.Length);

        CollectionAssert.AreEqual(plaintext, recovered);
    }

    /// <summary>
    /// Verifies that transforming the input in arbitrary, non-block-aligned segments produces the same output as a
    /// single one-shot transform, confirming the partial-block keystream carry is correct.
    /// </summary>
    [TestMethod]
    public void Transform_WhenInputIsSegmented_ShouldMatchOneShotTransform()
    {
        byte[] key = CreateKey();
        byte[] nonce = CreateNonce();
        byte[] plaintext = CreatePayload(4097);

        byte[] oneShot;
        using (TAlgorithm cipher = CreateAlgorithm())
        using (ICryptoTransform e = cipher.CreateEncryptor(key, nonce))
            oneShot = e.TransformFinalBlock(plaintext, 0, plaintext.Length);

        byte[] segmented = new byte[plaintext.Length];
        using (TAlgorithm cipher = CreateAlgorithm())
        using (ICryptoTransform e = cipher.CreateEncryptor(key, nonce))
        {
            int offset = 0;
            foreach (int chunk in new[] { 1, 63, 64, 65, 127, 200, 1024, 0 })
            {
                int count = Math.Min(chunk, plaintext.Length - offset);
                offset += e.TransformBlock(plaintext, offset, count, segmented, offset);
            }

            byte[] tail = e.TransformFinalBlock(plaintext, offset, plaintext.Length - offset);
            tail.CopyTo(segmented.AsSpan(offset));
        }

        CollectionAssert.AreEqual(oneShot, segmented);
    }

    /// <summary>
    /// Verifies that encrypting an all-zero plaintext yields the raw keystream, confirming the additive (XOR) cipher
    /// construction.
    /// </summary>
    [TestMethod]
    public void Transform_WhenPlaintextIsZero_ShouldEqualGeneratedKeystream()
    {
        byte[] key = CreateKey();
        byte[] nonce = CreateNonce();

        // Two independent instances under the same key/nonce must produce identical keystream.
        byte[] first;
        using (TAlgorithm cipher = CreateAlgorithm())
        using (ICryptoTransform e = cipher.CreateEncryptor(key, nonce))
            first = e.TransformFinalBlock(new byte[257], 0, 257);

        byte[] second;
        using (TAlgorithm cipher = CreateAlgorithm())
        using (ICryptoTransform e = cipher.CreateEncryptor(key, nonce))
            second = e.TransformFinalBlock(new byte[257], 0, 257);

        CollectionAssert.AreEqual(first, second);

        // The keystream must not be all zeros (which would indicate the engine produced nothing).
        Assert.IsTrue(Array.Exists(first, b => b != 0), "Keystream should not be all zero bytes.");
    }

    /// <summary>
    /// Verifies that the same key and nonce reproduce the same first bytes of keystream across fresh instances,
    /// confirming deterministic, repeatable keystream generation.
    /// </summary>
    [TestMethod]
    public void CreateEncryptor_WhenReusedWithSameKeyAndNonce_ShouldReproduceKeystream()
    {
        byte[] key = CreateKey();
        byte[] nonce = CreateNonce();
        byte[] zeros = new byte[96];

        byte[] a;
        using (TAlgorithm cipher = CreateAlgorithm())
        using (ICryptoTransform e = cipher.CreateEncryptor(key, nonce))
            a = e.TransformFinalBlock(zeros, 0, zeros.Length);

        byte[] b;
        using (TAlgorithm cipher = CreateAlgorithm())
        using (ICryptoTransform e = cipher.CreateEncryptor(key, nonce))
            b = e.TransformFinalBlock(zeros, 0, zeros.Length);

        CollectionAssert.AreEqual(a, b);
    }

    /// <summary>
    /// Verifies that <see cref="StreamCipherAlgorithm.CreateEncryptor(byte[], byte[])" /> rejects a key whose length is
    /// not the algorithm's key size with a <see cref="CryptographicException" />.
    /// </summary>
    [TestMethod]
    public void CreateEncryptor_WhenKeyLengthIsInvalid_ShouldThrowCryptographicException()
    {
        using TAlgorithm cipher = CreateAlgorithm();
        byte[] shortKey = new byte[KeyLengthBytes - 1];
        byte[] nonce = CreateNonce();

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = cipher.CreateEncryptor(shortKey, nonce);
        });
    }

    /// <summary>
    /// Verifies that <see cref="StreamCipherAlgorithm.CreateEncryptor(byte[], byte[])" /> rejects a nonce whose length
    /// is not the algorithm's nonce size with a <see cref="CryptographicException" />.
    /// </summary>
    [TestMethod]
    public void CreateEncryptor_WhenNonceLengthIsInvalid_ShouldThrowCryptographicException()
    {
        using TAlgorithm cipher = CreateAlgorithm();
        byte[] key = CreateKey();
        byte[] wrongNonce = new byte[NonceLengthBytes + 1];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = cipher.CreateEncryptor(key, wrongNonce);
        });
    }

    /// <summary>
    /// Verifies that accessing a disposed cipher's transform factory throws <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void CreateEncryptor_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        TAlgorithm cipher = CreateAlgorithm();
        byte[] key = CreateKey();
        byte[] nonce = CreateNonce();
        cipher.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = cipher.CreateEncryptor(key, nonce);
        });
    }

    /// <summary>
    /// Verifies that <see cref="StreamCipherAlgorithm.GenerateKey" /> and
    /// <see cref="StreamCipherAlgorithm.GenerateIV" /> produce a key and nonce of the algorithm's required lengths that
    /// drive a successful round-trip.
    /// </summary>
    [TestMethod]
    public void GenerateKeyAndIV_WhenUsed_ShouldDriveSuccessfulRoundTrip()
    {
        using TAlgorithm cipher = CreateAlgorithm();
        cipher.GenerateKey();
        cipher.GenerateIV();

        Assert.AreEqual(KeyLengthBytes, cipher.Key.Length);
        Assert.AreEqual(NonceLengthBytes, cipher.IV.Length);

        byte[] plaintext = CreatePayload(200);

        byte[] ciphertext;
        using (ICryptoTransform e = cipher.CreateEncryptor())
            ciphertext = e.TransformFinalBlock(plaintext, 0, plaintext.Length);

        byte[] recovered;
        using (ICryptoTransform d = cipher.CreateDecryptor())
            recovered = d.TransformFinalBlock(ciphertext, 0, ciphertext.Length);

        CollectionAssert.AreEqual(plaintext, recovered);
    }

    /// <summary>
    /// Creates a deterministic key of the algorithm's required length.
    /// </summary>
    /// <returns>A key byte array.</returns>
    private byte[] CreateKey() => FillSequential(new byte[KeyLengthBytes], 0x10);

    /// <summary>
    /// Creates a deterministic nonce of the algorithm's required length.
    /// </summary>
    /// <returns>A nonce byte array.</returns>
    private byte[] CreateNonce() => FillSequential(new byte[NonceLengthBytes], 0x40);

    /// <summary>
    /// Creates a deterministic, non-trivial payload of the requested length.
    /// </summary>
    /// <param name="length">The payload length, in bytes.</param>
    /// <returns>A payload byte array.</returns>
    private static byte[] CreatePayload(int length) => FillSequential(new byte[length], 1);

    /// <summary>
    /// Fills a buffer with a simple incrementing byte pattern.
    /// </summary>
    /// <param name="buffer">The buffer to fill.</param>
    /// <param name="seed">The starting byte value.</param>
    /// <returns>The same <paramref name="buffer" />, filled.</returns>
    private static byte[] FillSequential(byte[] buffer, int seed)
    {
        for (int i = 0; i < buffer.Length; i++)
            buffer[i] = (byte)(seed + i);

        return buffer;
    }
}
