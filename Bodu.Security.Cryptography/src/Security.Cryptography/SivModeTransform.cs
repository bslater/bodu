// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SivModeTransform.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    using System;

    /// <summary>
    /// Applies the SIV (Synthetic IV / RFC 5297) encryption/decryption transformation to an underlying
    /// <see cref="IBlockCipher" />, using CTR mode seeded from a pre-computed synthetic IV.
    /// </summary>
    /// <remarks>
    /// <para>
    /// SIV is a misuse-resistant authenticated encryption mode. Its encryption component is CTR mode where
    /// the initial counter is derived from the SIV (the output of S2V over the plaintext and associated data):
    /// <list type="bullet">
    /// <item><description>Keystream_i = E(counter_i), counter incremented (big-endian) each block.</description></item>
    /// <item><description>Both encrypt and decrypt: output_i = input_i ⊕ Keystream_i.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Bits 31 and 63 of the SIV (numbered from the right in big-endian order) are cleared before use as
    /// the initial counter. This prevents the counter from wrapping within the 2^31 or 2^63 block boundaries
    /// that would cause keystream reuse, as required by RFC 5297 §2.6.
    /// </para>
    /// <para>
    /// This implementation accepts the pre-computed SIV directly as the IV parameter, corresponding to the CTR
    /// encryption component with the S2V derivation step elided. The authentication component (S2V computation
    /// from the plaintext and associated data) requires the <c>IAeadBlockCipherModeTransform</c> interface
    /// extension.
    /// </para>
    /// </remarks>
    public sealed class SivModeTransform : IBlockCipherModeTransform
    {
        private readonly IBlockCipher cipher;
        private readonly byte[] counter; // SIV with bits 31 and 63 cleared, incremented big-endian each block

        /// <summary>
        /// Initialises a new instance of the <see cref="SivModeTransform" /> class.
        /// </summary>
        /// <param name="cipher">The block cipher used to generate keystream blocks.</param>
        /// <param name="iv">
        /// The pre-computed Synthetic IV (S2V output). Must equal the cipher block size. A defensive copy is
        /// taken; bits 31 and 63 are cleared on the copy before use as the initial counter. The caller's array
        /// is not modified.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="cipher" /> or <paramref name="iv" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="iv" /> length does not equal the cipher block size.
        /// </exception>
        public SivModeTransform(IBlockCipher cipher, byte[] iv)
        {
            this.cipher = cipher ?? throw new ArgumentNullException(nameof(cipher));
            if (iv is null) throw new ArgumentNullException(nameof(iv));
            if (iv.Length != cipher.BlockSize)
                throw new ArgumentException(
                    $"IV length ({iv.Length}) must equal the cipher block size ({cipher.BlockSize}).",
                    nameof(iv));

            this.counter = (byte[])iv.Clone();

            // Clear bits 31 and 63 (numbered from the right in a big-endian 128-bit value) to prevent
            // counter wrap within the message-length limits of 2^31 and 2^63 blocks (RFC 5297 §2.6).
            // Bit n from the right is in byte[length - 1 - n/8] at bit position n%8.
            this.counter[this.counter.Length - 4] &= 0x7F; // clear bit 31
            this.counter[this.counter.Length - 8] &= 0x7F; // clear bit 63
        }

        /// <inheritdoc />
        public int Transform(ReadOnlySpan<byte> input, Span<byte> output, bool encrypt)
        {
            int blockSize = this.cipher.BlockSize;
            ThrowHelper.ThrowIfSpanLengthNotPositiveMultipleOf(input, blockSize);
            ThrowHelper.ThrowIfSpanLengthIsInsufficient(output, 0, input.Length);

            Span<byte> keystream = stackalloc byte[blockSize];

            for (int offset = 0; offset < input.Length; offset += blockSize)
            {
                // Generate keystream block from the current counter (always uses encrypt primitive).
                this.cipher.Encrypt(this.counter, keystream);

                // XOR with input — identical for both encrypt and decrypt (CTR property).
                ReadOnlySpan<byte> inBlock = input.Slice(offset, blockSize);
                Span<byte> outBlock = output.Slice(offset, blockSize);
                for (int i = 0; i < blockSize; i++)
                    outBlock[i] = (byte)(inBlock[i] ^ keystream[i]);

                // Advance counter (big-endian increment, rightmost byte first).
                IncrementBigEndian(this.counter);
            }

            return input.Length;
        }

        /// <summary>
        /// Increments <paramref name="counter" /> as an unsigned big-endian integer, wrapping on overflow.
        /// </summary>
        private static void IncrementBigEndian(byte[] counter)
        {
            for (int i = counter.Length - 1; i >= 0; i--)
                if (++counter[i] != 0) break;
        }
    }
}
