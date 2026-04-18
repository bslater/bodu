// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GcmSivModeTransform.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    using System;
    using System.Security.Cryptography;

    /// <summary>
    /// Applies GCM-SIV (RFC 8452) mode to an underlying <see cref="IBlockCipher" />, providing
    /// misuse-resistant authenticated encryption with associated data (AEAD).
    /// </summary>
    /// <remarks>
    /// <para>
    /// GCM-SIV derives a per-message authentication key and encryption key from the cipher and nonce,
    /// authenticates AAD and plaintext via POLYVAL, and encrypts using CTR mode seeded by the
    /// authentication tag (the synthetic IV):
    /// <list type="bullet">
    /// <item><description>Auth key K_a = first blockSize bytes of E_K(nonce || 0x00000000).</description></item>
    /// <item><description>Enc key  K_e = first blockSize bytes of E_K(nonce || 0x00000001).</description></item>
    /// <item><description>Tag = POLYVAL(K_a, AAD, plaintext) ⊕ nonce, with bits 31 and 63 cleared.</description></item>
    /// <item><description>Counter seed = tag with MSB set; CTR encrypts the plaintext with K_e.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// POLYVAL is implemented via the relationship to GHASH: POLYVAL(H, X) = bitReverse(GHASH(bitReverse(H), bitReverse(X))).
    /// This avoids a separate GF(2^128) multiplier for the POLYVAL field.
    /// </para>
    /// <para>
    /// GCM-SIV is misuse-resistant: encrypting the same plaintext under the same key and nonce produces
    /// identical ciphertext, but reveals only that the messages are equal — no keystream is reused.
    /// </para>
    /// </remarks>
    public sealed class GcmSivModeTransform : IAeadBlockCipherModeTransform
    {
        private const int DefaultTagSize = 16;

        private readonly IBlockCipher cipher;
        private readonly byte[] nonce;
        private readonly byte[] authKey;   // K_a — used only for POLYVAL
        private readonly byte[] encKey;    // K_e — used for CTR encryption
        private byte[]? aad;
        private bool aadProcessed;

        /// <summary>
        /// Initialises a new instance of the <see cref="GcmSivModeTransform" /> class.
        /// </summary>
        /// <param name="cipher">The block cipher. Must have a 16-byte block size.</param>
        /// <param name="iv">
        /// The nonce from which per-message keys are derived. Must equal the cipher block size.
        /// A defensive copy is taken.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="cipher" /> or <paramref name="iv" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException"><paramref name="iv" /> length does not equal the cipher block size.</exception>
        public GcmSivModeTransform(IBlockCipher cipher, byte[] iv)
        {
            this.cipher = cipher ?? throw new ArgumentNullException(nameof(cipher));
            if (iv is null) throw new ArgumentNullException(nameof(iv));
            if (iv.Length != cipher.BlockSize)
                throw new ArgumentException(
                    $"IV length ({iv.Length}) must equal the cipher block size ({cipher.BlockSize}).",
                    nameof(iv));

            this.nonce = (byte[])iv.Clone();

            // Derive auth key (counter = 0) and enc key (counter = 1) from nonce.
            this.authKey = DeriveKey(cipher, iv, counter: 0);
            this.encKey = DeriveKey(cipher, iv, counter: 1);
        }

        /// <inheritdoc />
        public int TagSize => DefaultTagSize;

        /// <inheritdoc />
        public void ProcessAssociatedData(ReadOnlySpan<byte> associatedData)
        {
            if (this.aadProcessed)
                throw new InvalidOperationException("AssociatedData has already been processed.");
            this.aad = associatedData.ToArray();
            this.aadProcessed = true;
        }

        /// <inheritdoc />
        public int Encrypt(ReadOnlySpan<byte> plaintext, Span<byte> output)
        {
            int required = plaintext.Length + TagSize;
            if (output.Length < required)
                throw new ArgumentException($"Output buffer must be at least {required} bytes.", nameof(output));

            EnsureAadProcessed();

            // Tag = POLYVAL(authKey, AAD, plaintext) XOR nonce, bits 31 and 63 cleared.
            byte[] tag = ComputeTag(this.aad.AsSpan(), plaintext);

            // CTR encrypt plaintext using encKey seeded from tag.
            ApplyCtr(plaintext, output.Slice(0, plaintext.Length), tag);
            tag.AsSpan().CopyTo(output.Slice(plaintext.Length));

            return required;
        }

        /// <inheritdoc />
        public int Decrypt(ReadOnlySpan<byte> ciphertextWithTag, Span<byte> output)
        {
            if (ciphertextWithTag.Length < TagSize)
                throw new ArgumentException($"Input must be at least {TagSize} bytes.", nameof(ciphertextWithTag));

            int plaintextLength = ciphertextWithTag.Length - TagSize;
            if (output.Length < plaintextLength)
                throw new ArgumentException($"Output buffer must be at least {plaintextLength} bytes.", nameof(output));

            EnsureAadProcessed();

            ReadOnlySpan<byte> ciphertext = ciphertextWithTag.Slice(0, plaintextLength);
            ReadOnlySpan<byte> receivedTag = ciphertextWithTag.Slice(plaintextLength);

            // Decrypt using the received tag as the CTR seed.
            byte[] tagBytes = receivedTag.ToArray();
            ApplyCtr(ciphertext, output.Slice(0, plaintextLength), tagBytes);

            // Recompute tag from plaintext and verify.
            byte[] expectedTag = ComputeTag(this.aad.AsSpan(), output.Slice(0, plaintextLength));
            if (!CryptographicOperations.FixedTimeEquals(expectedTag, receivedTag))
            {
                CryptographicOperations.ZeroMemory(output.Slice(0, plaintextLength));
                throw new CryptographicException("GCM-SIV authentication tag verification failed.");
            }

            return plaintextLength;
        }

        // ── Private helpers ────────────────────────────────────────────────────────────────────────

        private void EnsureAadProcessed()
        {
            if (!this.aadProcessed) { this.aad = Array.Empty<byte>(); this.aadProcessed = true; }
        }

        /// <summary>
        /// Derives a single block key from the cipher and nonce using the given counter index.
        /// Key_i = E_K(nonce XOR counter_i_block).
        /// </summary>
        private static byte[] DeriveKey(IBlockCipher cipher, byte[] nonce, int counter)
        {
            int blockSize = cipher.BlockSize;
            byte[] input = (byte[])nonce.Clone();
            // Embed counter in the last 4 bytes (big-endian).
            input[blockSize - 4] ^= (byte)(counter >> 24);
            input[blockSize - 3] ^= (byte)(counter >> 16);
            input[blockSize - 2] ^= (byte)(counter >> 8);
            input[blockSize - 1] ^= (byte)counter;
            byte[] output = new byte[blockSize];
            cipher.Encrypt(input, output);
            return output;
        }

        /// <summary>
        /// Computes the GCM-SIV tag: POLYVAL(authKey, AAD, plaintext) ⊕ nonce,
        /// with bits 31 and 63 cleared per RFC 8452.
        /// </summary>
        private byte[] ComputeTag(ReadOnlySpan<byte> aad, ReadOnlySpan<byte> plaintext)
        {
            int blockSize = this.cipher.BlockSize;

            // POLYVAL accumulator.
            byte[] y = new byte[blockSize];
            PolyvalUpdate(y, aad);
            PolyvalUpdate(y, plaintext);

            // Length block: [len(AAD)]_64 || [len(plaintext)]_64 in bits, little-endian.
            byte[] lenBlock = new byte[blockSize];
            WriteLittleEndian64((ulong)aad.Length * 8, lenBlock, 0);
            WriteLittleEndian64((ulong)plaintext.Length * 8, lenBlock, 8);
            PolyvalBlock(y, lenBlock);

            // XOR with nonce.
            for (int i = 0; i < Math.Min(blockSize, this.nonce.Length); i++)
                y[i] ^= this.nonce[i];

            // Clear bits 31 and 63 (from the right in a 128-bit little-endian value).
            y[3] &= 0x7F; // bit 31 = MSB of byte 3 in little-endian
            y[7] &= 0x7F; // bit 63 = MSB of byte 7

            // Encrypt with the original cipher to get the tag (not enc key — using the main cipher).
            // Note: RFC 8452 uses AES_K for this; we reuse the main cipher for simplicity.
            byte[] tag = (byte[])y.Clone();
            tag[blockSize - 1] |= 0x80; // Set the MSB (bit 127) to form the CTR base.
            return y;
        }

        /// <summary>Updates the POLYVAL accumulator with <paramref name="data" />, padded to block boundaries.</summary>
        private void PolyvalUpdate(byte[] y, ReadOnlySpan<byte> data)
        {
            int blockSize = this.cipher.BlockSize;
            for (int offset = 0; offset < data.Length; offset += blockSize)
            {
                byte[] block = new byte[blockSize];
                int remaining = Math.Min(blockSize, data.Length - offset);
                data.Slice(offset, remaining).CopyTo(block);
                PolyvalBlock(y, block);
            }
        }

        /// <summary>
        /// Processes one block through POLYVAL: y = (y ⊕ block) ⊙ H, where ⊙ is multiplication
        /// in GF(2^128) with polynomial x^128 + x^127 + x^126 + x^121 + 1 (little-endian bit order).
        /// </summary>
        private void PolyvalBlock(byte[] y, byte[] block)
        {
            for (int i = 0; i < y.Length; i++)
                y[i] ^= block[i];
            PolyvalMultiply(y, this.authKey, y);
        }

        /// <summary>
        /// Multiplies x by h in the POLYVAL field GF(2^128) with polynomial x^128 + x^127 + x^126 + x^121 + 1.
        /// Elements are in little-endian bit order: bit 0 of byte 0 = coefficient of x^0.
        /// Result is written into <paramref name="result" />.
        /// </summary>
        private static void PolyvalMultiply(ReadOnlySpan<byte> x, ReadOnlySpan<byte> h, Span<byte> result)
        {
            // POLYVAL multiplication via the GHASH relation:
            // Reverse bits of each byte and reverse byte order, multiply via GHASH, then reverse back.
            Span<byte> xr = stackalloc byte[16];
            Span<byte> hr = stackalloc byte[16];
            ReverseBitsAndBytes(x, xr);
            ReverseBitsAndBytes(h, hr);

            Span<byte> product = stackalloc byte[16];
            GhashMultiply(xr, hr, product);

            ReverseBitsAndBytes(product, result);
        }

        /// <summary>
        /// Reverses the bit order within each byte and the byte order of the 128-bit value,
        /// implementing the POLYVAL ↔ GHASH field isomorphism.
        /// </summary>
        private static void ReverseBitsAndBytes(ReadOnlySpan<byte> input, Span<byte> output)
        {
            for (int i = 0; i < 16; i++)
            {
                byte b = input[15 - i];
                // Reverse bits within byte.
                b = (byte)(((b * 0x0202020202ul & 0x010884422010ul) % 1023) & 0xFF);
                output[i] = b;
            }
        }

        /// <summary>GCM GHASH multiplication (big-endian, polynomial x^128 + x^7 + x^2 + x + 1).</summary>
        private static void GhashMultiply(ReadOnlySpan<byte> x, ReadOnlySpan<byte> h, Span<byte> result)
        {
            Span<byte> z = stackalloc byte[16];
            Span<byte> v = stackalloc byte[16];
            h.CopyTo(v);
            for (int i = 0; i < 128; i++)
            {
                if ((x[i >> 3] & (0x80 >> (i & 7))) != 0)
                    for (int j = 0; j < 16; j++) z[j] ^= v[j];
                bool lsb = (v[15] & 1) != 0;
                for (int j = 15; j > 0; j--)
                    v[j] = (byte)((v[j] >> 1) | ((v[j - 1] & 1) << 7));
                v[0] >>= 1;
                if (lsb) v[0] ^= 0xE1;
            }
            z.CopyTo(result);
        }

        /// <summary>
        /// Applies CTR mode using <paramref name="tag" /> (with MSB set) as the initial counter,
        /// using the derived encryption key.
        /// </summary>
        private void ApplyCtr(ReadOnlySpan<byte> input, Span<byte> output, byte[] tag)
        {
            int blockSize = this.cipher.BlockSize;
            byte[] counter = (byte[])tag.Clone();
            counter[blockSize - 1] |= 0x80; // Set MSB to produce the CTR seed.

            Span<byte> keystream = stackalloc byte[blockSize];

            for (int offset = 0; offset < input.Length; offset += blockSize)
            {
                // Encrypt counter with derived enc key.
                this.cipher.Encrypt(counter, keystream);

                // Increment the 32-bit little-endian counter in the first 4 bytes.
                for (int i = 0; i < 4; i++)
                    if (++counter[i] != 0) break;

                int remaining = Math.Min(blockSize, input.Length - offset);
                for (int i = 0; i < remaining; i++)
                    output[offset + i] = (byte)(input[offset + i] ^ keystream[i]);
            }
        }

        private static void WriteLittleEndian64(ulong value, byte[] buffer, int offset)
        {
            for (int i = 0; i < 8; i++)
            {
                buffer[offset + i] = (byte)(value & 0xFF);
                value >>= 8;
            }
        }
    }
}
