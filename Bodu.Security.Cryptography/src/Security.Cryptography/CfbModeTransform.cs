namespace Bodu.Security.Cryptography
{
    using System;

    /// <summary>
    /// Applies the Cipher Feedback (CFB) mode transformation to an underlying <see cref="IBlockCipher" />, turning it into a
    /// self-synchronising stream cipher.
    /// </summary>
    /// <remarks>
    /// Both directions use the cipher's encryption primitive: encryption computes <c>Cᵢ = Pᵢ ⊕ E(IVᵢ)</c> and decryption
    /// <c>Pᵢ = Cᵢ ⊕ E(IVᵢ)</c>, with <c>IV₀</c> supplied by the caller and <c>IVᵢ₊₁ = Cᵢ</c> for subsequent blocks. The initialisation
    /// vector must equal the cipher block size in length and should be unique and unpredictable per message under a given key.
    /// </remarks>
    public sealed class CfbModeTransform : IBlockCipherModeTransform
    {
        private readonly IBlockCipher cipher;
        private readonly byte[] currentIv;

        /// <summary>
        /// Initializes a new instance of the <see cref="CfbModeTransform" /> class with the specified block cipher and initialization vector.
        /// </summary>
        /// <param name="cipher">The block cipher used for transformation.</param>
        /// <param name="iv">The initialization vector used for the first block.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="cipher" /> or <paramref name="iv" /> is <see langword="null" />.</exception>
        public CfbModeTransform(IBlockCipher cipher, byte[] iv)
        {
            this.cipher = cipher ?? throw new ArgumentNullException(nameof(cipher));
            this.currentIv = (byte[])iv.Clone();
        }

        /// <summary>
        /// Transforms data using CFB mode for either encryption or decryption.
        /// </summary>
        /// <param name="input">The input data to transform. Must be a multiple of the cipher block size.</param>
        /// <param name="output">The buffer to receive the transformed output. Must be at least <paramref name="input" /> length.</param>
        /// <param name="encrypt"><c>true</c> to encrypt the input; <c>false</c> to decrypt.</param>
        /// <returns>The number of bytes written to <paramref name="output" />.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="input" /> length is not a multiple of the block size, or if <paramref name="output" /> is too small.
        /// </exception>
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