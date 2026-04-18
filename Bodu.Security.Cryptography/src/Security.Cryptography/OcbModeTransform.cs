// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OcbModeTransform.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Applies the Offset Codebook (OCB3) encryption/decryption transformation to an underlying
    /// <see cref="IBlockCipher" />, using ntz-derived offsets for per-block tweaking.
    /// </summary>
    /// <remarks>
    /// <para>
    /// OCB derives a sequence of offsets from a nonce and pre-computed multiples of E(0...0):
    /// <list type="bullet">
    /// <item><description>Encrypt: C_i = E(P_i ⊕ Δ_i) ⊕ Δ_i</description></item>
    /// <item><description>Decrypt: P_i = D(C_i ⊕ Δ_i) ⊕ Δ_i</description></item>
    /// </list>
    /// where Δ_i = Δ_{i−1} ⊕ L[ntz(i)], ntz(i) is the number of trailing zeros of i, and L[j] is
    /// the j-th double of E(0...0) in GF(2^n).
    /// </para>
    /// <para>
    /// This implementation provides the cipher-text transformation component only. Full OCB3 includes
    /// generation and verification of an integrity tag over the ciphertext and optional associated data,
    /// which requires the <c>IAeadBlockCipherModeTransform</c> interface extension.
    /// </para>
    /// <para>
    /// GF(2^128) doubling uses big-endian byte ordering with reduction polynomial
    /// x^128 + x^7 + x^2 + x + 1 (constant 0x87) per RFC 7253.
    /// </para>
    /// </remarks>
    public sealed class OcbModeTransform : IBlockCipherModeTransform
    {
        private readonly IBlockCipher cipher;
        private readonly List<byte[]> L;      // lazily-extended: L[j] = 2^(j+1) * E(0...0)
        private readonly byte[] offset;       // running offset Δ, updated each block
        private int blockIndex;               // 1-based index of the next block to process

        /// <summary>
        /// Initialises a new instance of the <see cref="OcbModeTransform" /> class.
        /// </summary>
        /// <param name="cipher">The block cipher providing the encryption and decryption primitives.</param>
        /// <param name="iv">
        /// The nonce used to derive the initial offset. Must equal the cipher block size. A defensive copy
        /// is taken; the caller's array is not modified.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="cipher" /> or <paramref name="iv" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="iv" /> length does not equal the cipher block size.
        /// </exception>
        public OcbModeTransform(IBlockCipher cipher, byte[] iv)
        {
            this.cipher = cipher ?? throw new ArgumentNullException(nameof(cipher));
            if (iv is null) throw new ArgumentNullException(nameof(iv));
            if (iv.Length != cipher.BlockSize)
                throw new ArgumentException(
                    $"IV length ({iv.Length}) must equal the cipher block size ({cipher.BlockSize}).",
                    nameof(iv));

            int blockSize = cipher.BlockSize;

            // L_star = E(0...0); L[0] = 2 * L_star (first pre-computed multiple).
            byte[] zeros = new byte[blockSize];
            byte[] lStar = new byte[blockSize];
            cipher.Encrypt(zeros, lStar);
            this.L = new List<byte[]> { GfDouble(lStar, blockSize) };

            // Initial offset = E(nonce).
            this.offset = new byte[blockSize];
            cipher.Encrypt(iv, this.offset);

            this.blockIndex = 1;
        }

        /// <inheritdoc />
        public int Transform(ReadOnlySpan<byte> input, Span<byte> output, bool encrypt)
        {
            int blockSize = this.cipher.BlockSize;
            ThrowHelper.ThrowIfSpanLengthNotPositiveMultipleOf(input, blockSize);
            ThrowHelper.ThrowIfSpanLengthIsInsufficient(output, 0, input.Length);

            Span<byte> tweaked = stackalloc byte[blockSize];

            for (int off = 0; off < input.Length; off += blockSize)
            {
                // Advance offset: Δ_i = Δ_{i−1} ⊕ L[ntz(i)].
                int ntz = NumberOfTrailingZeros(this.blockIndex);
                EnsureLComputed(ntz, blockSize);
                byte[] lNtz = this.L[ntz];
                for (int i = 0; i < blockSize; i++)
                    this.offset[i] ^= lNtz[i];
                this.blockIndex++;

                ReadOnlySpan<byte> inBlock = input.Slice(off, blockSize);
                Span<byte> outBlock = output.Slice(off, blockSize);

                // Step 1: XOR input with offset.
                for (int i = 0; i < blockSize; i++)
                    tweaked[i] = (byte)(inBlock[i] ^ this.offset[i]);

                // Step 2: Apply cipher primitive.
                if (encrypt)
                    this.cipher.Encrypt(tweaked, outBlock);
                else
                    this.cipher.Decrypt(tweaked, outBlock);

                // Step 3: XOR result with offset.
                for (int i = 0; i < blockSize; i++)
                    outBlock[i] ^= this.offset[i];
            }

            return input.Length;
        }

        /// <summary>
        /// Returns the number of trailing zero bits in the binary representation of <paramref name="n" />.
        /// For n = 0, returns 32.
        /// </summary>
        private static int NumberOfTrailingZeros(int n)
        {
            if (n == 0) return 32;
            int count = 0;
            while ((n & 1) == 0) { count++; n >>= 1; }
            return count;
        }

        /// <summary>
        /// Ensures that <see cref="L" /> contains at least <paramref name="index" /> + 1 elements,
        /// computing additional multiples via <see cref="GfDouble" /> as needed.
        /// </summary>
        private void EnsureLComputed(int index, int blockSize)
        {
            while (this.L.Count <= index)
                this.L.Add(GfDouble(this.L[this.L.Count - 1], blockSize));
        }

        /// <summary>
        /// Returns a new byte array equal to 2 * <paramref name="t" /> in GF(2^128) using big-endian
        /// byte ordering and reduction polynomial x^128 + x^7 + x^2 + x + 1 (constant 0x87).
        /// </summary>
        private static byte[] GfDouble(byte[] t, int blockSize)
        {
            byte[] result = new byte[blockSize];
            bool carry = (t[0] & 0x80) != 0; // MSB of most-significant byte (big-endian)

            for (int i = 0; i < blockSize - 1; i++)
                result[i] = (byte)((t[i] << 1) | (t[i + 1] >> 7));
            result[blockSize - 1] = (byte)(t[blockSize - 1] << 1);

            // If carry, reduce by XOR-ing the low byte with the field constant.
            if (carry)
                result[blockSize - 1] ^= 0x87;

            return result;
        }
    }
}
