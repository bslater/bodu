using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Performs encryption and decryption using Counter (CTR) mode for a given block cipher.
    /// </summary>
    /// <remarks>
    /// CTR mode turns a block cipher into a stream cipher by encrypting successive values of a counter and XORing them with the input. The
    /// counter value is typically formed from a nonce and an incrementing counter per block.
    /// <para>CTR mode supports parallelism, random access to encrypted blocks, and symmetrical encryption/decryption.</para>
    /// </remarks>
    public sealed class CtrModeTransform : IBlockCipherModeTransform
    {
        private readonly IBlockCipher cipher;
        private readonly byte[] counter;

        /// <summary>
        /// Initializes a new instance of the <see cref="CtrModeTransform" /> class with the specified cipher and initial counter.
        /// </summary>
        /// <param name="cipher">The block cipher used to generate the keystream.</param>
        /// <param name="initialCounter">The initial counter value. Must match the cipher's block size in length.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="cipher" /> or <paramref name="initialCounter" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="initialCounter" /> length does not match the cipher's block size.
        /// </exception>
        public CtrModeTransform(IBlockCipher cipher, byte[] initialCounter)
        {
            this.cipher = cipher ?? throw new ArgumentNullException(nameof(cipher));
            ThrowHelper.ThrowIfArrayLengthIsInsufficient(initialCounter, cipher.BlockSize);

            this.counter = (byte[])initialCounter.Clone();
        }

        /// <summary>
        /// Transforms the input buffer using CTR mode. Encryption and decryption are identical in CTR mode.
        /// </summary>
        /// <param name="input">The input data to transform. Must be a multiple of the cipher's block size.</param>
        /// <param name="output">The buffer to write the transformed result. Must be at least as long as <paramref name="input" />.</param>
        /// <param name="encrypt"><c>true</c> for encryption, <c>false</c> for decryption (same operation).</param>
        /// <returns>The number of bytes written to <paramref name="output" />.</returns>
        /// <exception cref="ArgumentException">Thrown if the input is not a multiple of the block size, or the output is too small.</exception>
        public int Transform(ReadOnlySpan<byte> input, Span<byte> output, bool encrypt)
        {
            int blockSize = this.cipher.BlockSize;

            ThrowHelper.ThrowIfSpanLengthNotPositiveMultipleOf(input, blockSize);
            ThrowHelper.ThrowIfArrayLengthIsInsufficient(output, 0, input.Length);

            Span<byte> keystreamBlock = stackalloc byte[blockSize];
            Span<byte> counterBlock = stackalloc byte[blockSize];

            for (int offset = 0; offset < input.Length; offset += blockSize)
            {
                input.Slice(offset, blockSize).CopyTo(output.Slice(offset, blockSize));

                this.counter.AsSpan().CopyTo(counterBlock);
                this.cipher.Encrypt(counterBlock, keystreamBlock);

                for (int i = 0; i < blockSize; i++)
                    output[offset + i] ^= keystreamBlock[i];

                IncrementCounter();
            }

            return input.Length;
        }

        /// <summary>
        /// Increments the counter in-place in little-endian order.
        /// </summary>
        private void IncrementCounter()
        {
            Span<byte> span = this.counter;

            for (int i = 0; i < span.Length; i++)
            {
                if (++span[i] != 0)
                    break;
            }
        }
    }
}