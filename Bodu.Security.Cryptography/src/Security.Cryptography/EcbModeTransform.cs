using System;

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Performs encryption and decryption using Electronic Codebook (ECB) mode for a given block cipher.
    /// </summary>
    /// <remarks>
    /// ECB mode is the simplest form of block cipher operation. Each block of plaintext is independently encrypted into ciphertext, and
    /// vice versa. It does not use an initialization vector (IV) and provides no diffusion across blocks.
    /// <para>
    /// WARNING: ECB mode is generally insecure for most cryptographic purposes because identical plaintext blocks result in identical
    /// ciphertext blocks, making it vulnerable to pattern analysis. Use CBC or another mode for secure applications.
    /// </para>
    /// </remarks>
    public sealed class EcbModeTransform : IBlockCipherModeTransform
    {
        private readonly IBlockCipher cipher;

        /// <summary>
        /// Initializes a new instance of the <see cref="EcbModeTransform" /> class with the specified block cipher.
        /// </summary>
        /// <param name="cipher">The block cipher used for transformation.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="cipher" /> is <see langword="null" />.</exception>
        public EcbModeTransform(IBlockCipher cipher)
        {
            this.cipher = cipher ?? throw new ArgumentNullException(nameof(cipher));
        }

        /// <summary>
        /// Transforms the input data using ECB mode for either encryption or decryption.
        /// </summary>
        /// <param name="input">The input data to transform. Must be a multiple of the cipher block size.</param>
        /// <param name="output">The buffer to write the transformed data to. Must be at least the size of <paramref name="input" />.</param>
        /// <param name="encrypt"><c>true</c> to encrypt; <c>false</c> to decrypt.</param>
        /// <returns>The number of bytes written to <paramref name="output" />.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown if the input length is not a multiple of the block size, or if the output span is too small.
        /// </exception>
        public int Transform(ReadOnlySpan<byte> input, Span<byte> output, bool encrypt)
        {
            int blockSize = this.cipher.BlockSize;

            ThrowHelper.ThrowIfSpanLengthNotPositiveMultipleOf(input, blockSize);
            ThrowHelper.ThrowIfArrayLengthIsInsufficient(output, 0, input.Length);

            for (int offset = 0; offset < input.Length; offset += blockSize)
            {
                ReadOnlySpan<byte> inBlock = input.Slice(offset, blockSize);
                Span<byte> outBlock = output.Slice(offset, blockSize);

                if (encrypt)
                    this.cipher.Encrypt(inBlock, outBlock);
                else
                    this.cipher.Decrypt(inBlock, outBlock);
            }

            return input.Length;
        }
    }
}