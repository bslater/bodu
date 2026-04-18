// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OcbModeTransform.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    using System;
    using System.Security.Cryptography;

    /// <summary>
    /// Applies Offset CodeBook mode version 3 (OCB3) to an underlying <see cref="IBlockCipher" />,
    /// providing single-pass authenticated encryption with associated data per RFC 7253.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The nonce is derived from the first 12 bytes of the IV supplied to the constructor.
    /// The tag length is fixed at 16 bytes (128-bit tag, TAGLEN = 128).
    /// </para>
    /// <para>
    /// Offset initialisation uses the RFC 7253 K_top stretch:
    /// <code>
    ///   Nonce_word = 0x00000001 || N (for 12-byte N and TAGLEN=128 mod 128=0)
    ///   bottom     = Nonce_word[123..128] (last 6 bits)
    ///   K_top      = E(Nonce_word with last 6 bits zeroed)
    ///   Stretch    = K_top || (K_top[0..7] XOR K_top[8..15])
    ///   Offset_0   = Stretch[bottom..bottom+127]
    /// </code>
    /// </para>
    /// <para>
    /// The L array uses GF(2^128) doubling with polynomial x^128 + x^7 + x^2 + x + 1 (big-endian).
    /// </para>
    /// </remarks>
    public sealed class OcbModeTransform : IAeadBlockCipherModeTransform
    {
        private const int TagLengthBytes = 16;
        private const int NonceLengthBytes = 12;
        private const int MaxLValues = 32; // enough for 2^32 blocks

        private readonly IBlockCipher cipher;
        private readonly byte[] nonce;        // 12-byte nonce
        private readonly byte[] lStar;        // L_* = E(0^128)
        private readonly byte[] lDollar;      // L_$ = double(L_*)
        private readonly byte[][] lArray;     // L[0] = double(L_$), L[1] = double(L[0]), …
        private byte[]? aad;
        private bool aadProcessed;

        /// <summary>
        /// Initialises a new instance of the <see cref="OcbModeTransform" /> class.
        /// </summary>
        /// <param name="cipher">The block cipher (must have a 16-byte block size).</param>
        /// <param name="iv">
        /// The initialisation vector. The first 12 bytes are used as the OCB3 nonce.
        /// Must equal the cipher block size. A defensive copy is taken.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="cipher" /> or <paramref name="iv" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException"><paramref name="iv" /> length does not equal the cipher block size.</exception>
        public OcbModeTransform(IBlockCipher cipher, byte[] iv)
        {
            this.cipher = cipher ?? throw new ArgumentNullException(nameof(cipher));
            if (iv is null) throw new ArgumentNullException(nameof(iv));
            if (iv.Length != cipher.BlockSize)
                throw new ArgumentException(
                    $"IV length ({iv.Length}) must equal the cipher block size ({cipher.BlockSize}).", nameof(iv));

            this.nonce = new byte[NonceLengthBytes];
            iv.AsSpan(0, NonceLengthBytes).CopyTo(this.nonce);

            int blockSize = cipher.BlockSize;

            // L_* = E(0^128)
            this.lStar = new byte[blockSize];
            cipher.Encrypt(new byte[blockSize], this.lStar);

            // L_$ = double(L_*), L[0] = double(L_$), L[i] = double(L[i-1])
            this.lDollar = GfDouble(this.lStar);
            this.lArray = new byte[MaxLValues][];
            this.lArray[0] = GfDouble(this.lDollar);
            for (int i = 1; i < MaxLValues; i++)
                this.lArray[i] = GfDouble(this.lArray[i - 1]);
        }

        /// <inheritdoc />
        public int TagSize => TagLengthBytes;

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
                throw new ArgumentException($"Output must be at least {required} bytes.", nameof(output));
            EnsureAadProcessed();

            int blockSize = this.cipher.BlockSize;
            byte[] offset = ComputeInitialOffset();
            byte[] checksum = new byte[blockSize];
            byte[] block = new byte[blockSize];
            int m = (plaintext.Length + blockSize - 1) / blockSize;
            if (m == 0) m = 1;
            int j = 0;

            // All full blocks except the last.
            for (int blockIdx = 1; blockIdx <= m - 1; blockIdx++)
            {
                int src = (blockIdx - 1) * blockSize;
                Xor(offset, this.lArray[Ntz(blockIdx)], offset);
                plaintext.Slice(src, blockSize).CopyTo(block);
                Xor(block, offset, block);
                this.cipher.Encrypt(block, block);
                Xor(block, offset, block);
                block.CopyTo(output.Slice(src));
                Xor(checksum, plaintext.Slice(src, blockSize), checksum);
                j = blockIdx;
            }

            // Last block.
            int lastSrc = (m - 1) * blockSize;
            int lastLen = plaintext.Length - lastSrc;
            Xor(offset, this.lArray[Ntz(m)], offset);

            if (lastLen == blockSize)
            {
                // Complete last block.
                plaintext.Slice(lastSrc, blockSize).CopyTo(block);
                Xor(block, offset, block);
                this.cipher.Encrypt(block, block);
                Xor(block, offset, block);
                block.CopyTo(output.Slice(lastSrc));
                Xor(checksum, plaintext.Slice(lastSrc, blockSize), checksum);
            }
            else
            {
                // Partial last block: Pad = E(L_* XOR Offset_m).
                byte[] pad = new byte[blockSize];
                Xor(this.lStar, offset, pad);
                this.cipher.Encrypt(pad, pad);
                for (int i = 0; i < lastLen; i++)
                    output[lastSrc + i] = (byte)(plaintext[lastSrc + i] ^ pad[i]);
                // Checksum update: P_m || 1 || 0...0
                byte[] padBlock = new byte[blockSize];
                plaintext.Slice(lastSrc, lastLen).CopyTo(padBlock);
                padBlock[lastLen] = 0x80;
                Xor(checksum, padBlock, checksum);
            }

            // Tag = E(L_$ XOR Offset_m XOR Checksum) XOR HASH(A).
            byte[] tagInput = new byte[blockSize];
            Xor(this.lDollar, offset, tagInput);
            Xor(tagInput, checksum, tagInput);
            this.cipher.Encrypt(tagInput, tagInput);
            byte[] hashResult = ComputeHash(this.aad.AsSpan());
            Xor(tagInput, hashResult, tagInput);
            tagInput.AsSpan(0, TagSize).CopyTo(output.Slice(plaintext.Length));

            return required;
        }

        /// <inheritdoc />
        public int Decrypt(ReadOnlySpan<byte> ciphertextWithTag, Span<byte> output)
        {
            if (ciphertextWithTag.Length < TagSize)
                throw new ArgumentException($"Input must be at least {TagSize} bytes.", nameof(ciphertextWithTag));
            int plaintextLength = ciphertextWithTag.Length - TagSize;
            if (output.Length < plaintextLength)
                throw new ArgumentException($"Output must be at least {plaintextLength} bytes.", nameof(output));
            EnsureAadProcessed();

            ReadOnlySpan<byte> ciphertext = ciphertextWithTag.Slice(0, plaintextLength);
            ReadOnlySpan<byte> receivedTag = ciphertextWithTag.Slice(plaintextLength);

            int blockSize = this.cipher.BlockSize;
            byte[] offset = ComputeInitialOffset();
            byte[] checksum = new byte[blockSize];
            byte[] block = new byte[blockSize];
            int m = (plaintextLength + blockSize - 1) / blockSize;
            if (m == 0) m = 1;

            for (int blockIdx = 1; blockIdx <= m - 1; blockIdx++)
            {
                int src = (blockIdx - 1) * blockSize;
                Xor(offset, this.lArray[Ntz(blockIdx)], offset);
                ciphertext.Slice(src, blockSize).CopyTo(block);
                Xor(block, offset, block);
                this.cipher.Decrypt(block, block);
                Xor(block, offset, block);
                block.CopyTo(output.Slice(src));
                Xor(checksum, output.Slice(src, blockSize), checksum);
            }

            int lastSrc = (m - 1) * blockSize;
            int lastLen = plaintextLength - lastSrc;
            Xor(offset, this.lArray[Ntz(m)], offset);

            if (lastLen == blockSize)
            {
                ciphertext.Slice(lastSrc, blockSize).CopyTo(block);
                Xor(block, offset, block);
                this.cipher.Decrypt(block, block);
                Xor(block, offset, block);
                block.CopyTo(output.Slice(lastSrc));
                Xor(checksum, output.Slice(lastSrc, blockSize), checksum);
            }
            else
            {
                byte[] pad = new byte[blockSize];
                Xor(this.lStar, offset, pad);
                this.cipher.Encrypt(pad, pad);
                for (int i = 0; i < lastLen; i++)
                    output[lastSrc + i] = (byte)(ciphertext[lastSrc + i] ^ pad[i]);
                byte[] padBlock = new byte[blockSize];
                output.Slice(lastSrc, lastLen).CopyTo(padBlock);
                padBlock[lastLen] = 0x80;
                Xor(checksum, padBlock, checksum);
            }

            // Recompute tag and verify.
            byte[] tagInput = new byte[blockSize];
            Xor(this.lDollar, offset, tagInput);
            Xor(tagInput, checksum, tagInput);
            this.cipher.Encrypt(tagInput, tagInput);
            byte[] hashResult = ComputeHash(this.aad.AsSpan());
            Xor(tagInput, hashResult, tagInput);

            if (!CryptographicOperations.FixedTimeEquals(tagInput.AsSpan(0, TagSize), receivedTag))
            {
                CryptographicOperations.ZeroMemory(output.Slice(0, plaintextLength));
                throw new CryptographicException("OCB authentication tag verification failed.");
            }
            return plaintextLength;
        }

        // ── Private helpers ────────────────────────────────────────────────────────────────────────

        private void EnsureAadProcessed()
        {
            if (!this.aadProcessed) { this.aad = Array.Empty<byte>(); this.aadProcessed = true; }
        }

        /// <summary>
        /// Computes the initial offset Offset_0 using the RFC 7253 K_top stretch.
        /// Supports a 12-byte (96-bit) nonce with TAGLEN = 128.
        /// </summary>
        private byte[] ComputeInitialOffset()
        {
            int blockSize = this.cipher.BlockSize;

            // Build Nonce_word (128 bits) = 0x00 0x00 0x00 0x01 N[0..11]
            // (for TAGLEN mod 128 = 0 and a 96-bit nonce the first 32 bits are 0x00000001)
            byte[] nonceWord = new byte[blockSize];
            nonceWord[3] = 0x01;
            this.nonce.CopyTo(nonceWord, 4);

            // bottom = last 6 bits of nonceWord[15].
            int bottom = nonceWord[blockSize - 1] & 0x3F;

            // K_top_input: clear last 6 bits.
            byte[] ktopInput = (byte[])nonceWord.Clone();
            ktopInput[blockSize - 1] &= 0xC0;

            // K_top = E(K_top_input).
            byte[] ktop = new byte[blockSize];
            this.cipher.Encrypt(ktopInput, ktop);

            // Stretch = K_top || (K_top[0..7] XOR K_top[8..15])  (24 bytes).
            byte[] stretch = new byte[blockSize + blockSize / 2];
            ktop.CopyTo(stretch, 0);
            for (int i = 0; i < blockSize / 2; i++)
                stretch[blockSize + i] = (byte)(ktop[i] ^ ktop[blockSize / 2 + i]);

            // Offset_0 = 128-bit window of Stretch starting at bit `bottom`.
            byte[] offset = new byte[blockSize];
            int byteOffset = bottom / 8;
            int bitOffset = bottom % 8;
            if (bitOffset == 0)
            {
                stretch.AsSpan(byteOffset, blockSize).CopyTo(offset);
            }
            else
            {
                for (int i = 0; i < blockSize; i++)
                    offset[i] = (byte)((stretch[byteOffset + i] << bitOffset) |
                                       (stretch[byteOffset + i + 1] >> (8 - bitOffset)));
            }
            return offset;
        }

        /// <summary>
        /// Computes HASH(K, A) — the OCB3 authentication of associated data per RFC 7253 Section 4.
        /// </summary>
        private byte[] ComputeHash(ReadOnlySpan<byte> aad)
        {
            int blockSize = this.cipher.BlockSize;
            byte[] sum = new byte[blockSize];
            if (aad.Length == 0) return sum;

            byte[] offsetHash = new byte[blockSize];
            byte[] block = new byte[blockSize];
            int m = (aad.Length + blockSize - 1) / blockSize;

            for (int blockIdx = 1; blockIdx <= m; blockIdx++)
            {
                int src = (blockIdx - 1) * blockSize;
                int blockLen = Math.Min(blockSize, aad.Length - src);
                bool full = blockLen == blockSize;

                if (full)
                {
                    Xor(offsetHash, this.lArray[Ntz(blockIdx)], offsetHash);
                    aad.Slice(src, blockSize).CopyTo(block);
                    Xor(block, offsetHash, block);
                    this.cipher.Encrypt(block, block);
                    Xor(sum, block, sum);
                }
                else
                {
                    // Partial last block: pad with 0x80 || 0...0 then use L_*.
                    Xor(offsetHash, this.lStar, offsetHash);
                    byte[] padBlock = new byte[blockSize];
                    aad.Slice(src, blockLen).CopyTo(padBlock);
                    padBlock[blockLen] = 0x80;
                    Xor(padBlock, offsetHash, padBlock);
                    this.cipher.Encrypt(padBlock, padBlock);
                    Xor(sum, padBlock, sum);
                }
            }
            return sum;
        }

        /// <summary>
        /// Multiplies <paramref name="x" /> by α in GF(2^128) with big-endian bit order and polynomial
        /// x^128 + x^7 + x^2 + x + 1 (reduction via 0x87 in LSByte).
        /// </summary>
        private static byte[] GfDouble(byte[] x)
        {
            byte[] result = new byte[x.Length];
            bool msb = (x[0] & 0x80) != 0;
            for (int i = 0; i < x.Length - 1; i++)
                result[i] = (byte)((x[i] << 1) | (x[i + 1] >> 7));
            result[x.Length - 1] = (byte)(x[x.Length - 1] << 1);
            if (msb) result[x.Length - 1] ^= 0x87;
            return result;
        }

        private static void Xor(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b, Span<byte> result)
        {
            for (int i = 0; i < result.Length; i++) result[i] = (byte)(a[i] ^ b[i]);
        }

        /// <summary>Returns the number of trailing zero bits in <paramref name="n" /> (ntz function).</summary>
        private static int Ntz(int n)
        {
            if (n == 0) return 32;
            int count = 0;
            while ((n & 1) == 0) { n >>= 1; count++; }
            return count;
        }
    }
}
