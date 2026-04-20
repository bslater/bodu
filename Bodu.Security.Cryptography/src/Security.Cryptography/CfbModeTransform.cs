namespace Bodu.Security.Cryptography
{
    using System;

    /// <summary>
    /// Applies the Cipher Feedback (CFB) mode transformation to an underlying <see cref="IBlockCipher" />, turning it into a
    /// self-synchronising stream cipher.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <img src="../images/diagrams/classic-modes.svg" alt="CFB panel — the previous ciphertext is fed back as the cipher input; its encryption produces a keystream XORed with plaintext." />
    /// </para>
    /// <para>
    /// Both directions use the cipher's encryption primitive: encryption computes <c>Cᵢ = Pᵢ ⊕ E(IVᵢ)</c> and decryption
    /// <c>Pᵢ = Cᵢ ⊕ E(IVᵢ)</c>, with <c>IV₀</c> supplied by the caller and <c>IVᵢ₊₁ = Cᵢ</c> for subsequent blocks.
    /// See <b>panel 3</b> of the diagram above: the dashed feedback lines carry ciphertext blocks back into the next cipher
    /// input — the cipher runs the same direction (encrypt) for both encryption and decryption, and the plaintext simply XORs
    /// into or out of the resulting keystream.
    /// </para>
    /// <para>
    /// The initialisation vector must equal the cipher block size in length and should be unique and unpredictable per message under a given key.
    /// </para>
    /// </remarks>
    public sealed class CfbModeTransform : IBlockCipherModeTransform
    {
        private readonly IBlockCipher cipher;
        private readonly byte[] currentIv;

        /// <summary>
        /// Initialises a new instance of the <see cref="CfbModeTransform" /> class with the specified cipher and initialisation vector.
        /// </summary>
        /// <param name="cipher">The block cipher over which CFB is applied.</param>
        /// <param name="iv">The initialisation vector used as the feedback register for the first block. A defensive copy is taken.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="cipher" /> or <paramref name="iv" /> is <see langword="null" />.</exception>
        public CfbModeTransform(IBlockCipher cipher, byte[] iv)
        {
            this.cipher = cipher ?? throw new ArgumentNullException(nameof(cipher));
            if (iv is null)
                throw new ArgumentNullException(nameof(iv));
            if (iv.Length != cipher.BlockSize)
                throw new ArgumentException(
                    $"IV length ({iv.Length}) must equal the cipher block size ({cipher.BlockSize}).",
                    nameof(iv));
            this.currentIv = (byte[])iv.Clone();
        }

        /// <inheritdoc />
        public int Transform(ReadOnlySpan<byte> input, Span<byte> output, bool encrypt)
        {
            int blockSize = this.cipher.BlockSize;

            ThrowHelper.ThrowIfSpanLengthNotPositiveMultipleOf(input, blockSize);
            ThrowHelper.ThrowIfSpanLengthIsInsufficient(output, 0, input.Length);

            Span<byte> feedback = stackalloc byte[blockSize];

            for (int offset = 0; offset < input.Length; offset += blockSize)
            {
                ReadOnlySpan<byte> inBlock = input.Slice(offset, blockSize);
                Span<byte> outBlock = output.Slice(offset, blockSize);

                // Encrypt the current IV (used as feedback input)
                this.cipher.Encrypt(this.currentIv, feedback);

                if (encrypt)
                {
                    // XOR plaintext with encrypted feedback to produce ciphertext
                    for (int i = 0; i < blockSize; i++)
                        outBlock[i] = (byte)(inBlock[i] ^ feedback[i]);

                    // Update IV to current ciphertext block
                    outBlock.CopyTo(this.currentIv);
                }
                else
                {
                    // XOR ciphertext with encrypted feedback to produce plaintext
                    for (int i = 0; i < blockSize; i++)
                        outBlock[i] = (byte)(inBlock[i] ^ feedback[i]);

                    // Update IV to current ciphertext block
                    inBlock.CopyTo(this.currentIv);
                }
            }

            return input.Length;
        }
    }
}