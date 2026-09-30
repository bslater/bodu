// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Salsa20Core.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Bodu.Extensions;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Implements the Salsa20 core function (Bernstein, 2005) for <see cref="Salsa20StreamCipher" />: one block at a time,
/// and whole runs of blocks through vector kernels that give each lane a block of its own.
/// </summary>
/// <remarks>
/// <para>
/// The state is sixteen 32-bit words: the four constant words on the diagonal (positions 0, 5, 10 and 15), the key in
/// words 1-4 and 11-14, the nonce in words 6 and 7, and the 64-bit block counter in words 8 (low) and 9 (high). Every
/// entry point takes the counter of its first block separately, whatever the state's counter words hold, and the blocks
/// after it count up modulo 2^64.
/// </para>
/// <para>
/// The many-block kernels are laid out as <see cref="ChaCha20Core" />'s are, and share its kernel kinds, its rotations
/// through Bodu.Core's <see cref="VectorExtensions" /> and its transposition back into block order; the counter's low
/// word counts up across the lanes, carrying into the high word where it wraps. None of Salsa20's rotations is by whole
/// bytes, so they are shift pairs where the processor has no rotate instruction.
/// </para>
/// </remarks>
[SkipLocalsInit]
internal static partial class Salsa20Core
{
    /// <summary>The number of 32-bit words in the Salsa20 state.</summary>
    internal const int StateWords = 16;

    /// <summary>The number of bytes in a keystream block.</summary>
    internal const int BlockBytes = 64;

    /// <summary>The number of bytes in a 128-bit key.</summary>
    internal const int Key128Bytes = 16;

    /// <summary>The number of bytes in a 256-bit key.</summary>
    internal const int Key256Bytes = 32;

    /// <summary>The number of bytes in the Salsa20 nonce.</summary>
    internal const int NonceBytes = 8;

    /// <summary>The index of the low word of the block counter in the state; the high word follows it.</summary>
    internal const int CounterWord = 8;

    /// <summary>The first little-endian word of the ASCII constant <c>"expand 32-byte k"</c>, state word 0 for a 256-bit key.</summary>
    internal const uint Sigma0 = 0x61707865;

    /// <summary>The second little-endian word of the ASCII constant <c>"expand 32-byte k"</c>, state word 5 for a 256-bit key.</summary>
    internal const uint Sigma1 = 0x3320646e;

    /// <summary>The third little-endian word of the ASCII constant <c>"expand 32-byte k"</c>, state word 10 for a 256-bit key.</summary>
    internal const uint Sigma2 = 0x79622d32;

    /// <summary>The fourth little-endian word of the ASCII constant <c>"expand 32-byte k"</c>, state word 15 for a 256-bit key.</summary>
    internal const uint Sigma3 = 0x6b206574;

    /// <summary>The first little-endian word of the ASCII constant <c>"expand 16-byte k"</c>, state word 0 for a 128-bit key.</summary>
    private const uint Tau0 = 0x61707865;

    /// <summary>The second little-endian word of the ASCII constant <c>"expand 16-byte k"</c>, state word 5 for a 128-bit key.</summary>
    private const uint Tau1 = 0x3120646e;

    /// <summary>The third little-endian word of the ASCII constant <c>"expand 16-byte k"</c>, state word 10 for a 128-bit key.</summary>
    private const uint Tau2 = 0x79622d36;

    /// <summary>The fourth little-endian word of the ASCII constant <c>"expand 16-byte k"</c>, state word 15 for a 128-bit key.</summary>
    private const uint Tau3 = 0x6b206574;

    /// <summary>The number of double rounds: a column round and a row round each, twenty rounds in all.</summary>
    private const int DoubleRounds = 10;

    /// <summary>
    /// Seeds a Salsa20 state from a key and a nonce, with a zero block counter.
    /// </summary>
    /// <param name="state">The state to write: at least sixteen words, of which the first sixteen are written.</param>
    /// <param name="key">The 16- or 32-byte key.</param>
    /// <param name="nonce">The 8-byte nonce.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="state" /> holds fewer than sixteen words, <paramref name="key" /> is neither 16 nor 32 bytes
    /// long, or <paramref name="nonce" /> is not 8 bytes long.
    /// </exception>
    /// <remarks>
    /// For a 256-bit key the second key half occupies words 11-14; for a 128-bit key the single 16-byte key is repeated
    /// into both halves, and the <c>"expand 16-byte k"</c> constants are used.
    /// </remarks>
    internal static void Initialize(Span<uint> state, ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(state.Length, StateWords, nameof(state));
        if (key.Length is not Key128Bytes and not Key256Bytes) throw new ArgumentOutOfRangeException(nameof(key));
        ArgumentOutOfRangeException.ThrowIfNotEqual(nonce.Length, NonceBytes, nameof(nonce));

        bool is256 = key.Length == Key256Bytes;

        state[0] = is256 ? Sigma0 : Tau0;
        state[5] = is256 ? Sigma1 : Tau1;
        state[10] = is256 ? Sigma2 : Tau2;
        state[15] = is256 ? Sigma3 : Tau3;

        // First key half -> words 1..4; second half -> words 11..14. A 128-bit key supplies both halves.
        ReadOnlySpan<byte> secondHalf = is256 ? key[16..] : key;
        for (int i = 0; i < 4; i++)
        {
            state[1 + i] = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(i * sizeof(uint)));
            state[11 + i] = BinaryPrimitives.ReadUInt32LittleEndian(secondHalf.Slice(i * sizeof(uint)));
        }

        state[6] = BinaryPrimitives.ReadUInt32LittleEndian(nonce);
        state[7] = BinaryPrimitives.ReadUInt32LittleEndian(nonce[4..]);
        state[CounterWord] = 0;
        state[CounterWord + 1] = 0;
    }

    /// <summary>
    /// Produces one keystream block: the Salsa20 core function over the state, with the specified block counter.
    /// </summary>
    /// <param name="state">The sixteen-word state; its counter words are ignored.</param>
    /// <param name="counter">The 64-bit block counter.</param>
    /// <param name="destination">Receives the 64-byte block in its first 64 bytes.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="state" /> holds fewer than sixteen words, or <paramref name="destination" /> fewer than 64
    /// bytes.
    /// </exception>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1107:Code should not contain multiple statements on one line", Justification = "The sixteen state words are loaded four to a line, as the 4×4 matrix the specification draws.")]
    internal static void Block(ReadOnlySpan<uint> state, ulong counter, Span<byte> destination)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(state.Length, StateWords, nameof(state));
        ArgumentOutOfRangeException.ThrowIfLessThan(destination.Length, BlockBytes, nameof(destination));

        uint j0 = state[0], j1 = state[1], j2 = state[2], j3 = state[3];
        uint j4 = state[4], j5 = state[5], j6 = state[6], j7 = state[7];
        uint j8 = (uint)counter, j9 = (uint)(counter >> 32), j10 = state[10], j11 = state[11];
        uint j12 = state[12], j13 = state[13], j14 = state[14], j15 = state[15];

        uint x0 = j0, x1 = j1, x2 = j2, x3 = j3;
        uint x4 = j4, x5 = j5, x6 = j6, x7 = j7;
        uint x8 = j8, x9 = j9, x10 = j10, x11 = j11;
        uint x12 = j12, x13 = j13, x14 = j14, x15 = j15;

        for (int round = 0; round < DoubleRounds; round++)
        {
            // Column round.
            QuarterRound(ref x0, ref x4, ref x8, ref x12);
            QuarterRound(ref x5, ref x9, ref x13, ref x1);
            QuarterRound(ref x10, ref x14, ref x2, ref x6);
            QuarterRound(ref x15, ref x3, ref x7, ref x11);

            // Row round.
            QuarterRound(ref x0, ref x1, ref x2, ref x3);
            QuarterRound(ref x5, ref x6, ref x7, ref x4);
            QuarterRound(ref x10, ref x11, ref x8, ref x9);
            QuarterRound(ref x15, ref x12, ref x13, ref x14);
        }

        // Add the original state back in, then serialize little-endian.
        BinaryPrimitives.WriteUInt32LittleEndian(destination, x0 + j0);
        BinaryPrimitives.WriteUInt32LittleEndian(destination[4..], x1 + j1);
        BinaryPrimitives.WriteUInt32LittleEndian(destination[8..], x2 + j2);
        BinaryPrimitives.WriteUInt32LittleEndian(destination[12..], x3 + j3);
        BinaryPrimitives.WriteUInt32LittleEndian(destination[16..], x4 + j4);
        BinaryPrimitives.WriteUInt32LittleEndian(destination[20..], x5 + j5);
        BinaryPrimitives.WriteUInt32LittleEndian(destination[24..], x6 + j6);
        BinaryPrimitives.WriteUInt32LittleEndian(destination[28..], x7 + j7);
        BinaryPrimitives.WriteUInt32LittleEndian(destination[32..], x8 + j8);
        BinaryPrimitives.WriteUInt32LittleEndian(destination[36..], x9 + j9);
        BinaryPrimitives.WriteUInt32LittleEndian(destination[40..], x10 + j10);
        BinaryPrimitives.WriteUInt32LittleEndian(destination[44..], x11 + j11);
        BinaryPrimitives.WriteUInt32LittleEndian(destination[48..], x12 + j12);
        BinaryPrimitives.WriteUInt32LittleEndian(destination[52..], x13 + j13);
        BinaryPrimitives.WriteUInt32LittleEndian(destination[56..], x14 + j14);
        BinaryPrimitives.WriteUInt32LittleEndian(destination[60..], x15 + j15);
    }

    /// <summary>
    /// Combines whole blocks of input with the Salsa20 keystream by XOR, using the kernel dispatch selects.
    /// </summary>
    /// <param name="state">The sixteen-word state; its counter words are ignored.</param>
    /// <param name="counter">
    /// The block counter of the first block; each later block counts up by one, modulo 2^64.
    /// </param>
    /// <param name="input">The input, a whole number of 64-byte blocks.</param>
    /// <param name="output">
    /// The destination, at least as long as <paramref name="input" />; it may be the same memory, but must not
    /// partially overlap it.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="state" /> holds fewer than sixteen words, or <paramref name="output" /> is shorter than
    /// <paramref name="input" />.
    /// </exception>
    /// <exception cref="ArgumentException">The length of <paramref name="input" /> is not a multiple of 64.</exception>
    internal static void XorBlocks(ReadOnlySpan<uint> state, ulong counter, ReadOnlySpan<byte> input, Span<byte> output) =>
        XorBlocks(ChaCha20Core.KernelKind.Auto, state, counter, input, output);

    /// <summary>
    /// Combines whole blocks of input with the Salsa20 keystream by XOR, using the specified kernel: runs of the widest
    /// vectors it offers first, then narrower runs, then single blocks.
    /// </summary>
    /// <param name="kernel">
    /// The kernel, or <see cref="ChaCha20Core.KernelKind.Auto" /> for the one dispatch selects.
    /// </param>
    /// <param name="state">The sixteen-word state; its counter words are ignored.</param>
    /// <param name="counter">
    /// The block counter of the first block; each later block counts up by one, modulo 2^64.
    /// </param>
    /// <param name="input">The input, a whole number of 64-byte blocks.</param>
    /// <param name="output">
    /// The destination, at least as long as <paramref name="input" />; it may be the same memory, but must not
    /// partially overlap it.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="state" /> holds fewer than sixteen words, or <paramref name="output" /> is shorter than
    /// <paramref name="input" />.
    /// </exception>
    /// <exception cref="ArgumentException">The length of <paramref name="input" /> is not a multiple of 64.</exception>
    /// <remarks>
    /// Every kernel produces the keystream of <see cref="Block" />; a kernel the processor does not support throws
    /// <see cref="PlatformNotSupportedException" />.
    /// </remarks>
    internal static void XorBlocks(ChaCha20Core.KernelKind kernel, ReadOnlySpan<uint> state, ulong counter, ReadOnlySpan<byte> input, Span<byte> output)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(state.Length, StateWords, nameof(state));
        if (input.Length % BlockBytes != 0) throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Crypt_Invalid_InputLengthBlockMultiple, BlockBytes), nameof(input));
        ArgumentOutOfRangeException.ThrowIfLessThan(output.Length, input.Length, nameof(output));

        if (kernel == ChaCha20Core.KernelKind.Auto)
            kernel = ChaCha20Core.SelectKernel();

        ref uint words = ref MemoryMarshal.GetReference(state);
        int blocks = input.Length / BlockBytes;
        int done = 0;

        while (done < blocks)
        {
            int remaining = blocks - done;
            int lanes = ChaCha20Core.LanesFor(kernel, remaining);
            int groups = remaining / lanes;
            int offset = done * BlockBytes;
            ref byte source = ref Unsafe.Add(ref MemoryMarshal.GetReference(input), offset);
            ref byte destination = ref Unsafe.Add(ref MemoryMarshal.GetReference(output), offset);
            ulong first = counter + (ulong)done;

            switch (kernel)
            {
                case ChaCha20Core.KernelKind.Avx512Wide when lanes == 16:
                    Vector512Kernel.XorBlocks(ref words, first, ref source, ref destination, groups);
                    break;

                case ChaCha20Core.KernelKind.Avx512Wide or ChaCha20Core.KernelKind.Avx512 when lanes == 8:
                    Vector256Kernel<VectorRotation.Avx512>.XorBlocks(ref words, first, ref source, ref destination, groups);
                    break;

                case ChaCha20Core.KernelKind.Avx2 when lanes == 8:
                    Vector256Kernel<VectorRotation.Avx2>.XorBlocks(ref words, first, ref source, ref destination, groups);
                    break;

                case ChaCha20Core.KernelKind.Avx512Wide or ChaCha20Core.KernelKind.Avx512 when lanes == 4:
                    Vector128Kernel<VectorRotation.Avx512>.XorBlocks(ref words, first, ref source, ref destination, groups);
                    break;

                case ChaCha20Core.KernelKind.Avx2 or ChaCha20Core.KernelKind.Ssse3 when lanes == 4:
                    Vector128Kernel<VectorRotation.Ssse3>.XorBlocks(ref words, first, ref source, ref destination, groups);
                    break;

                case ChaCha20Core.KernelKind.AdvSimd when lanes == 4:
                    Vector128Kernel<VectorRotation.AdvSimd>.XorBlocks(ref words, first, ref source, ref destination, groups);
                    break;

                default:
                    XorBlocksScalar(state, first, input.Slice(offset, groups * BlockBytes), output.Slice(offset, groups * BlockBytes));
                    break;
            }

            done += groups * lanes;
        }
    }

    /// <summary>
    /// Applies the Salsa20 quarter round to four state words in place.
    /// </summary>
    /// <param name="a">The first word.</param>
    /// <param name="b">The second word.</param>
    /// <param name="c">The third word.</param>
    /// <param name="d">The fourth word.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void QuarterRound(ref uint a, ref uint b, ref uint c, ref uint d)
    {
        b ^= (a + d).RotateBitsLeftUnchecked(7);
        c ^= (b + a).RotateBitsLeftUnchecked(9);
        d ^= (c + b).RotateBitsLeftUnchecked(13);
        a ^= (d + c).RotateBitsLeftUnchecked(18);
    }

    /// <summary>
    /// Combines whole blocks of input with the keystream one block at a time, through <see cref="Block" />.
    /// </summary>
    /// <param name="state">The sixteen-word state.</param>
    /// <param name="counter">The block counter of the first block.</param>
    /// <param name="input">The input, a whole number of blocks.</param>
    /// <param name="output">The destination, as long as <paramref name="input" />.</param>
    private static void XorBlocksScalar(ReadOnlySpan<uint> state, ulong counter, ReadOnlySpan<byte> input, Span<byte> output)
    {
        Span<byte> keystream = stackalloc byte[BlockBytes];

        for (int offset = 0; offset < input.Length; offset += BlockBytes)
        {
            Block(state, counter++, keystream);
            CryptographyHelper.Xor(input.Slice(offset, BlockBytes), keystream, output.Slice(offset, BlockBytes));
        }

        CryptographicOperations.ZeroMemory(keystream);
    }
}
