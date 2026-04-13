namespace Bodu.Security.Cryptography
{
    using System;

    /// <summary>
    /// Applies the Output Feedback (OFB) mode transformation to an underlying <see cref="IBlockCipher" />, turning it into a synchronous
    /// stream cipher in which encryption and decryption are identical operations.
    /// </summary>
    /// <remarks>
    /// The keystream is produced by repeatedly encrypting the feedback register: <c>Oᵢ = E(Oᵢ₋₁)</c> with <c>O₀ = IV</c>, and the output
    /// is <c>Pᵢ ⊕ Oᵢ</c>. The initialisation vector must equal the cipher block size in length and must never be reused under the same
    /// key, otherwise keystreams collide and confidentiality is lost.
    /// </remarks>
    public sealed class OfbModeTransform : IBlockCipherModeTransform
    {
        private readonly IBlockCipher cipher;
        private readonly byte[] currentIv;

        /// <summary>
        /// Initialises a new instance of the <see cref="OfbModeTransform" /> class with the specified cipher and initialisation vector.
        /// </summary>
        /// <param name="cipher">The block cipher used to generate the keystream.</param>
        /// <param name="iv">The initialisation vector used to seed the feedback register. A defensive copy is taken.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="cipher" /> or <paramref name="iv" /> is <see langword="null" />.</exception>
        public OfbModeTransform(IBlockCipher cipher, byte[] iv)
        {
            this.cipher = cipher ?? throw new ArgumentNullException(nameof(cipher));
            this.currentIv = (byte[])iv?.Clone() ?? throw new ArgumentNullException(nameof(cipher));
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