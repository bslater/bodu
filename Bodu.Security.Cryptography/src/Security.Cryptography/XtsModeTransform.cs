// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XtsModeTransform.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    using System;

    /// <summary>
    /// Applies the XEX-based Tweaked-Codebook mode with Ciphertext Stealing (XTS) transformation to an
    /// underlying <see cref="IBlockCipher" />, providing per-block tweaking suited to storage encryption.
    /// </summary>
    /// <remarks>
    /// <para>
    /// XTS processes each block with an independent tweak T_i derived from an IV (representing the sector
    /// number) and updated between consecutive blocks via GF(2^n) multiplication by α:
    /// <list type="bullet">
    /// <item><description>Encrypt: C_i = E(P_i ⊕ T_i) ⊕ T_i</description></item>
    /// <item><description>Decrypt: P_i = D(C_i ⊕ T_i) ⊕ T_i</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The initial tweak T_0 is derived by encrypting the IV: T_0 = E(IV). In a spec-compliant XTS
    /// implementation this uses a second independent key; here a single cipher instance is used for both
    /// the tweak derivation and the data transform. The encrypt primitive is used to compute T_0 and to
    /// encrypt each plaintext block; the decrypt primitive is used to decrypt each ciphertext block.
    /// </para>
    /// <para>
    /// This implementation targets 128-bit (16-byte) blocks. The GF(2^128) reduction polynomial is
    /// x^128 + x^7 + x^2 + x + 1 (constant 0x87), with bytes stored in little-endian order as required
    /// by IEEE 1619-2007.
    /// </para>
    /// </remarks>
    public sealed class XtsModeTransform : IBlockCipherModeTransform
    {
        private readonly IBlockCipher cipher;
        private readonly byte[] tweak; // current T_i, updated via GF multiplication after each block

        /// <summary>
        /// Initialises a new instance of the <see cref="XtsModeTransform" /> class.
        /// </summary>
        /// <param name="cipher">The block cipher used for both tweak derivation and data transformation.</param>
        /// <param name="iv">
        /// The initialisation vector representing the sector number. Must equal the cipher block size. A
        /// defensive copy is taken; the caller's array is not modified.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="cipher" /> or <paramref name="iv" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="iv" /> length does not equal the cipher block size.
        /// </exception>
        public XtsModeTransform(IBlockCipher cipher, byte[] iv)
        {
            this.cipher = cipher ?? throw new ArgumentNullException(nameof(cipher));
            if (iv is null) throw new ArgumentNullException(nameof(iv));
            if (iv.Length != cipher.BlockSize)
                throw new ArgumentException(
                    $"IV length ({iv.Length}) must equal the cipher block size ({cipher.BlockSize}).",
                    nameof(iv));

            // T_0 = E(IV) — the IV plays the role of the sector/tweak number.
            this.tweak = new byte[cipher.BlockSize];
            cipher.Encrypt(iv, this.tweak);
        }

        /// <inheritdoc />
        public int Transform(ReadOnlySpan<byte> input, Span<byte> output, bool encrypt)
        {
            int blockSize = this.cipher.BlockSize;
            ThrowHelper.ThrowIfSpanLengthNotPositiveMultipleOf(input, blockSize);
            ThrowHelper.ThrowIfSpanLengthIsInsufficient(output, 0, input.Length);

            Span<byte> tweaked = stackalloc byte[blockSize];

            for (int offset = 0; offset < input.Length; offset += blockSize)
            {
                ReadOnlySpan<byte> inBlock = input.Slice(offset, blockSize);
                Span<byte> outBlock = output.Slice(offset, blockSize);

                // Step 1: XOR input block with current tweak.
                for (int i = 0; i < blockSize; i++)
                    tweaked[i] = (byte)(inBlock[i] ^ this.tweak[i]);

                // Step 2: Apply cipher primitive (encrypt uses E, decrypt uses D).
                if (encrypt)
                    this.cipher.Encrypt(tweaked, outBlock);
                else
                    this.cipher.Decrypt(tweaked, outBlock);

                // Step 3: XOR output with current tweak.
                for (int i = 0; i < blockSize; i++)
                    outBlock[i] ^= this.tweak[i];

                // Step 4: Advance tweak T_{i+1} = T_i * α in GF(2^128), little-endian.
                GfMultiplyAlpha(this.tweak);
            }

            return input.Length;
        }

        /// <summary>
        /// Multiplies <paramref name="t" /> by the generator α in GF(2^128) using the IEEE 1619-2007
        /// reduction polynomial x^128 + x^7 + x^2 + x + 1 (constant 0x87). Bytes are treated as a
        /// 128-bit value in little-endian order: <c>t[0]</c> is the least significant byte.
        /// </summary>
        private static void GfMultiplyAlpha(byte[] t)
        {
            // The carry bit is the MSB of the most-significant byte (last byte in little-endian).
            bool carry = (t[t.Length - 1] & 0x80) != 0;

            // Left-shift the entire value by 1 bit (little-endian left = towards higher indices).
            for (int i = t.Length - 1; i > 0; i--)
                t[i] = (byte)((t[i] << 1) | (t[i - 1] >> 7));
            t[0] <<= 1;

            // If the carry bit was set, reduce by XOR-ing the low byte with the field constant.
            if (carry)
                t[0] ^= 0x87;
        }
    }
}
