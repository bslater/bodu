// ---------------------------------------------------------------------------------------------------------------
// <copyright file="KeccakSponge4.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides four SHAKE sponges advanced side by side through the four-way <c>Keccak-f[1600]</c> permutation, for the
/// independent XOF streams from which ML-KEM and ML-DSA expand their matrices, secrets, masks and noise.
/// </summary>
/// <remarks>
/// <para>
/// Each sponge absorbs one message shorter than the rate, which covers every stream the lattice schemes expand: each is
/// keyed by a seed of at most 64 bytes and an index or nonce of at most two bytes. The sponges then squeeze whole
/// blocks. Word <c>i</c> of sponge <c>j</c> is element <c>j</c> of vector <c>i</c>, so one call of
/// <see cref="KeccakPermutation.Permute4(KeccakPermutation.KernelKind, Span{Vector256{ulong}})" /> advances all four.
/// </para>
/// <para>
/// Each sponge produces exactly the output a <see cref="KeccakSponge" /> of the same function would for its message.
/// Byte order within each word is little-endian, as FIPS 202 specifies, whatever the host's.
/// </para>
/// </remarks>
internal struct KeccakSponge4
{
    /// <summary>The number of sponges advanced together.</summary>
    internal const int Ways = 4;

    /// <summary>The number of 64-bit words each vector of the interleaved state carries: one per sponge.</summary>
    private const int WordsPerVector = 4;

    /// <summary>Domain-separation suffix for the SHAKE extendable-output functions.</summary>
    private const byte ShakeDomainSuffix = 0x1F;

    /// <summary>The sponge rate, in bytes.</summary>
    private readonly int _rateBytes;

    /// <summary>The kernel that permutes the four states.</summary>
    private readonly KeccakPermutation.KernelKind _kernel;

    /// <summary>The four states, interleaved one word per vector element.</summary>
    private StateBuffer _state;

    /// <summary>Indicates whether the states hold output not yet squeezed: set by the permutation that ends absorbing, so the first block needs no permutation of its own.</summary>
    private bool _blockReady;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeccakSponge4" /> struct with the given rate and kernel.
    /// </summary>
    /// <param name="rateBytes">The sponge rate in bytes.</param>
    /// <param name="kernel">The kernel that permutes the four states.</param>
    private KeccakSponge4(int rateBytes, KeccakPermutation.KernelKind kernel)
    {
        _state = default;
        _rateBytes = rateBytes;
        _kernel = kernel;
        _blockReady = false;
    }

    /// <summary>
    /// Gets the rate of each sponge, in bytes: the size of the blocks <see cref="Squeeze" /> produces.
    /// </summary>
    internal readonly int RateBytes => _rateBytes;

    /// <summary>
    /// Creates four SHAKE128 sponges (rate 168 bytes).
    /// </summary>
    /// <param name="kernel">
    /// The kernel that permutes the states; <see cref="KeccakPermutation.KernelKind.Auto" /> for the one dispatch
    /// selects.
    /// </param>
    /// <returns>Four fresh sponges ready to absorb.</returns>
    internal static KeccakSponge4 CreateShake128(KeccakPermutation.KernelKind kernel = KeccakPermutation.KernelKind.Auto) =>
        new(KeccakSponge.Shake128RateBytes, Resolve(kernel));

    /// <summary>
    /// Creates four SHAKE256 sponges (rate 136 bytes).
    /// </summary>
    /// <param name="kernel">
    /// The kernel that permutes the states; <see cref="KeccakPermutation.KernelKind.Auto" /> for the one dispatch
    /// selects.
    /// </param>
    /// <returns>Four fresh sponges ready to absorb.</returns>
    internal static KeccakSponge4 CreateShake256(KeccakPermutation.KernelKind kernel = KeccakPermutation.KernelKind.Auto) =>
        new(KeccakSponge.Shake256RateBytes, Resolve(kernel));

    /// <summary>
    /// Absorbs one message into each sponge and pads it, ending the absorb phase: <paramref name="message0" /> into the
    /// first sponge, and so on.
    /// </summary>
    /// <param name="message0">The first sponge's message.</param>
    /// <param name="message1">The second sponge's message.</param>
    /// <param name="message2">The third sponge's message.</param>
    /// <param name="message3">The fourth sponge's message.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="message0" /> is not shorter than <see cref="RateBytes" />.
    /// </exception>
    /// <exception cref="ArgumentException">The messages differ in length.</exception>
    /// <remarks>
    /// Call it once, on fresh sponges. The four messages then fill one block each, their padding included, and one
    /// permutation absorbs them together.
    /// </remarks>
    internal void Absorb(ReadOnlySpan<byte> message0, ReadOnlySpan<byte> message1, ReadOnlySpan<byte> message2, ReadOnlySpan<byte> message3)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(message0.Length, _rateBytes, nameof(message0));
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(message1, message0.Length);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(message2, message0.Length);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(message3, message0.Length);

        // One padded block per sponge (pad10*1: the domain suffix after the message, 0x80 in the last rate byte),
        // held in whole words so that each block fills the rate's words of its sponge.
        Span<byte> blocks = stackalloc byte[Ways * KeccakSponge.Shake128RateBytes];
        blocks = blocks[..(Ways * _rateBytes)];
        blocks.Clear();

        FillBlock(blocks[.._rateBytes], message0);
        FillBlock(blocks.Slice(_rateBytes, _rateBytes), message1);
        FillBlock(blocks.Slice(2 * _rateBytes, _rateBytes), message2);
        FillBlock(blocks.Slice(3 * _rateBytes, _rateBytes), message3);

        Span<Vector256<ulong>> state = _state;
        int words = _rateBytes / sizeof(ulong);
        for (int word = 0; word < words; word++)
        {
            state[word] ^= Vector256.Create(
                BinaryPrimitives.ReadUInt64LittleEndian(blocks.Slice(word * sizeof(ulong), sizeof(ulong))),
                BinaryPrimitives.ReadUInt64LittleEndian(blocks.Slice(_rateBytes + (word * sizeof(ulong)), sizeof(ulong))),
                BinaryPrimitives.ReadUInt64LittleEndian(blocks.Slice((2 * _rateBytes) + (word * sizeof(ulong)), sizeof(ulong))),
                BinaryPrimitives.ReadUInt64LittleEndian(blocks.Slice((3 * _rateBytes) + (word * sizeof(ulong)), sizeof(ulong))));
        }

        KeccakPermutation.Permute4(_kernel, state);
        _blockReady = true;
        CryptographyHelper.Clear(blocks);
    }

    /// <summary>
    /// Squeezes each sponge's next blocks of output: <paramref name="destination0" /> receives the first sponge's, and
    /// so on.
    /// </summary>
    /// <param name="destination0">The span receiving the first sponge's output.</param>
    /// <param name="destination1">The span receiving the second sponge's output.</param>
    /// <param name="destination2">The span receiving the third sponge's output.</param>
    /// <param name="destination3">The span receiving the fourth sponge's output.</param>
    /// <exception cref="ArgumentException">The destinations differ in length.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The length of <paramref name="destination0" /> is not a positive multiple of <see cref="RateBytes" />.
    /// </exception>
    /// <remarks>
    /// Squeezing continues each sponge's stream from where the previous call left it, so the output of several calls is
    /// the stream a <see cref="KeccakSponge" /> would squeeze in one.
    /// </remarks>
    internal void Squeeze(Span<byte> destination0, Span<byte> destination1, Span<byte> destination2, Span<byte> destination3)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(destination1, destination0.Length);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(destination2, destination0.Length);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(destination3, destination0.Length);
        ThrowHelper.ThrowIfNotPositiveMultipleOf(destination0.Length, _rateBytes, nameof(destination0));

        Span<Vector256<ulong>> state = _state;
        for (int offset = 0; offset < destination0.Length; offset += _rateBytes)
        {
            // The permutation that ended absorbing left the first block ready; each later block costs one of its own,
            // taken only when the block is wanted, so no permutation is spent past the last.
            if (!_blockReady)
                KeccakPermutation.Permute4(_kernel, state);

            _blockReady = false;
            WriteBlock(
                state,
                destination0.Slice(offset, _rateBytes),
                destination1.Slice(offset, _rateBytes),
                destination2.Slice(offset, _rateBytes),
                destination3.Slice(offset, _rateBytes));
        }
    }

    /// <summary>
    /// Zeroes the four states. Call when the absorbed material is secret and the sponges are no longer needed.
    /// </summary>
    internal void Clear()
    {
        Span<Vector256<ulong>> state = _state;
        CryptographyHelper.Clear(state);
        _blockReady = false;
    }

    /// <summary>
    /// Resolves <see cref="KeccakPermutation.KernelKind.Auto" /> to the kernel dispatch selects.
    /// </summary>
    /// <param name="kernel">The requested kernel.</param>
    /// <returns>The kernel the sponges use; never <see cref="KeccakPermutation.KernelKind.Auto" />.</returns>
    private static KeccakPermutation.KernelKind Resolve(KeccakPermutation.KernelKind kernel) =>
        kernel == KeccakPermutation.KernelKind.Auto ? KeccakPermutation.SelectKernel() : kernel;

    /// <summary>
    /// Copies a message into a zeroed block and appends the SHAKE padding.
    /// </summary>
    /// <param name="block">The rate-sized block, zeroed.</param>
    /// <param name="message">The message, shorter than the block.</param>
    private static void FillBlock(Span<byte> block, ReadOnlySpan<byte> message)
    {
        message.CopyTo(block);
        block[message.Length] ^= ShakeDomainSuffix;
        block[^1] ^= 0x80;
    }

    /// <summary>
    /// Writes the rate words of each state, little-endian, to its destination block.
    /// </summary>
    /// <param name="state">The interleaved states.</param>
    /// <param name="destination0">The first sponge's block.</param>
    /// <param name="destination1">The second sponge's block.</param>
    /// <param name="destination2">The third sponge's block.</param>
    /// <param name="destination3">The fourth sponge's block.</param>
    /// <remarks>
    /// On a little-endian AVX2 host each group of four words is transposed in registers, so every sponge receives its
    /// four words with one store; otherwise, and for the word after the last whole group, the words are written one at
    /// a time.
    /// </remarks>
    private readonly void WriteBlock(
        ReadOnlySpan<Vector256<ulong>> state,
        Span<byte> destination0,
        Span<byte> destination1,
        Span<byte> destination2,
        Span<byte> destination3)
    {
        int words = _rateBytes / sizeof(ulong);
        int word = 0;

        if (BitConverter.IsLittleEndian && SimdCapabilities.Avx2)
        {
            ref byte d0 = ref MemoryMarshal.GetReference(destination0);
            ref byte d1 = ref MemoryMarshal.GetReference(destination1);
            ref byte d2 = ref MemoryMarshal.GetReference(destination2);
            ref byte d3 = ref MemoryMarshal.GetReference(destination3);

            for (; word + WordsPerVector <= words; word += WordsPerVector)
            {
                // Element j of vector i is word i of sponge j; transposing four vectors gathers four words per sponge.
                Vector256<ulong> t0 = Avx2.UnpackLow(state[word], state[word + 1]);
                Vector256<ulong> t1 = Avx2.UnpackHigh(state[word], state[word + 1]);
                Vector256<ulong> t2 = Avx2.UnpackLow(state[word + 2], state[word + 3]);
                Vector256<ulong> t3 = Avx2.UnpackHigh(state[word + 2], state[word + 3]);

                nuint offset = (nuint)(word * sizeof(ulong));
                Avx2.Permute2x128(t0, t2, 0x20).AsByte().StoreUnsafe(ref d0, offset);
                Avx2.Permute2x128(t1, t3, 0x20).AsByte().StoreUnsafe(ref d1, offset);
                Avx2.Permute2x128(t0, t2, 0x31).AsByte().StoreUnsafe(ref d2, offset);
                Avx2.Permute2x128(t1, t3, 0x31).AsByte().StoreUnsafe(ref d3, offset);
            }
        }

        for (; word < words; word++)
        {
            int offset = word * sizeof(ulong);
            Vector256<ulong> lanes = state[word];
            BinaryPrimitives.WriteUInt64LittleEndian(destination0.Slice(offset, sizeof(ulong)), lanes.GetElement(0));
            BinaryPrimitives.WriteUInt64LittleEndian(destination1.Slice(offset, sizeof(ulong)), lanes.GetElement(1));
            BinaryPrimitives.WriteUInt64LittleEndian(destination2.Slice(offset, sizeof(ulong)), lanes.GetElement(2));
            BinaryPrimitives.WriteUInt64LittleEndian(destination3.Slice(offset, sizeof(ulong)), lanes.GetElement(3));
        }
    }

    /// <summary>
    /// Inline storage for the four interleaved 25-word states, kept stack-allocatable so the sponges never touch the
    /// heap.
    /// </summary>
    [InlineArray(KeccakPermutation.StateWords)]
    private struct StateBuffer
    {
        /// <summary>The first vector of the inline buffer; the <see cref="InlineArrayAttribute" /> expands it to 25.</summary>
        private Vector256<ulong> _word0;
    }
}
