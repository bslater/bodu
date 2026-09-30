// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2Blake2b.Hasher.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Diagnostics;

namespace Bodu.Security.Cryptography;

internal static partial class Argon2Blake2b
{
    /// <summary>
    /// Computes an unkeyed BLAKE2b digest incrementally over caller-supplied state, so several inputs can be hashed as
    /// one message without first being copied into a single buffer.
    /// </summary>
    /// <remarks>
    /// The caller supplies the chaining state and the block buffer - typically on its own stack - so the hasher
    /// allocates nothing. <see cref="Finish" /> clears both, and a hasher must not be used after it.
    /// </remarks>
    internal ref struct Hasher
    {
        /// <summary>The eight-word chaining state, supplied by the caller.</summary>
        private readonly Span<ulong> _state;

        /// <summary>The 128-byte buffer that collects input until a full block is known not to be the last.</summary>
        private readonly Span<byte> _block;

        /// <summary>The digest length, in bytes, folded into the parameter block.</summary>
        private readonly int _digestLength;

        /// <summary>The number of input bytes compressed so far.</summary>
        private ulong _counter;

        /// <summary>The number of bytes buffered in <see cref="_block" />.</summary>
        private int _filled;

        /// <summary>
        /// Initializes a new instance of the <see cref="Hasher" /> struct over the supplied state and block buffer.
        /// </summary>
        /// <param name="state">The chaining state; exactly <see cref="StateWords" /> words.</param>
        /// <param name="block">The block buffer; exactly <see cref="BlockSizeBytes" /> bytes.</param>
        /// <param name="digestLength">The digest length, in bytes: 1 to 64.</param>
        internal Hasher(Span<ulong> state, Span<byte> block, int digestLength)
        {
            Debug.Assert(state.Length == StateWords, "The chaining state must be eight words.");
            Debug.Assert(block.Length == BlockSizeBytes, "The block buffer must be 128 bytes.");
            Debug.Assert(digestLength is >= 1 and <= MaxDigestBytes, "The digest length must be 1 to 64 bytes.");

            Blake2bCore.InitializationVector.CopyTo(state);

            // Parameter block: digest length, key length (0), fanout (1), depth (1) packed into the first word.
            state[0] ^= 0x0101_0000UL ^ (ulong)(uint)digestLength;

            _state = state;
            _block = block;
            _digestLength = digestLength;
            _counter = 0;
            _filled = 0;
        }

        /// <summary>
        /// Appends bytes to the message.
        /// </summary>
        /// <param name="data">The bytes to append.</param>
        internal void Append(scoped ReadOnlySpan<byte> data)
        {
            while (!data.IsEmpty)
            {
                // A full buffer is compressed only once more input arrives, because BLAKE2b flags the final block and
                // a full buffer may yet turn out to be it.
                if (_filled == BlockSizeBytes)
                {
                    _counter += BlockSizeBytes;
                    Blake2bCore.Compress(_state, _block, _counter, last: false);
                    _filled = 0;
                }

                int take = Math.Min(BlockSizeBytes - _filled, data.Length);
                data[..take].CopyTo(_block[_filled..]);
                _filled += take;
                data = data[take..];
            }
        }

        /// <summary>
        /// Appends a 32-bit value to the message in little-endian order, as Argon2 encodes its lengths and parameters.
        /// </summary>
        /// <param name="value">The value to append.</param>
        internal void AppendLittleEndian(int value)
        {
            Span<byte> encoded = stackalloc byte[sizeof(int)];
            BinaryPrimitives.WriteInt32LittleEndian(encoded, value);
            Append(encoded);
        }

        /// <summary>
        /// Completes the digest, writes it to <paramref name="output" />, and clears the state and the block buffer.
        /// </summary>
        /// <param name="output">
        /// The destination; its length must equal the digest length the hasher was initialized with. It may overlap
        /// input appended earlier, which is buffered or already compressed.
        /// </param>
        internal void Finish(Span<byte> output)
        {
            Debug.Assert(output.Length == _digestLength, "The output must be the digest length.");

            _counter += (ulong)_filled;
            _block[_filled..].Clear();
            Blake2bCore.Compress(_state, _block, _counter, last: true);

            Span<byte> digest = stackalloc byte[MaxDigestBytes];
            for (int i = 0; i < StateWords; i++)
                BinaryPrimitives.WriteUInt64LittleEndian(digest.Slice(i * 8, 8), _state[i]);

            digest[.._digestLength].CopyTo(output);

            CryptographyHelper.Clear(digest);
            CryptographyHelper.Clear(_state);
            CryptographyHelper.Clear(_block);
        }
    }
}
