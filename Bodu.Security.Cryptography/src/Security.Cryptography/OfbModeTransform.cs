namespace Bodu.Security.Cryptography
{
    using System;

    /// <summary>
    /// Performs encryption and decryption using Output Feedback (OFB) mode for a given block cipher.
    /// </summary>
    /// <remarks>
    /// OFB mode converts a block cipher into a synchronous stream cipher. It repeatedly encrypts the IV (or feedback register) and XORs the
    /// result with the plaintext or ciphertext to produce output. Since encryption and decryption are symmetric, the same operation is
    /// applied in both directions.
    /// <para>
    /// OFB mode preserves error propagation and allows for parallelizable keystream generation, but requires that IVs be unique and
    /// unpredictable for each encryption session.
    /// </para>
    /// </remarks>
    public sealed class OfbModeTransform : IBlockCipherModeTransform
    {
        private readonly IBlockCipher cipher;
        private readonly byte[] currentIv;

        /// <summary>
        /// Initializes a new instance of the <see cref="OfbModeTransform" /> class with the specified cipher and initialization vector.
        /// </summary>
        /// <param name="cipher">The block cipher used for encryption and decryption.</param>
        /// <param name="iv">The initialization vector used to generate the keystream.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="cipher" /> or <paramref name="iv" /> is <see langword="null" />.</exception>
        public OfbModeTransform(IBlockCipher cipher, byte[] iv)
        {
            this.cipher = cipher ?? throw new ArgumentNullException(nameof(cipher));
            this.currentIv = (byte[])iv?.Clone() ?? throw new ArgumentNullException(nameof(cipher));
        }

        /// <summary>
        /// Transforms the input buffer using OFB mode. This method applies the same operation for both encryption and decryption.
        /// </summary>
        /// <param name="input">The input buffer to transform. Must be a multiple of the cipher block size.</param>
        /// <param name="output">The buffer to receive the transformed data. Must be at least as long as <paramref name="input" />.</param>
        /// <param name="encrypt"><c>true</c> for encryption, <c>false</c> for decryption (identical operation).</param>
        /// <returns>The number of bytes written to <paramref name="output" />.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown if the input length is not a positive multiple of the block size, or if the output span is too small.
        /// </exception>
        public int Transform(ReadOnlySpan<byte> input, Span<byte> output, bool encrypt)
        {
            int blockSize = this.cipher.BlockSize;

            ThrowHelper.ThrowIfSpanLengthNotPositiveMultipleOf(input, blockSize);
            ThrowHelper.ThrowIfSpanLengthIsInsufficient(output, 0, input.Length);

            Span<byte> keystream = stackalloc byte[blockSize];

            for (int offset = 0; offset < input.Length; offset += blockSize)
            {
                ReadOnlySpan<byte> inBlock = input.Slice(offset, blockSize);
                Span<byte> outBlock = output.Slice(offset, blockSize);

                // Encrypt the feedback register to generate keystream
                this.cipher.Encrypt(this.currentIv, keystream);

                // XOR keystream with plaintext or ciphertext
                for (int i = 0; i < blockSize; i++)
                    outBlock[i] = (byte)(inBlock[i] ^ keystream[i]);

                // Update feedback register with generated keystream
                keystream.CopyTo(this.currentIv);
            }

            return input.Length;
        }
    }
}