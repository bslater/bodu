// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Salsa20Core.Keystream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

internal static partial class Salsa20Core
{
    /// <summary>
    /// Provides a Salsa20 keystream held entirely in a value, so that drawing on it allocates nothing: the XSalsa20
    /// AEADs seal and open each message with one on the stack.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The keystream is the one <see cref="Salsa20StreamCipher" /> produces under the same key, nonce and initial
    /// counter, through the same core function and kernels, with the 64-bit counter carrying into its high word. Unlike
    /// the engine it does not guard the counter against wrapping, which no message held in a span comes near.
    /// </para>
    /// <para>
    /// The value holds key material. Callers keep it in one place, pass it by reference so that no copy is left behind,
    /// and <see cref="Clear" /> it when the message is done.
    /// </para>
    /// </remarks>
    internal struct Keystream
        : IKeystreamSource
    {
        /// <summary>The sixteen-word state; its counter words are not used.</summary>
        private ChaCha20Core.StateBuffer _state;

        /// <summary>The block counter of the next block.</summary>
        private ulong _counter;

        /// <inheritdoc />
        public readonly ChaCha20Core.KernelKind Kernel =>
            ChaCha20Core.SelectKernel();

        /// <summary>
        /// Seeds the keystream from a key and a nonce, positioned at the specified block counter.
        /// </summary>
        /// <param name="key">The 16- or 32-byte key.</param>
        /// <param name="nonce">The 8-byte nonce.</param>
        /// <param name="counter">The block counter of the first block.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="key" /> is neither 16 nor 32 bytes long, or <paramref name="nonce" /> is not 8 bytes long.
        /// </exception>
        internal void Initialize(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, ulong counter)
        {
            Salsa20Core.Initialize(_state, key, nonce);
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
            Salsa20Core.XorBlocks(_state, _counter, input, output);
            _counter += (ulong)(input.Length / BlockBytes);
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
