// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SimpleReversingBlockCipher.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using Bodu.Security.Cryptography;

namespace Bodu.Infrastructure
{
    /// <summary>
    /// A toy <see cref="IBlockCipher" /> used by the test harness that implements its block primitive as a simple byte
    /// reversal with an optional tweak. The primitive is deterministic, self-invertible under the same tweak, and has no
    /// cryptographic value — it exists purely so that <see cref="BlockCipherModeFactory" /> and <see cref="PaddingFactory" />
    /// can be exercised without pulling in a real cipher engine.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Encryption computes <c>output = reverse(input + tweak)</c>, where the tweak is added byte-wise modulo
    /// <see cref="byte.MaxValue" /> (matching the historical behaviour of the previous inline implementation in
    /// <see cref="SimpleReversingCryptoTransform" />). Decryption reverses first and then subtracts the same tweak so that
    /// an encrypt-then-decrypt round trip recovers the original plaintext block-for-block.
    /// </para>
    /// <para>
    /// When <paramref name="tweak" /> is <see langword="null" />, the cipher is a pure byte-reversal, which makes it easy to
    /// pin expected outputs in known-answer tests (for example, <c>[1,2,3,4] → [4,3,2,1]</c>).
    /// </para>
    /// </remarks>
    public sealed class SimpleReversingBlockCipher : IBlockCipher
    {
        private readonly int blockSizeBytes;
        private readonly byte[]? tweak;
        private bool disposed;

        /// <summary>
        /// Initialises a new instance of the <see cref="SimpleReversingBlockCipher" /> class with the specified block size
        /// and optional tweak.
        /// </summary>
        /// <param name="blockSizeBytes">The block size in bytes. Must be greater than zero.</param>
        /// <param name="tweak">
        /// An optional tweak value. If <see langword="null" /> or empty, the cipher is a pure byte-reversal. Tweaks whose
        /// length differs from <paramref name="blockSizeBytes" /> are accepted and indexed modulo their own length so tests
        /// that vary <c>TweakSize</c> independently of <c>BlockSize</c> can still construct the primitive.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="blockSizeBytes" /> is less than or equal to zero.</exception>
        public SimpleReversingBlockCipher(int blockSizeBytes, byte[]? tweak = null)
        {
            if (blockSizeBytes <= 0)
                throw new ArgumentOutOfRangeException(nameof(blockSizeBytes), "Block size must be greater than zero.");

            this.blockSizeBytes = blockSizeBytes;
            this.tweak = (tweak is { Length: > 0 }) ? tweak : null;
        }

        /// <inheritdoc />
        public int BlockSize => this.blockSizeBytes;

        /// <inheritdoc />
        public void Encrypt(ReadOnlySpan<byte> input, Span<byte> output)
        {
            this.ThrowIfDisposed();
            this.ValidateLengths(input, output);

            Span<byte> temp = stackalloc byte[this.blockSizeBytes];
            input.CopyTo(temp);

            if (this.tweak is not null)
            {
                int tweakLen = this.tweak.Length;
                for (int i = 0; i < this.blockSizeBytes; i++)
                    temp[i] = (byte)((temp[i] + this.tweak[i % tweakLen]) % byte.MaxValue);
            }

            temp.Reverse();
            temp.CopyTo(output);
        }

        /// <inheritdoc />
        public void Decrypt(ReadOnlySpan<byte> input, Span<byte> output)
        {
            this.ThrowIfDisposed();
            this.ValidateLengths(input, output);

            Span<byte> temp = stackalloc byte[this.blockSizeBytes];
            input.CopyTo(temp);

            // Decrypt inverts encryption: reverse first, then undo the tweak against the original positions.
            temp.Reverse();

            if (this.tweak is not null)
            {
                int tweakLen = this.tweak.Length;
                for (int i = 0; i < this.blockSizeBytes; i++)
                    temp[i] = (byte)((byte.MaxValue + temp[i] - this.tweak[i % tweakLen]) % byte.MaxValue);
            }

            temp.CopyTo(output);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.disposed = true;
        }

        private void ValidateLengths(ReadOnlySpan<byte> input, Span<byte> output)
        {
            if (input.Length != this.blockSizeBytes)
                throw new ArgumentException(
                    $"Input length must equal the block size ({this.blockSizeBytes} bytes); got {input.Length}.",
                    nameof(input));

            if (output.Length != this.blockSizeBytes)
                throw new ArgumentException(
                    $"Output length must equal the block size ({this.blockSizeBytes} bytes); got {output.Length}.",
                    nameof(output));
        }

        private void ThrowIfDisposed()
        {
            if (this.disposed)
                throw new ObjectDisposedException(nameof(SimpleReversingBlockCipher));
        }
    }
}
