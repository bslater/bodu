// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SimpleReversingCryptoTransform.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Bodu.Infrastructure
{
    /// <summary>
    /// A test-only <see cref="ICryptoTransform" /> that coordinates a caller-supplied <see cref="IBlockCipher" /> engine,
    /// <see cref="IBlockCipherModeTransform" /> chaining mode, and <see cref="IPaddingStrategy" /> to exercise the same
    /// block-cipher / mode / padding composition pipeline used by production algorithms such as <see cref="Skipjack" /> and
    /// <see cref="Threefish" />, without depending on a real cipher engine.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class deliberately does not call <see cref="BlockCipherModeFactory" /> or <see cref="PaddingFactory" /> itself.
    /// Callers (typically the <see cref="SimpleReversingSymmetricAlgorithm" /> and
    /// <see cref="SimpleReversingTweakableSymmetricAlgorithm" /> harnesses) construct the pipeline components explicitly and
    /// hand them in, so factory usage remains visible at the algorithm layer and the transform has a single responsibility:
    /// coordinate streaming and finalisation across the mode and the padding strategy.
    /// </para>
    /// </remarks>
    public sealed class SimpleReversingCryptoTransform : ICryptoTransform, IAsyncDisposable
    {
        private readonly IBlockCipher cipher;
        private readonly IBlockCipherModeTransform mode;
        private readonly IPaddingStrategy padding;
        private readonly TransformMode transformMode;
        private byte[]? deferredInput;
        private bool disposed;

        /// <summary>
        /// Initialises a new instance of the <see cref="SimpleReversingCryptoTransform" /> class from pre-built pipeline
        /// components.
        /// </summary>
        /// <param name="cipher">
        /// The block cipher engine to drive. Typically a <see cref="SimpleReversingBlockCipher" />, but any
        /// <see cref="IBlockCipher" /> is accepted.
        /// </param>
        /// <param name="mode">The block chaining mode, usually obtained from <see cref="BlockCipherModeFactory.Create" />.</param>
        /// <param name="padding">The padding strategy, usually obtained from <see cref="PaddingFactory.Create" />.</param>
        /// <param name="transformMode">The direction (<see cref="TransformMode.Encrypt" /> or <see cref="TransformMode.Decrypt" />).</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="cipher" />, <paramref name="mode" />, or <paramref name="padding" /> is <see langword="null" />.
        /// </exception>
        public SimpleReversingCryptoTransform(
            IBlockCipher cipher,
            IBlockCipherModeTransform mode,
            IPaddingStrategy padding,
            TransformMode transformMode)
        {
            this.cipher = cipher ?? throw new ArgumentNullException(nameof(cipher));
            this.mode = mode ?? throw new ArgumentNullException(nameof(mode));
            this.padding = padding ?? throw new ArgumentNullException(nameof(padding));
            this.transformMode = transformMode;
        }

        /// <summary>
        /// Occurs when the transform is disposed.
        /// </summary>
        public event EventHandler? Disposed;

        /// <inheritdoc />
        public bool CanReuseTransform => true;

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
            this.Disposed?.Invoke(this, EventArgs.Empty);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        public ValueTask DisposeAsync()
        {
            this.Dispose();
            return ValueTask.CompletedTask;
        }

        /// <inheritdoc />
        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            this.ThrowIfDisposed();
            ValidateBuffer(inputBuffer, inputOffset, inputCount);
            ValidateBuffer(outputBuffer, outputOffset, inputCount);

            // Short-circuit empty input. The underlying mode transforms (ECB, CFB, OFB, CTR) reject a
            // zero-length span via ThrowIfSpanLengthNotPositiveMultipleOf, but the ICryptoTransform
            // contract callers expect "no-op, return 0" for empty input.
            if (inputCount == 0)
                return 0;

            ReadOnlySpan<byte> input = inputBuffer.AsSpan(inputOffset, inputCount);
            Span<byte> output = outputBuffer.AsSpan(outputOffset, inputCount);

            bool encrypt = this.transformMode == TransformMode.Encrypt;

            if (encrypt)
            {
                // Encryption: stream every incoming block through the mode transform directly.
                return this.mode.Transform(input, output, true);
            }

            // Decryption: defer the last complete block until TransformFinalBlock so padding can be
            // validated and stripped once the end of the stream is known.
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

        /// <inheritdoc />
        [return: NotNullIfNotNull(nameof(inputBuffer))]
        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            this.ThrowIfDisposed();
            ValidateBuffer(inputBuffer, inputOffset, inputCount);

            ReadOnlySpan<byte> input = inputBuffer.AsSpan(inputOffset, inputCount);
            int blockSize = this.cipher.BlockSize;

            if (this.transformMode == TransformMode.Encrypt)
            {
                byte[] padded = this.padding.Pad(input, blockSize);
                if (padded.Length == 0)
                    return Array.Empty<byte>();

                byte[] output = new byte[padded.Length];
                this.mode.Transform(padded, output, true);
                return output;
            }

            byte[] combined = Combine(this.deferredInput, input);
            if (combined.Length == 0)
                return Array.Empty<byte>();

            byte[] decrypted = new byte[combined.Length];
            this.mode.Transform(combined, decrypted, false);
            return this.padding.Unpad(decrypted, blockSize);
        }

        /// <summary>
        /// Concatenates a deferred buffer (if any) with a fresh input span, returning a contiguous array.
        /// </summary>
        private static byte[] Combine(byte[]? first, ReadOnlySpan<byte> second)
        {
            if (first is null || first.Length == 0)
                return second.ToArray();

            byte[] result = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, result, 0, first.Length);
            second.CopyTo(result.AsSpan(first.Length));
            return result;
        }

        private static void ValidateBuffer(byte[] buffer, int offset, int count)
        {
            ArgumentNullException.ThrowIfNull(buffer);
            ArgumentOutOfRangeException.ThrowIfNegative(offset);
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            if (offset + count > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(offset), "Invalid buffer range.");
        }

        private void ThrowIfDisposed()
        {
            if (this.disposed)
                throw new ObjectDisposedException(nameof(SimpleReversingCryptoTransform));
        }
    }
}
