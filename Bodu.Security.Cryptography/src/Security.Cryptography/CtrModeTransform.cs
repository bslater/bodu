namespace Bodu.Security.Cryptography
{
    using System;
    using System.Buffers.Binary;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Applies the Counter (CTR) mode transformation to an underlying <see cref="IBlockCipher" />, turning it into a parallelisable
    /// stream cipher in which encryption and decryption are identical operations.
    /// </summary>
    /// <remarks>
    /// The keystream is produced by encrypting a running counter block: <c>Kᵢ = E(counter + i)</c>, and the output is
    /// <c>Pᵢ ⊕ Kᵢ</c>. The initial counter block must equal the cipher block size in length and is typically split into a nonce and a
    /// counter portion by the caller. Reusing a <c>(key, counter)</c> pair across messages is catastrophic: the XOR of the two
    /// ciphertexts recovers the XOR of the plaintexts, so callers must ensure that every counter value is used at most once per key.
    /// </remarks>
    public sealed class CtrModeTransform : IBlockCipherModeTransform
    {
        private readonly IBlockCipher cipher;
        private readonly byte[] counter;

        /// <summary>
        /// Initialises a new instance of the <see cref="CtrModeTransform" /> class with the specified cipher and initial counter block.
        /// </summary>
        /// <param name="cipher">The block cipher used to generate the keystream.</param>
        /// <param name="initialCounter">The initial counter block. Its length must equal the cipher's block size. A defensive copy is taken.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="cipher" /> or <paramref name="initialCounter" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException">
        /// Thrown if the length of <paramref name="initialCounter" /> does not match the cipher's block size.
        /// </exception>
        public CtrModeTransform(IBlockCipher cipher, byte[] initialCounter)
        {
            this.cipher = cipher ?? throw new ArgumentNullException(nameof(cipher));
            ThrowHelper.ThrowIfArrayLengthIsInsufficient(initialCounter, cipher.BlockSize);

            this.counter = (byte[])initialCounter.Clone();
        }

        /// <inheritdoc />
        public int Transform(ReadOnlySpan<byte> input, Span<byte> output, bool encrypt)
        {
            int blockSize = this.cipher.BlockSize;

            ThrowHelper.ThrowIfSpanLengthNotPositiveMultipleOf(input, blockSize);
            ThrowHelper.ThrowIfSpanLengthIsInsufficient(output, 0, input.Length);

            Span<byte> keystreamBlock = stackalloc byte[blockSize];
            Span<byte> counterBlock = stackalloc byte[blockSize];

            for (int offset = 0; offset < input.Length; offset += blockSize)
            {
                input.Slice(offset, blockSize).CopyTo(output.Slice(offset, blockSize));

                this.counter.AsSpan().CopyTo(counterBlock);
                this.cipher.Encrypt(counterBlock, keystreamBlock);

                for (int i = 0; i < blockSize; i++)
                    output[offset + i] ^= keystreamBlock[i];

                this.IncrementCounter();
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