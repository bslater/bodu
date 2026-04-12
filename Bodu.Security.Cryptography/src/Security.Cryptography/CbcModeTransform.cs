using System;
using Bodu.Security.Cryptography;

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Implements the Cipher Block Chaining (CBC) mode transformation for a block cipher.
    /// </summary>
    /// <remarks>
    /// CBC mode XORs each plaintext block with the previous ciphertext block before encryption. The first block uses the initialization
    /// vector (IV) in place of a previous ciphertext block. On decryption, the reverse is applied: the decrypted block is XORed with the
    /// previous ciphertext.
    /// </remarks>
    public sealed class CbcModeTransform
        : IBlockCipherModeTransform
    {
        private readonly IBlockCipher cipher;
        private readonly byte[] currentIv;

        /// <summary>
        /// Initializes a new instance of the <see cref="CbcModeTransform" /> class with the specified cipher and initialization vector (IV).
        /// </summary>
        /// <param name="cipher">The block cipher to apply CBC mode to.</param>
        /// <param name="iv">The initialization vector (IV) to use for the first block.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="cipher" /> or <paramref name="iv" /> is <see langword="null" />.</exception>
        public CbcModeTransform(IBlockCipher cipher, byte[] iv)
        {
            this.cipher = cipher ?? throw new ArgumentNullException(nameof(cipher));
            this.currentIv = (byte[])iv.Clone(); // Used to track the evolving IV during transformation
        }

        /// <summary>
        /// Transforms the input data using CBC mode, performing either encryption or decryption.
        /// </summary>
        /// <param name="input">The input data to transform. Must be a multiple of the block size.</param>
        /// <param name="output">The buffer to write the transformed data to. Must be at least the size of <paramref name="input" />.</param>
        /// <param name="encrypt"><c>true</c> to encrypt; <c>false</c> to decrypt.</param>
        /// <returns>The number of bytes written to <paramref name="output" />.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown if the input length is not a multiple of the block size, or the output buffer is too small.
        /// </exception>
        public int Transform(ReadOnlySpan<byte> input, Span<byte> output, bool encrypt)
        {
            int blockSize = this.cipher.BlockSize;

            ThrowHelper.ThrowIfSpanLengthNotPositiveMultipleOf(input, blockSize, throwIfZero: false);
            ThrowHelper.ThrowIfArrayLengthIsInsufficient(output, 0, input.Length);

            Span<byte> tempBlock = stackalloc byte[blockSize];

            for (int offset = 0; offset < input.Length; offset += blockSize)
            {
                ReadOnlySpan<byte> inBlock = input.Slice(offset, blockSize);
                Span<byte> outBlock = output.Slice(offset, blockSize);

                if (encrypt)
                {
                    // Encrypt: XOR input with IV, then encrypt
                    for (int i = 0; i < blockSize; i++)
                        tempBlock[i] = (byte)(inBlock[i] ^ this.currentIv[i]);

                    this.cipher.Encrypt(tempBlock, outBlock);

                    // Update IV to the current ciphertext block
                    outBlock.CopyTo(this.currentIv);
                }
                else
                {
                    // Decrypt: store current ciphertext block, decrypt, then XOR with IV
                    inBlock.CopyTo(tempBlock);

                    this.cipher.Decrypt(inBlock, outBlock);

                    for (int i = 0; i < blockSize; i++)
                        outBlock[i] ^= this.currentIv[i];

                    // Update IV to original ciphertext block
                    tempBlock.CopyTo(this.currentIv);
                }
            }

            return input.Length;
        }
    }
}