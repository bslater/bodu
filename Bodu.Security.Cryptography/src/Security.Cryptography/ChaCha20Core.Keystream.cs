// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ChaCha20Core.Keystream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

internal static partial class ChaCha20Core
{
    /// <summary>
    /// Provides a ChaCha20 keystream held entirely in a value, so that drawing on it allocates nothing: the Poly1305
    /// AEADs seal and open each message with one on the stack.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The keystream is the one <see cref="ChaCha20StreamCipher" /> produces under the same key, nonce and initial
    /// counter, through the same block function and kernels. Unlike the engine it does not guard its counter against
    /// wrapping: a caller draws fewer than 2^32 blocks from it, as any message held in a span does.
    /// </para>
    /// <para>
    /// The value holds key material. Callers keep it in one place, pass it by reference so that no copy is left behind,
    /// and <see cref="Clear" /> it when the message is done.
    /// </para>
    /// </remarks>
    internal struct Keystream
        : IKeystreamSource
    {
        /// <summary>The sixteen-word state; its counter word is not used.</summary>
        private StateBuffer _state;

        /// <summary>The block counter of the next block.</summary>
        private uint _counter;

        /// <inheritdoc />
        public readonly KernelKind Kernel =>
            SelectKernel();

        /// <summary>
        /// Seeds the keystream from a key and a nonce, positioned at the specified block counter.
        /// </summary>
        /// <param name="key">The 32-byte key.</param>
        /// <param name="nonce">The 12-byte nonce.</param>
        /// <param name="counter">The block counter of the first block.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="key" /> is not 32 bytes long, or <paramref name="nonce" /> is not 12 bytes long.
        /// </exception>
        internal void Initialize(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, uint counter)
        {
            ChaCha20Core.Initialize(_state, key, nonce);
            _counter = counter;
        }

        /// <inheritdoc />
        public void NextBlock(Span<byte> destination)
        {
            Block(_state, _counter, destination);
            _counter++;
        }

        /// <inheritdoc />
        public void XorBlocks(ReadOnlySpan<byte> input, Span<byte> output)
        {
            ChaCha20Core.XorBlocks(_state, _counter, input, output);
            _counter += (uint)(input.Length / BlockBytes);
        }

        /// <summary>
        /// Zeroes the state and the counter, leaving the keystream of an all-zero state at counter zero.
        /// </summary>
        internal void Clear()
        {
            CryptographicOperations.ZeroMemory(MemoryMarshal.AsBytes((Span<uint>)_state));
            _counter = 0;
        }
    }
}
