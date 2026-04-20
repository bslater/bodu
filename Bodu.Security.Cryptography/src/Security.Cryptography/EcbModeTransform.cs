namespace Bodu.Security.Cryptography
{
    using System;

    /// <summary>
    /// Applies the Electronic Codebook (ECB) mode transformation to an underlying <see cref="IBlockCipher" />, encrypting or decrypting
    /// each block independently with no chaining.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <img src="../images/diagrams/classic-modes.svg" alt="ECB panel — each plaintext block is encrypted independently to its ciphertext block with no feedback." />
    /// </para>
    /// <para>
    /// Encryption computes <c>Cᵢ = E(Pᵢ)</c> and decryption <c>Pᵢ = D(Cᵢ)</c>; no initialisation vector is used.
    /// See <b>panel 1</b> of the diagram above: each column is entirely self-contained, so the three cells carry
    /// no arrows between them.
    /// </para>
    /// <para>
    /// That independence is exactly what makes ECB insecure for virtually all real-world messages: identical plaintext
    /// blocks always yield identical ciphertext blocks, leaking structural information. Prefer CBC, CTR, or an
    /// authenticated mode unless ECB is required as a primitive inside a larger construction.
    /// </para>
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