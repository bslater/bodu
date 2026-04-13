namespace Bodu.Security.Cryptography
{
    using System;

    /// <summary>
    /// Applies the Electronic Codebook (ECB) mode transformation to an underlying <see cref="IBlockCipher" />, encrypting or decrypting
    /// each block independently with no chaining.
    /// </summary>
    /// <remarks>
    /// Encryption computes <c>Cᵢ = E(Pᵢ)</c> and decryption <c>Pᵢ = D(Cᵢ)</c>; no initialisation vector is used. ECB leaks structural
    /// information because identical plaintext blocks always yield identical ciphertext blocks, and it is insecure for virtually all
    /// real-world messages. Prefer CBC, CTR, or an authenticated mode unless ECB is required as a primitive inside a larger construction.
    /// </remarks>
    public sealed class EcbModeTransform : IBlockCipherModeTransform
    {
        private readonly IBlockCipher cipher;

        /// <summary>
        /// Initialises a new instance of the <see cref="EcbModeTransform" /> class that wraps the specified block cipher.
        /// </summary>
        /// <param name="cipher">The block cipher over which ECB is applied.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="cipher" /> is <see langword="null" />.</exception>
        public EcbModeTransform(IBlockCipher cipher)
        {
            this.cipher = cipher ?? throw new ArgumentNullException(nameof(cipher));
        }

        /// <inheritdoc />
        public int Transform(ReadOnlySpan<byte> input, Span<byte> output, bool encrypt)
        {
            int blockSize = this.cipher.BlockSize;

            ThrowHelper.ThrowIfSpanLengthNotPositiveMultipleOf(input, blockSize);
            ThrowHelper.ThrowIfSpanLengthIsInsufficient(output, 0, input.Length);

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