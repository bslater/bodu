// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GcmSivModeTransformTests.Encrypt.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public sealed partial class GcmSivModeTransformTests
{
    /// <summary>
    /// The plaintext lengths the reference cross-checks use: short tails, one to three blocks, one either side of a
    /// four-block POLYVAL group and a 4 KiB run of counters, and several runs.
    /// </summary>
    private static readonly int[] s_referenceLengths = [0, 1, 15, 16, 17, 63, 64, 65, 4095, 4096, 4097, (3 * 4096) + 17, 20000];

    /// <summary>
    /// The associated-data lengths the reference cross-checks use: none, a partial block, one past a block, four
    /// blocks, and an unaligned run past four blocks.
    /// </summary>
    private static readonly int[] s_referenceAadLengths = [0, 1, 17, 64, 100];

    /// <summary>
    /// Verifies that the ciphertext and tag match a block-at-a-time reference built from RFC 8452's definitions - the
    /// platform's AES and POLYVAL computed directly in its own field - for 128- and 256-bit keys across plaintext
    /// lengths that fit in one run of counters, straddle one, and span several, with associated data of every
    /// alignment.
    /// </summary>
    [TestMethod]
    public void Encrypt_WhenMessageSpansSeveralRunsOfCounters_ShouldMatchBlockAtATimeReference()
    {
        foreach (int keyLength in new[] { 16, 32 })
        {
            byte[] key = ReferenceBytes(keyLength, keyLength);
            byte[] nonce = ReferenceBytes(12, keyLength + 1);

            foreach (int length in s_referenceLengths)
            {
                foreach (int aadLength in s_referenceAadLengths)
                {
                    byte[] plaintext = ReferenceBytes(length, length + 7);
                    byte[] aad = ReferenceBytes(aadLength, aadLength + 11);
                    byte[] expected = ReferenceSeal(key, nonce, aad, plaintext);

                    using var master = new AesBlockCipher(key);
                    using var transform = new GcmSivModeTransform(master, k => new AesBlockCipher(k), ReferenceIv(nonce));
                    transform.ProcessAssociatedData(aad);
                    byte[] actual = new byte[length + 16];
                    transform.Encrypt(plaintext, actual);

                    CollectionAssert.AreEqual(expected, actual, $"AES-{keyLength * 8}, {length} bytes, {aadLength} bytes of AAD");
                }
            }
        }
    }

    /// <summary>
    /// Seals a message one block at a time from RFC 8452's definitions: six or four single-block key-derivation calls,
    /// POLYVAL computed bit by bit in its own field, and a counter incremented block by block.
    /// </summary>
    /// <param name="key">The 16- or 32-byte key-generating key.</param>
    /// <param name="nonce">The 12-byte nonce.</param>
    /// <param name="aad">The associated data.</param>
    /// <param name="plaintext">The plaintext.</param>
    /// <returns>The ciphertext followed by the tag.</returns>
    private static byte[] ReferenceSeal(byte[] key, byte[] nonce, byte[] aad, byte[] plaintext)
    {
        using var master = Aes.Create();
        master.Key = key;

        // RFC 8452 Section 4: each key half is the first eight bytes of E(K, LE32(i) || nonce).
        byte[] authKey = new byte[16];
        byte[] encKey = new byte[key.Length];
        for (int i = 0; i < 2 + (key.Length / 8); i++)
        {
            byte[] input = new byte[16];
            BinaryPrimitives.WriteUInt32LittleEndian(input, (uint)i);
            nonce.CopyTo(input, 4);
            byte[] output = master.EncryptEcb(input, PaddingMode.None);
            if (i < 2)
                Array.Copy(output, 0, authKey, 8 * i, 8);
            else
                Array.Copy(output, 0, encKey, 8 * (i - 2), 8);
        }

        using var encryption = Aes.Create();
        encryption.Key = encKey;

        // Section 5: POLYVAL over the padded associated data, the padded plaintext, and the length block.
        UInt128 h = BinaryPrimitives.ReadUInt128LittleEndian(authKey);
        UInt128 s = 0;
        s = ReferencePolyvalUpdate(h, s, aad);
        s = ReferencePolyvalUpdate(h, s, plaintext);
        byte[] lengths = new byte[16];
        BinaryPrimitives.WriteUInt64LittleEndian(lengths, (ulong)aad.Length * 8);
        BinaryPrimitives.WriteUInt64LittleEndian(lengths.AsSpan(8), (ulong)plaintext.Length * 8);
        s = ReferencePolyvalUpdate(h, s, lengths);

        byte[] tagInput = new byte[16];
        BinaryPrimitives.WriteUInt128LittleEndian(tagInput, s);
        for (int i = 0; i < 12; i++)
            tagInput[i] ^= nonce[i];

        tagInput[15] &= 0x7F;
        byte[] tag = encryption.EncryptEcb(tagInput, PaddingMode.None);

        // The counter block is the tag with its top bit set; its first 32 bits count, little-endian, modulo 2^32.
        byte[] counter = (byte[])tag.Clone();
        counter[15] |= 0x80;
        byte[] result = new byte[plaintext.Length + 16];
        for (int offset = 0; offset < plaintext.Length; offset += 16)
        {
            byte[] keystream = encryption.EncryptEcb(counter, PaddingMode.None);
            for (int i = 0; i < Math.Min(16, plaintext.Length - offset); i++)
                result[offset + i] = (byte)(plaintext[offset + i] ^ keystream[i]);

            BinaryPrimitives.WriteUInt32LittleEndian(counter, unchecked(BinaryPrimitives.ReadUInt32LittleEndian(counter) + 1));
        }

        tag.CopyTo(result, plaintext.Length);
        return result;
    }

    /// <summary>
    /// Folds data into a POLYVAL state one zero-padded block at a time, using RFC 8452's definition directly:
    /// <c>S = dot(S ⊕ X, H)</c>, where <c>dot(A, B) = A · B · x⁻¹²⁸</c> in the field with polynomial
    /// <c>x¹²⁸ + x¹²⁷ + x¹²⁶ + x¹²¹ + 1</c>, bit <c>i</c> of the little-endian value being the coefficient of
    /// <c>xⁱ</c>.
    /// </summary>
    /// <param name="h">The POLYVAL key.</param>
    /// <param name="state">The starting state.</param>
    /// <param name="data">The data to fold in.</param>
    /// <returns>The resulting state.</returns>
    private static UInt128 ReferencePolyvalUpdate(UInt128 h, UInt128 state, byte[] data)
    {
        for (int offset = 0; offset < data.Length; offset += 16)
        {
            byte[] block = new byte[16];
            Array.Copy(data, offset, block, 0, Math.Min(16, data.Length - offset));
            UInt128 a = state ^ BinaryPrimitives.ReadUInt128LittleEndian(block);

            // Montgomery-style: add A for each set bit of H, then divide by x; after 128 steps the sum is A·H·x⁻¹²⁸.
            UInt128 product = 0;
            for (int bit = 0; bit < 128; bit++)
            {
                if (((h >> bit) & 1) != 0)
                    product ^= a;

                bool odd = (product & 1) != 0;
                product >>= 1;
                if (odd)
                    product ^= (UInt128)0xE1 << 120;
            }

            state = product;
        }

        return state;
    }

    /// <summary>
    /// Returns the 16-byte initialization vector whose first twelve bytes are <paramref name="nonce" />.
    /// </summary>
    /// <param name="nonce">The 12-byte nonce.</param>
    /// <returns>The initialization vector.</returns>
    private static byte[] ReferenceIv(byte[] nonce)
    {
        byte[] iv = new byte[16];
        nonce.CopyTo(iv, 0);
        return iv;
    }

    /// <summary>
    /// Returns a deterministic pseudo-random buffer for the reference cross-checks.
    /// </summary>
    /// <param name="length">The buffer's length.</param>
    /// <param name="seed">The generator's seed.</param>
    /// <returns>The buffer.</returns>
    private static byte[] ReferenceBytes(int length, int seed)
    {
        byte[] buffer = new byte[length];
        new Random(seed).NextBytes(buffer);
        return buffer;
    }
}
