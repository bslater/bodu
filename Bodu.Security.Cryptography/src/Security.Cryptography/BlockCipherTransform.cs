// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockCipherTransform.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    using System;
    using System.Security.Cryptography;

    /// <summary>
    /// Provides a reusable base implementation of <see cref="ICryptoTransform" /> for block cipher algorithms that
    /// combine an <see cref="IBlockCipher" /> engine with a <see cref="IBlockCipherModeTransform" /> and an
    /// <see cref="IPaddingStrategy" />.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Block-aligned streaming data is processed via <see cref="TransformBlock" />, and the final (potentially
    /// partial) block — including padding application or removal — is handled by <see cref="TransformFinalBlock" />.
    /// </para>
    /// <para>
    /// When decrypting with a strippable padding mode (for example <see cref="PaddingMode.PKCS7" />), the last
    /// complete block of input is deferred until <see cref="TransformFinalBlock" /> is called to allow correct
    /// padding validation and removal at the boundary of the stream.
    /// </para>
    /// <para>
    /// Derived classes need only provide a constructor that calls
    /// <see cref="BlockCipherTransform(IBlockCipher, CipherBlockMode, PaddingMode, byte[], bool)" /> with the
    /// appropriate arguments. All transform logic is handled by this base class.
    /// </para>
    /// </remarks>
    public abstract class BlockCipherTransform : ICryptoTransform
    {
        private readonly IBlockCipher cipher;
        private readonly bool encrypt;
        private readonly IBlockCipherModeTransform mode;
        private readonly IPaddingStrategy padding;

        private byte[]? deferredInput;
        private bool disposed;

        /// <summary>
        /// Initialises a new instance of the <see cref="BlockCipherTransform" /> class using the specified cipher
        /// engine, mode, padding scheme, initialisation vector, and transform direction.
        /// </summary>
        /// <param name="cipher">
        /// The configured <see cref="IBlockCipher" /> engine to use. Must not be <see langword="null" />.
        /// </param>
        /// <param name="cipherMode">The block cipher mode of operation (for example, <see cref="CipherBlockMode.CBC" />).</param>
        /// <param name="paddingMode">The padding scheme to apply to the final block.</param>
        /// <param name="iv">The initialisation vector for the cipher mode. Must match the cipher block size.</param>
        /// <param name="encrypt">
        /// <see langword="true" /> to configure for encryption; <see langword="false" /> for decryption.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="cipher" /> is <see langword="null" />.</exception>
        protected BlockCipherTransform(IBlockCipher cipher, CipherBlockMode cipherMode, PaddingMode paddingMode, byte[] iv, bool encrypt)
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
        /// <exception cref="ObjectDisposedException">This instance has been disposed.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="inputBuffer" /> or <paramref name="outputBuffer" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The input or output buffer span is invalid or insufficient in length for the requested operation.
        /// </exception>
        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount,
                                  byte[] outputBuffer, int outputOffset)
        {
            ObjectDisposedException.ThrowIf(this.disposed, this);
            ThrowHelper.ThrowIfNull(inputBuffer);
            ThrowHelper.ThrowIfNull(outputBuffer);

            ReadOnlySpan<byte> input = inputBuffer.AsSpan(inputOffset, inputCount);
            Span<byte> output = outputBuffer.AsSpan(outputOffset, inputCount);

            if (this.encrypt)
            {
                return this.mode.Transform(input, output, true);
            }
            else
            {
                bool stripPadding = this.padding is Pkcs7Padding;

                if (stripPadding && input.Length <= this.cipher.BlockSize)
                {
                    this.deferredInput = input.ToArray();
                    return 0;
                }

                int bytesToProcess = input.Length;
                if (stripPadding)
                {
                    bytesToProcess -= this.cipher.BlockSize;
                    this.deferredInput = input.Slice(bytesToProcess).ToArray();
                }

                return this.mode.Transform(input.Slice(0, bytesToProcess), output.Slice(0, bytesToProcess), false);
            }
        }

        /// <summary>
        /// Transforms the final block of data, applying or removing padding as appropriate, and returns the result.
        /// </summary>
        /// <param name="inputBuffer">The final input data buffer. Must not be <see langword="null" />.</param>
        /// <param name="inputOffset">The byte offset within <paramref name="inputBuffer" /> at which to begin reading.</param>
        /// <param name="inputCount">The number of bytes to process from <paramref name="inputBuffer" />.</param>
        /// <returns>A new byte array containing the transformed and padded (or depadded) final block.</returns>
        /// <exception cref="ObjectDisposedException">This instance has been disposed.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="inputBuffer" /> is <see langword="null" />.</exception>
        /// <exception cref="CryptographicException">The padding is invalid or cannot be removed during decryption.</exception>
        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            ObjectDisposedException.ThrowIf(this.disposed, this);
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
        /// Concatenates an optional deferred byte array with an incoming input span into a single contiguous byte array.
        /// </summary>
        /// <param name="first">The previously cached partial or complete block, or <see langword="null" /> if none was deferred.</param>
        /// <param name="second">The newly arriving data to append.</param>
        /// <returns>
        /// A new byte array containing <paramref name="first" /> followed by <paramref name="second" />, or a copy of
        /// <paramref name="second" /> alone if <paramref name="first" /> is <see langword="null" /> or empty.
        /// </returns>
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
