using Bodu.Security.Cryptography;
using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Bodu.Infrastructure
{
    /// <summary>
    /// A simple <see cref="ICryptoTransform" /> implementation that reverses each input block and optionally applies tweak logic.
    /// </summary>
    public sealed class SimpleReversingCryptoTransform : ICryptoTransform, IAsyncDisposable
    {
        private readonly int blockSizeBytes;
        private readonly byte[] depadBuffer;
        private readonly PaddingMode paddingMode;
        private readonly TransformMode transformMode;
        private readonly byte[]? tweak;
        private bool disposed;
        private bool hasDepadBlock;

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleReversingCryptoTransform" /> class.
        /// </summary>
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
            blockSizeBytes = blockSizeBits / 8;
            this.paddingMode = paddingMode;
            this.transformMode = transformMode;
            this.tweak = tweak;
            depadBuffer = new byte[blockSizeBytes];
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
        public int InputBlockSize => blockSizeBytes;

        /// <inheritdoc />
        public int OutputBlockSize => blockSizeBytes;

        /// <inheritdoc />
        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                Disposed?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <inheritdoc />
        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }

        /// <inheritdoc />
        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            this.ThrowIfDisposed();
            ValidateBuffer(inputBuffer, inputOffset, inputCount);
            ValidateBuffer(outputBuffer, outputOffset, inputCount);

            var input = inputBuffer.AsSpan(inputOffset, inputCount);
            var output = outputBuffer.AsSpan(outputOffset, inputCount);

            // Route based on transform mode
            return transformMode == TransformMode.Encrypt
                ? TransformBlocks(input, output)
                : DecryptBlocks(input, output);
        }

        /// <inheritdoc />
        [return: NotNullIfNotNull(nameof(inputBuffer))]

        /// <inheritdoc />
        [return: NotNullIfNotNull(nameof(inputBuffer))]
        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            this.ThrowIfDisposed();
            ValidateBuffer(inputBuffer, inputOffset, inputCount);
            var input = inputBuffer.AsSpan(inputOffset, inputCount);

            if (transformMode == TransformMode.Encrypt)
            {
                // For encryption, apply padding first, then transform the padded blocks
                if (paddingMode == PaddingMode.None && input.Length % blockSizeBytes != 0)
                    throw new CryptographicException("Input is not a multiple of block size.");

                int paddedLength = ((input.Length / blockSizeBytes) + 1) * blockSizeBytes;
                byte[] pooled = ArrayPool<byte>.Shared.Rent(paddedLength);
                Span<byte> padded = pooled.AsSpan(0, paddedLength);

                int finalLength = CryptoHelpers.PadBlock(paddingMode, blockSizeBytes, input, padded);
                TransformBlocks(padded.Slice(0, finalLength), padded);

                byte[] encrypted = padded.Slice(0, finalLength).ToArray();
                ArrayPool<byte>.Shared.Return(pooled, clearArray: true);
                return encrypted;
            }

            // --- Decryption path --- Special case: if input is empty and there's no depad buffer, treat as empty output
            if (input.Length == 0 && !hasDepadBlock)
            {
                // For padding modes that require block alignment, this would be invalid input
                if (paddingMode != PaddingMode.None && paddingMode != PaddingMode.Zeros)
                    throw new CryptographicException("Cannot decrypt empty input with padding.");
                return Array.Empty<byte>();
            }

            // Calculate expected buffer length (accounting for possible stored depad block)
            int totalLength = hasDepadBlock ? input.Length + blockSizeBytes : input.Length;
            byte[] decryptBuffer = ArrayPool<byte>.Shared.Rent(totalLength);
            Span<byte> buffer = decryptBuffer.AsSpan(0, totalLength);

            // Copy depad block (if present), then append current input
            if (hasDepadBlock)
            {
                depadBuffer.AsSpan().CopyTo(buffer);
                input.CopyTo(buffer.Slice(blockSizeBytes));
            }
            else
            {
                input.CopyTo(buffer);
            }

            // Transform all blocks in-place
            TransformBlocks(buffer, buffer);

            // Prepare destination buffer for depadded result
            byte[] output = ArrayPool<byte>.Shared.Rent(buffer.Length);
            Span<byte> destination = output.AsSpan(0, buffer.Length);

            int resultLength = CryptoHelpers.DepadBlock(paddingMode, blockSizeBytes, buffer, destination);
            byte[] decrypted = destination.Slice(0, resultLength).ToArray();

            ArrayPool<byte>.Shared.Return(decryptBuffer, clearArray: true);
            ArrayPool<byte>.Shared.Return(output, clearArray: true);
            return decrypted;
        }

        /// <summary>
        /// Validates the buffer and range inputs for safety.
        /// </summary>
        private static void ValidateBuffer(byte[] buffer, int offset, int count)
        {
            ArgumentNullException.ThrowIfNull(buffer);
            ArgumentOutOfRangeException.ThrowIfNegative(offset);
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            if (offset + count > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(offset), "Invalid buffer range.");
        }

        /// <summary>
        /// Applies depadding behavior during decryption and then processes remaining full blocks.
        /// </summary>
        private int DecryptBlocks(ReadOnlySpan<byte> input, Span<byte> output)
        {
            int total = 0;
            int decryptableLength = input.Length;

            if (paddingMode != PaddingMode.None && paddingMode != PaddingMode.Zeros)
            {
                if (hasDepadBlock)
                {
                    TransformBlockInternal(depadBuffer, output.Slice(0, blockSizeBytes));
                    output = output.Slice(blockSizeBytes);
                    total += blockSizeBytes;
                }
                input.Slice(input.Length - blockSizeBytes).CopyTo(depadBuffer);
                decryptableLength -= blockSizeBytes;
                hasDepadBlock = true;
            }

            if (decryptableLength > 0)
            {
                total += TransformBlocks(input.Slice(0, decryptableLength), output);
            }
            return total;
        }

        /// <summary>
        /// Throws an exception if this instance has already been disposed.
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(SimpleReversingCryptoTransform));
        }

        /// <summary>
        /// Performs reversal and optional tweak on a single block.
        /// </summary>
        private void TransformBlockInternal(ReadOnlySpan<byte> input, Span<byte> output)
        {
            Span<byte> temp = stackalloc byte[blockSizeBytes];
            input.CopyTo(temp);

            if (tweak != null)
            {
                for (int i = 0; i < blockSizeBytes; i++)
                {
                    int index = transformMode == TransformMode.Encrypt ? i : blockSizeBytes - 1 - i;
                    temp[i] = transformMode == TransformMode.Encrypt
                        ? (byte)((temp[i] + tweak[index]) % byte.MaxValue)
                        : (byte)((byte.MaxValue + temp[i] - tweak[index]) % byte.MaxValue);
                }
            }

            temp.Reverse();
            temp.CopyTo(output);
        }

        /// <summary>
        /// Transforms a span of data in fixed-size blocks using reversal and optional tweak logic.
        /// </summary>
        private int TransformBlocks(ReadOnlySpan<byte> input, Span<byte> output)
        {
            int total = 0;
            for (; total + blockSizeBytes <= input.Length; total += blockSizeBytes)
            {
                TransformBlockInternal(input.Slice(total, blockSizeBytes), output.Slice(total, blockSizeBytes));
            }
            return total;
        }
    }
}