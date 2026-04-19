// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SkipjackTransform.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    using System;
    using System.Security.Cryptography;

    /// <summary>
    /// Performs a cryptographic transformation of data using the <see cref="Skipjack" /> algorithm. This class cannot be inherited.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class integrates a <see cref="SkipjackBlockCipher" /> engine with an <see cref="IBlockCipherModeTransform" /> and an
    /// <see cref="IPaddingStrategy" />. Block-aligned streaming data is processed via <see cref="TransformBlock" />, and the final
    /// (potentially partial) block, including padding application or removal, is handled by <see cref="TransformFinalBlock" />.
    /// </para>
    /// <para>
    /// Instances of this class are returned by <see cref="Skipjack.CreateEncryptor(byte[], byte[])" /> and
    /// <see cref="Skipjack.CreateDecryptor(byte[], byte[])" />. Using this class directly is not recommended; prefer using
    /// <see cref="Skipjack" /> with a <see cref="CryptoStream" />, which handles padding and block alignment automatically.
    /// </para>
    /// </remarks>
    public sealed class SkipjackTransform : ICryptoTransform
    {
        private readonly IBlockCipher cipher;
        private readonly bool encrypt;
        private readonly IBlockCipherModeTransform mode;
        private readonly IPaddingStrategy padding;
        private byte[]? deferredInput;

        private bool disposed;

        /// <summary>
        /// Initialises a new instance of the <see cref="SkipjackTransform" /> class using the specified cipher, mode, padding, and
        /// initialisation vector.
        /// </summary>
        /// <param name="cipher">The configured <see cref="IBlockCipher" /> engine to use. Must not be <see langword="null" />.</param>
        /// <param name="cipherMode">The block cipher mode of operation (for example, <see cref="CipherBlockMode.CBC" />).</param>
        /// <param name="paddingMode">The padding scheme to apply to the final block.</param>
        /// <param name="iv">The initialisation vector for the cipher mode. Must match the cipher block size.</param>
        /// <param name="encrypt"><see langword="true" /> to configure for encryption; <see langword="false" /> for decryption.</param>
        /// <exception cref="ArgumentNullException"><paramref name="cipher" /> is <see langword="null" />.</exception>
        public SkipjackTransform(IBlockCipher cipher, CipherBlockMode cipherMode, PaddingMode paddingMode, byte[] iv, bool encrypt)
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
            if (this.disposed)
                return;

            if (this.deferredInput is not null)
            {
                CryptographicOperations.ZeroMemory(this.deferredInput);
                this.deferredInput = null;
            }

            this.cipher.Dispose();
            this.disposed = true;
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Transforms a block-aligned region of the input byte array and writes the result to the output buffer.
        /// </summary>
        /// <param name="inputBuffer">The input data buffer. Must not be <see langword="null" />.</param>
        /// <param name="inputOffset">The byte offset within <paramref name="inputBuffer" /> at which to begin reading.</param>
        /// <param name="inputCount">The number of bytes to process. Must be a multiple of <see cref="InputBlockSize" />.</param>
        /// <param name="outputBuffer">The buffer to write the transformed data into. Must not be <see langword="null" />.</param>
        /// <param name="outputOffset">The byte offset within <paramref name="outputBuffer" /> at which to begin writing.</param>
        /// <returns>The number of bytes written to <paramref name="outputBuffer" />.</returns>
        /// <exception cref="ArgumentException">
        /// The input or output buffer span is invalid or insufficient in length for the requested operation.
        /// </exception>
        /// <remarks>
        /// When decrypting with a strippable padding mode (for example <see cref="PaddingMode.PKCS7" />), the last complete block of input
        /// is deferred and not written to the output until <see cref="TransformFinalBlock" /> is called. This allows correct padding
        /// removal at the end of the stream.
        /// </remarks>
        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount,
                                  byte[] outputBuffer, int outputOffset)
        {
            ObjectDisposedException.ThrowIf(this.disposed, this);

            // Fix: enforce ICryptoTransform null-argument contract (previously threw NullReferenceException via .AsSpan).
            ThrowHelper.ThrowIfNull(inputBuffer);
            ThrowHelper.ThrowIfNull(outputBuffer);

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
            ObjectDisposedException.ThrowIf(this.disposed, this);

            // Fix: enforce ICryptoTransform null-argument contract (previously threw NullReferenceException via .AsSpan).
            ThrowHelper.ThrowIfNull(inputBuffer);

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