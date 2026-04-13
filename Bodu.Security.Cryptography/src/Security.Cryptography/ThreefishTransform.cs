namespace Bodu.Security.Cryptography
{
    using System;
    using System.Security.Cryptography;

    /// <summary>
    /// Performs cryptographic transformations using the Threefish block cipher algorithm. Supports encryption and decryption in CBC mode
    /// with configurable padding.
    /// </summary>
    /// <remarks>
    /// This class integrates a block cipher engine ( <see cref="IBlockCipher" />) with a cipher mode (
    /// <see cref="IBlockCipherModeTransform" />) and padding scheme ( <see cref="IPaddingStrategy" />). It supports both streaming (via
    /// <see cref="TransformBlock" />) and final block processing (via <see cref="TransformFinalBlock" />), following the
    /// <see cref="ICryptoTransform" /> contract.
    /// </remarks>
    public sealed class ThreefishTransform : ICryptoTransform
    {
        private readonly IBlockCipher cipher;

        private readonly bool encrypt;

        private readonly IBlockCipherModeTransform mode;

        private readonly IPaddingStrategy padding;

        private byte[]? deferredInput;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreefishTransform" /> class using the specified cipher, mode, padding, and
        /// initialization vector.
        /// </summary>
        /// <param name="cipher">The block cipher engine to use for encryption or decryption.</param>
        /// <param name="cipherMode">The block cipher mode of operation (e.g., CBC, CFB).</param>
        /// <param name="paddingMode">The padding scheme to apply to input data.</param>
        /// <param name="iv">The initialization vector to use for the block cipher mode.</param>
        /// <param name="encrypt"><c>true</c> to encrypt; <c>false</c> to decrypt.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="cipher" /> or <paramref name="iv" /> is <see langword="null" />.</exception>
        public ThreefishTransform(IBlockCipher cipher, CipherBlockMode cipherMode, PaddingMode paddingMode, byte[] iv, bool encrypt)
        {
            this.cipher = cipher ?? throw new ArgumentNullException(nameof(cipher));
            this.encrypt = encrypt;
            this.mode = BlockCipherModeFactory.Create(cipherMode, cipher, iv);
            this.padding = PaddingFactory.Create(paddingMode);
        }

        /// <inheritdoc />
        public bool CanReuseTransform => false;

        /// <inheritdoc />
        public bool CanTransformMultipleBlocks => true;

        /// <inheritdoc />
        public int InputBlockSize => this.cipher.BlockSize;

        /// <inheritdoc />
        public int OutputBlockSize => this.cipher.BlockSize;

        /// <inheritdoc />
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        /// <summary>
        /// Transforms a block of bytes and writes the output to the specified buffer.
        /// </summary>
        /// <param name="inputBuffer">The input data buffer.</param>
        /// <param name="inputOffset">The byte offset into <paramref name="inputBuffer" /> to begin reading from.</param>
        /// <param name="inputCount">The number of bytes to read from <paramref name="inputBuffer" />.</param>
        /// <param name="outputBuffer">The buffer to write the transformed data to.</param>
        /// <param name="outputOffset">The byte offset into <paramref name="outputBuffer" /> to begin writing at.</param>
        /// <returns>The number of bytes written to <paramref name="outputBuffer" />.</returns>
        /// <exception cref="ArgumentException">Thrown if the input or output spans are invalid or insufficient in length.</exception>
        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount,
                                  byte[] outputBuffer, int outputOffset)
        {
            ReadOnlySpan<byte> input = inputBuffer.AsSpan(inputOffset, inputCount);
            Span<byte> output = outputBuffer.AsSpan(outputOffset, inputCount);

            if (this.encrypt)
            {
                // ENCRYPTION: transform all blocks immediately
                return this.mode.Transform(input, output, true);
            }
            else
            {
                // DECRYPTION: defer final block to allow padding validation
                bool stripPadding = this.padding is Pkcs7Padding; // Extend with other depaddable schemes

                if (stripPadding && input.Length <= this.cipher.BlockSize)
                {
                    // Buffer the last block until finalization
                    this.deferredInput = input.ToArray();
                    return 0;
                }

                int bytesToProcess = input.Length;
                if (stripPadding)
                {
                    // Retain last block for padding removal later
                    bytesToProcess -= this.cipher.BlockSize;
                    this.deferredInput = input.Slice(bytesToProcess).ToArray();
                }

                return this.mode.Transform(input.Slice(0, bytesToProcess), output.Slice(0, bytesToProcess), false);
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// Transforms the final block of data, applying padding (or removing it, if decrypting).
        /// </summary>
        /// <param name="inputBuffer">The final portion of input data to transform.</param>
        /// <param name="inputOffset">The byte offset in the input buffer to begin reading from.</param>
        /// <param name="inputCount">The number of bytes to read from <paramref name="inputBuffer" />.</param>
        /// <returns>A new array containing the transformed final block.</returns>
        /// <exception cref="CryptographicException">Thrown if the padding is invalid during decryption.</exception>
        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            ReadOnlySpan<byte> input = inputBuffer.AsSpan(inputOffset, inputCount);

            if (this.encrypt)
            {
                byte[] padded = this.padding.Pad(input, this.cipher.BlockSize);
                byte[] output = new byte[padded.Length];
                this.mode.Transform(padded, output, true);
                return output;
            }
            else
            {
                byte[] combined = Combine(this.deferredInput, input);
                byte[] decrypted = new byte[combined.Length];
                this.mode.Transform(combined, decrypted, false);
                return this.padding.Unpad(decrypted, this.cipher.BlockSize);
            }
        }

        /// <summary>
        /// Combines a deferred block with a new input span to produce a single contiguous byte array.
        /// </summary>
        /// <param name="first">The previously cached partial block or <see langword="null" />.</param>
        /// <param name="second">The incoming data to append.</param>
        /// <returns>A new array containing the concatenated data.</returns>
        private static byte[] Combine(byte[]? first, ReadOnlySpan<byte> second)
        {
            if (first == null || first.Length == 0)
                return second.ToArray();

            byte[] result = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, result, 0, first.Length);
            second.CopyTo(result.AsSpan(first.Length));
            return result;
        }
    }
}