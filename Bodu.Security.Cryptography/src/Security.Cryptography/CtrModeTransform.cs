// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CtrModeTransform.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    using System;
    using System.Security.Cryptography;

    /// <summary>
    /// Applies Counter (CTR) mode to an underlying <see cref="IBlockCipher" />, turning it into a
    /// synchronous stream cipher. The counter is incremented in big-endian order (rightmost byte first),
    /// matching NIST SP 800-38A Section 6.5.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <img src="../images/diagrams/classic-modes.svg" alt="CTR panel — independent counter blocks are encrypted to form a keystream, then XORed with plaintext." />
    /// </para>
    /// <para>
    /// CTR is self-inverse: the same <see cref="Transform" /> operation is applied for both
    /// encryption and decryption. The cipher's <em>encrypt</em> primitive is always used; the
    /// decrypt primitive is never called.
    /// See <b>panel 5</b> of the diagram above: each cell has its own counter block <c>CTRᵢ</c> and
    /// no arrows connect one cell to the next — meaning the keystream is trivially parallelisable and
    /// supports random-access seeking into the middle of a message.
    /// </para>
    /// <para>
    /// That independence is also where the sharpest pitfall lives. To protect against keystream reuse,
    /// the transform tracks counter wrap-around: if the counter increments back to its initial value,
    /// the next call to <see cref="Transform" /> throws <see cref="CryptographicException" />. Reusing
    /// a <c>(key, nonce)</c> pair across messages is catastrophic — the XOR of two ciphertexts
    /// recovers the XOR of the two plaintexts — so callers must ensure each counter value is used at
    /// most once per key.
    /// </para>
    /// </remarks>
    public sealed class CtrModeTransform : IBlockCipherModeTransform
    {
        private readonly IBlockCipher cipher;
        private readonly byte[] initialCounter;
        private readonly byte[] counter;
        private bool counterWrapped;

        /// <summary>
        /// Initialises a new instance of the <see cref="CtrModeTransform" /> class.
        /// </summary>
        /// <param name="cipher">The block cipher whose encrypt primitive generates the keystream.</param>
        /// <param name="initialCounter">
        /// The starting counter block. Must equal the cipher block size. A defensive copy is taken.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="cipher" /> or <paramref name="initialCounter" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="initialCounter" /> length does not equal the cipher block size.
        /// </exception>
        public CtrModeTransform(IBlockCipher cipher, byte[] initialCounter)
        {
            this.cipher = cipher ?? throw new ArgumentNullException(nameof(cipher));
            if (initialCounter is null) throw new ArgumentNullException(nameof(initialCounter));
            if (initialCounter.Length != cipher.BlockSize)
                throw new ArgumentException(
                    $"initialCounter length ({initialCounter.Length}) must equal the cipher block size ({cipher.BlockSize}).",
                    nameof(initialCounter));

            this.initialCounter = (byte[])initialCounter.Clone();
            this.counter = (byte[])initialCounter.Clone();
        }

        /// <inheritdoc />
        public int Transform(ReadOnlySpan<byte> input, Span<byte> output, bool encrypt)
        {
            ThrowHelper.ThrowIfSpanLengthIsInsufficient(output, 0, input.Length);

            int blockSize = this.cipher.BlockSize;
            Span<byte> keystream = stackalloc byte[blockSize];

            for (int offset = 0; offset < input.Length; offset += blockSize)
            {
                if (this.counterWrapped)
                    throw new CryptographicException(
                        "The CTR counter has wrapped to its initial value. Continuing would reuse the keystream.");

                this.cipher.Encrypt(this.counter, keystream);
                this.IncrementCounter();

                int len = Math.Min(blockSize, input.Length - offset);
                for (int i = 0; i < len; i++)
                    output[offset + i] = (byte)(input[offset + i] ^ keystream[i]);
            }

            return input.Length;
        }

        // ── Private helpers ────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Increments the counter in big-endian (rightmost-byte-first) order, matching NIST SP 800-38A,
        /// then detects wrap-around.
        /// </summary>
        private void IncrementCounter()
        {
            for (int i = this.counter.Length - 1; i >= 0; i--)
                if (++this.counter[i] != 0) break;

            // Wrap detected: counter has returned to its initial value.
            if (this.counter.AsSpan().SequenceEqual(this.initialCounter))
                this.counterWrapped = true;
        }
    }
}
