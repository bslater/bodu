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
    /// A test-only <see cref="ICryptoTransform" /> that runs a <see cref="SimpleReversingBlockCipher" /> primitive through
    /// the library's standard <see cref="BlockCipherModeFactory" /> and <see cref="PaddingFactory" /> pipelines. It exists
    /// so tests can exercise the same block-cipher / mode / padding composition path used by production algorithms such as
    /// <see cref="Skipjack" /> and <see cref="Threefish" /> without depending on a real cipher engine.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The transform owns the configured <see cref="IBlockCipher" />, the selected <see cref="IBlockCipherModeTransform" />,
    /// and the selected <see cref="IPaddingStrategy" />. Streaming blocks flow through <see cref="TransformBlock" />; the
    /// final block is handled by <see cref="TransformFinalBlock" /> which applies (or removes) padding.
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
        /// Initialises a new instance of the <see cref="SimpleReversingCryptoTransform" /> class.
        /// </summary>
        /// <param name="blockSizeBits">The block size in bits. Must be a positive multiple of 8.</param>
        /// <param name="feedbackSizeBits">The feedback size in bits. Accepted for compatibility and currently ignored.</param>
        /// <param name="key">The key. Not used by the reversing primitive but validated so tests can exercise key handling.</param>
        /// <param name="iv">The initialisation vector, passed to <see cref="BlockCipherModeFactory" /> for chaining modes that require one.</param>
        /// <param name="tweak">An optional tweak forwarded to <see cref="SimpleReversingBlockCipher" />.</param>
        /// <param name="cipherMode">The standard <see cref="CipherMode" /> to apply via <see cref="BlockCipherModeFactory" />.</param>
        /// <param name="paddingMode">The padding scheme to apply via <see cref="PaddingFactory" />.</param>
        /// <param name="transformMode">The direction (<see cref="TransformMode.Encrypt" /> or <see cref="TransformMode.Decrypt" />).</param>
        public SimpleReversingCryptoTransform(
            int blockSizeBits,
            int feedbackSizeBits,
            byte[] key,
            byte[] iv,
            byte[]? tweak,
            CipherMode cipherMode,
            PaddingMode paddingMode,
            TransformMode transformMode)
        {
            if (blockSizeBits <= 0 || blockSizeBits % 8 != 0)
                throw new ArgumentOutOfRangeException(nameof(blockSizeBits), "Block size must be a positive multiple of 8.");

            int blockSizeBytes = blockSizeBits / 8;

            this.cipher = new SimpleReversingBlockCipher(blockSizeBytes, tweak);
            this.mode = BlockCipherModeFactory.Create(ConvertMode(cipherMode), this.cipher, iv);
            this.padding = PaddingFactory.Create(paddingMode);
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
        /// Maps a standard <see cref="CipherMode" /> onto the library's <see cref="CipherBlockMode" /> enum so the transform
        /// can plug into <see cref="BlockCipherModeFactory.Create" />.
        /// </summary>
        private static CipherBlockMode ConvertMode(CipherMode mode) => mode switch
        {
            CipherMode.ECB => CipherBlockMode.ECB,
            CipherMode.CBC => CipherBlockMode.CBC,
            CipherMode.CFB => CipherBlockMode.CFB,
            CipherMode.OFB => CipherBlockMode.OFB,
            _ => throw new NotSupportedException($"Cipher mode '{mode}' is not supported by {nameof(SimpleReversingCryptoTransform)}."),
        };

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
