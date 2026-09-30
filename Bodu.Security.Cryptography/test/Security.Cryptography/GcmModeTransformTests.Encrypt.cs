// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GcmModeTransformTests.Encrypt.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public sealed partial class GcmModeTransformTests
{
    /// <summary>
    /// The plaintext lengths the BCL cross-checks use: short tails, one to three blocks, one either side of a 4 KiB run
    /// of counters, and several runs.
    /// </summary>
    private static readonly int[] s_crossCheckLengths = [0, 1, 15, 16, 17, 63, 64, 65, 4095, 4096, 4097, (3 * 4096) + 17, 20000];

    /// <summary>
    /// The associated-data lengths the BCL cross-checks use: none, a partial block, one past a block, four blocks, and
    /// an unaligned run past four blocks.
    /// </summary>
    private static readonly int[] s_crossCheckAadLengths = [0, 1, 17, 64, 100];

    /// <summary>
    /// Verifies that the ciphertext and tag match the platform's <see cref="AesGcm" /> - an independent implementation
    /// - for AES-128 and AES-256 across plaintext lengths that fit in one run of counters, straddle one, and span
    /// several, with associated data of every alignment.
    /// </summary>
    [TestMethod]
    public void Encrypt_WhenMessageSpansSeveralRunsOfCounters_ShouldMatchPlatformAesGcm()
    {
        if (!AesGcm.IsSupported)
            Assert.Inconclusive("AesGcm is not supported on this platform.");

        foreach (int keyLength in new[] { 16, 32 })
        {
            byte[] key = CrossCheckBytes(keyLength, keyLength);
            byte[] nonce = CrossCheckBytes(NonceSizeBytes, 3);
            using var platform = new AesGcm(key, 16);
            using var cipher = new AesBlockCipher(key);

            foreach (int length in s_crossCheckLengths)
            {
                foreach (int aadLength in s_crossCheckAadLengths)
                {
                    byte[] plaintext = CrossCheckBytes(length, length + 7);
                    byte[] aad = CrossCheckBytes(aadLength, aadLength + 11);
                    byte[] expected = new byte[length + 16];
                    platform.Encrypt(nonce, plaintext, expected.AsSpan(0, length), expected.AsSpan(length), aad);

                    using var transform = new GcmModeTransform(cipher, nonce);
                    transform.ProcessAssociatedData(aad);
                    byte[] actual = new byte[length + 16];
                    transform.Encrypt(plaintext, actual);

                    CollectionAssert.AreEqual(expected, actual, $"AES-{keyLength * 8}, {length} bytes, {aadLength} bytes of AAD");
                }
            }
        }
    }

    /// <summary>
    /// Returns a deterministic pseudo-random buffer for the platform cross-checks.
    /// </summary>
    /// <param name="length">The buffer's length.</param>
    /// <param name="seed">The generator's seed.</param>
    /// <returns>The buffer.</returns>
    private static byte[] CrossCheckBytes(int length, int seed)
    {
        byte[] buffer = new byte[length];
        new Random(seed).NextBytes(buffer);
        return buffer;
    }
}
