// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlowfishTransform.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Performs a cryptographic transformation of data using the <see cref="Blowfish" /> algorithm. This class cannot be inherited.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Using this class directly is not recommended. The preferred approach is to use <see cref="Blowfish" /> with a
    /// <see cref="CryptoStream" />, which handles padding and block alignment automatically.
    /// </para>
    /// <para>
    /// Both <see cref="Blowfish.CreateEncryptor(byte[], byte[])" /> and <see cref="Blowfish.CreateDecryptor(byte[], byte[])" /> return an
    /// instance of this class configured with the appropriate key material, cipher mode, padding scheme, and transform direction. To
    /// encrypt or decrypt data, pass the returned <see cref="BlowfishTransform" /> instance to a <see cref="CryptoStream" />.
    /// </para>
    /// <para>
    /// This class integrates a <see cref="BlowfishBlockCipher" /> engine with an <see cref="IBlockCipherModeTransform" /> and an
    /// <see cref="IPaddingStrategy" />. Block-aligned streaming data is processed via <see cref="TransformBlock" />, and the final
    /// (potentially partial) block — including padding application or removal — is handled by <see cref="TransformFinalBlock" />.
    /// </para>
    /// <para>
    /// When decrypting, the final ciphertext block is deferred until <see cref="TransformFinalBlock" /> is called to allow correct
    /// padding validation and removal.
    /// </para>
    /// </remarks>
    public sealed partial class BlowfishTransform
        : System.Security.Cryptography.ICryptoTransform
    {
        private readonly IBlockCipher cipher;
        private readonly bool encrypt;
        private readonly IBlockCipherModeTransform mode;
        private readonly IPaddingStrategy padding;

        // Holds the last complete ciphertext block when decrypting with strippable padding,
        // deferred until TransformFinalBlock can confirm and remove padding correctly.
        private byte[]? deferredInput;

        private bool disposed;

        /// <summary>
        /// Initialises a new instance of the <see cref="BlowfishTransform" /> class.
        /// </summary>
        /// <param name="cipher">
        /// The configured <see cref="BlowfishBlockCipher" /> engine to use. Must not be <see langword="null" />.
        /// </param>
        /// <param name="cipherMode">The block cipher mode of operation (e.g., CBC, ECB, CFB).</param>
        /// <param name="paddingMode">The padding scheme to apply to the final block.</param>
        /// <param name="iv">
        /// The initialisation vector for the cipher mode. Must match the block size. Must not be <see langword="null" /> for any mode other
        /// than ECB.
        /// </param>
        /// <param name="encrypt"><see langword="true" /> to configure for encryption; <see langword="false" /> for decryption.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="cipher" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="iv" /> is required but <see langword="null" />, or its length does not match the cipher block size.
        /// </exception>
        internal BlowfishTransform(IBlockCipher cipher, CipherBlockMode cipherMode, PaddingMode paddingMode, byte[] iv, bool encrypt)
        {
            this.cipher = cipher ?? throw new ArgumentNullException(nameof(cipher));
            this.encrypt = encrypt;
            this.mode = BlockCipherModeFactory.Create(cipherMode, cipher, iv);
            this.padding = PaddingFactory.Create(paddingMode);
        }

        /// <inheritdoc />
        /// <remarks>
        /// <para>
        /// Returns <see langword="false" />. Each <see cref="BlowfishTransform" /> instance is intended for a single-use transform
        /// lifetime. Create a new instance for each independent encryption or decryption operation.
        /// </para>
        /// </remarks>
        public bool CanReuseTransform => false;

        /// <inheritdoc />
        public bool CanTransformMultipleBlocks => true;

        /// <inheritdoc />
        /// <value>The Blowfish block size in bytes (8).</value>
        public int InputBlockSize => this.cipher.BlockSize;

        /// <inheritdoc />
        /// <value>The Blowfish block size in bytes (8).</value>
        public int OutputBlockSize => this.cipher.BlockSize;

        /// <summary>
        /// Releases all resources used by the current instance of the <see cref="BlowfishTransform" /> class, including the underlying
        /// cipher engine.
        /// </summary>
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
        /// <param name="outputBuffer">The buffer to write transformed data into. Must not be <see langword="null" />.</param>
        /// <param name="outputOffset">The byte offset within <paramref name="outputBuffer" /> at which to begin writing.</param>
        /// <returns>The number of bytes written to <paramref name="outputBuffer" />.</returns>
        /// <exception cref="ArgumentException">
        /// The input or output buffer span is invalid or insufficient in length for the requested operation.
        /// </exception>
        /// <remarks>
        /// <para>
        /// When decrypting with a strippable padding mode (e.g., <see cref="PaddingMode.PKCS7" />), the last complete block of input is
        /// deferred and not written to the output until <see cref="TransformFinalBlock" /> is called. This allows correct padding removal
        /// at the boundary of the stream.
        /// </para>
        /// </remarks>
        public int TransformBlock(
            byte[] inputBuffer,
            int inputOffset,
            int inputCount,
            byte[] outputBuffer,
            int outputOffset)
        {
            ObjectDisposedException.ThrowIf(this.disposed, this);

            // Fix: enforce ICryptoTransform null-argument contract (previously threw NullReferenceException via .AsSpan).
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
                // When decrypting, defer the last block to enable padding removal at finalization.
                bool stripPadding = this.padding is Pkcs7Padding;

                if (stripPadding && input.Length <= this.cipher.BlockSize)
                {
                    // Buffer this block; it may carry the padding.
                    this.deferredInput = input.ToArray();
                    return 0;
                }

                int bytesToProcess = input.Length;
                if (stripPadding)
                {
                    // Retain the final block for deferred inspection.
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
        /// <exception cref="CryptographicException">
        /// The padding is invalid or cannot be removed during decryption.
        /// </exception>
        /// <remarks>
        /// <para>
        /// When encrypting, padding is applied to the final block before transformation. When decrypting, any previously deferred block is
        /// prepended to <paramref name="inputBuffer" />, the combined data is decrypted, and the padding is then stripped.
        /// </para>
        /// </remarks>
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