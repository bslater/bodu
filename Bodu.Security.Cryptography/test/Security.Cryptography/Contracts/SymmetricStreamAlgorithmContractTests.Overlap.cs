// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricStreamAlgorithmContractTests.Overlap.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Contracts;

public abstract partial class SymmetricStreamAlgorithmContractTests<TCipher>
{
    /// <summary>
    /// Verifies that an exact in-place <see cref="ICryptoTransform.TransformBlock" /> (identical input and output
    /// offsets in the same array) produces the same ciphertext as a one-shot transform.
    /// </summary>
    [TestMethod]
    public void TransformBlock_WhenInPlaceSameOffset_ShouldSucceed()
    {
        byte[] key = CreateKey();
        byte[] nonce = CreateNonce();
        byte[] plaintext = CreatePayload(128);

        byte[] expected;
        using (TCipher cipher = CreateAlgorithm())
        using (ICryptoTransform e = cipher.CreateEncryptor(key, nonce))
            expected = e.TransformFinalBlock(plaintext, 0, plaintext.Length);

        byte[] buffer = (byte[])plaintext.Clone();
        using (TCipher cipher = CreateAlgorithm())
        using (ICryptoTransform e = cipher.CreateEncryptor(key, nonce))
            _ = e.TransformBlock(buffer, 0, buffer.Length, buffer, 0);

        CollectionAssert.AreEqual(expected, buffer);
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransform.TransformBlock" /> over fully disjoint ranges of the same array
    /// succeeds and produces the expected ciphertext.
    /// </summary>
    [TestMethod]
    public void TransformBlock_WhenDisjointRangesInSameArray_ShouldSucceed()
    {
        byte[] key = CreateKey();
        byte[] nonce = CreateNonce();
        const int count = 32;
        byte[] plaintext = CreatePayload(count);

        byte[] expected;
        using (TCipher cipher = CreateAlgorithm())
        using (ICryptoTransform e = cipher.CreateEncryptor(key, nonce))
            expected = e.TransformFinalBlock(plaintext, 0, count);

        // Input occupies [0, count); output is written to the disjoint range [count, 2*count).
        byte[] buffer = new byte[count * 2];
        plaintext.CopyTo(buffer.AsSpan(0));
        using (TCipher cipher = CreateAlgorithm())
        using (ICryptoTransform e = cipher.CreateEncryptor(key, nonce))
            _ = e.TransformBlock(buffer, 0, count, buffer, count);

        CollectionAssert.AreEqual(expected, buffer[count..]);
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransform.TransformBlock" /> rejects a same-array call whose output range
    /// partially overlaps the unread input with a <see cref="CryptographicException" />.
    /// </summary>
    [TestMethod]
    public void TransformBlock_WhenPartiallyOverlappingInSameArray_ShouldThrowCryptographicException()
    {
        byte[] key = CreateKey();
        byte[] nonce = CreateNonce();

        using TCipher cipher = CreateAlgorithm();
        using ICryptoTransform e = cipher.CreateEncryptor(key, nonce);

        // Input [0, 32) and output [16, 48) share the bytes [16, 32).
        byte[] buffer = CreatePayload(64);

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = e.TransformBlock(buffer, 0, 32, buffer, 16);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransform.TransformBlock" /> with separate input and output arrays succeeds and
    /// produces the expected ciphertext.
    /// </summary>
    [TestMethod]
    public void TransformBlock_WhenInputAndOutputAreDifferentArrays_ShouldSucceed()
    {
        byte[] key = CreateKey();
        byte[] nonce = CreateNonce();
        byte[] plaintext = CreatePayload(128);

        byte[] expected;
        using (TCipher cipher = CreateAlgorithm())
        using (ICryptoTransform e = cipher.CreateEncryptor(key, nonce))
            expected = e.TransformFinalBlock(plaintext, 0, plaintext.Length);

        byte[] output = new byte[plaintext.Length];
        using (TCipher cipher = CreateAlgorithm())
        using (ICryptoTransform e = cipher.CreateEncryptor(key, nonce))
            _ = e.TransformBlock(plaintext, 0, plaintext.Length, output, 0);

        CollectionAssert.AreEqual(expected, output);
    }
}
