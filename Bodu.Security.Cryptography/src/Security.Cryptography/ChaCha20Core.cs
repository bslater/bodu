// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ChaCha20Core.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Security.Cryptography;
using Bodu.Extensions;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Implements the ChaCha20 block function (RFC 8439, Section 2.3) for <see cref="ChaCha20StreamCipher" />: one block at
/// a time, and whole runs of blocks through vector kernels that give each lane a block of its own.
/// </summary>
/// <remarks>
/// <para>
/// The state is sixteen 32-bit words: the constant <c>"expand 32-byte k"</c>, the eight key words, the block counter
/// and the three nonce words. Every entry point takes the counter of its first block separately, whatever the state's
/// counter word holds, and the blocks after it count up modulo 2^32.
/// </para>
/// <para>
/// The many-block kernels keep one state word of 4, 8 or 16 consecutive blocks in each vector, with the blocks'
/// counters in successive lanes, so a quarter round costs the same few instructions for every block at once. The words
/// are transposed back into block order as the keystream meets the input. Rotations by 16 and 8 bits are byte shuffles
/// where the processor has no rotate instruction, and rotations by 12 and 7 are shift pairs. The rotations go through
/// Bodu.Core's <see cref="VectorExtensions" />, which take the instruction set as a type argument, so each kernel is
/// compiled once per instruction set and the tests can run every one the processor supports.
/// </para>
/// <para>
/// <see cref="Salsa20Core" /> shares the kernel kinds, the rotations and the transposition back into block order:
/// Salsa20 differs only in its round function and in where its state keeps the counter.
/// </para>
/// </remarks>
[SkipLocalsInit]
internal static partial class ChaCha20Core
{
    /// <summary>The number of 32-bit words in the ChaCha20 state.</summary>
    internal const int StateWords = 16;

    /// <summary>The number of bytes in a keystream block.</summary>
    internal const int BlockBytes = 64;

    /// <summary>The number of bytes in a ChaCha20 key.</summary>
    internal const int KeyBytes = 32;

    /// <summary>The number of bytes in the RFC 8439 nonce.</summary>
    internal const int NonceBytes = 12;

    /// <summary>The index of the block counter in the state.</summary>
    internal const int CounterWord = 12;

    /// <summary>The number of blocks the narrowest vector kernels compute at once: one in each 32-bit lane of a 128-bit vector.</summary>
    internal const int NarrowestKernelLanes = 4;

    /// <summary>The first little-endian word of the ASCII constant <c>"expand 32-byte k"</c>.</summary>
    internal const uint Sigma0 = 0x61707865;

    /// <summary>The second little-endian word of the ASCII constant <c>"expand 32-byte k"</c>.</summary>
    internal const uint Sigma1 = 0x3320646e;

    /// <summary>The third little-endian word of the ASCII constant <c>"expand 32-byte k"</c>.</summary>
    internal const uint Sigma2 = 0x79622d32;

    /// <summary>The fourth little-endian word of the ASCII constant <c>"expand 32-byte k"</c>.</summary>
    internal const uint Sigma3 = 0x6b206574;

    /// <summary>The number of double rounds: a column round and a diagonal round each, twenty rounds in all.</summary>
    private const int DoubleRounds = 10;

    /// <summary>
    /// Seeds a ChaCha20 state from a key and a nonce, with a zero block counter.
    /// </summary>
    /// <param name="state">The state to write: at least sixteen words, of which the first sixteen are written.</param>
    /// <param name="key">The 32-byte key.</param>
    /// <param name="nonce">The 12-byte nonce.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="state" /> holds fewer than sixteen words, <paramref name="key" /> is not 32 bytes long, or
    /// <paramref name="nonce" /> is not 12 bytes long.
    /// </exception>
    internal static void Initialize(Span<uint> state, ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(state.Length, StateWords, nameof(state));
        ArgumentOutOfRangeException.ThrowIfNotEqual(key.Length, KeyBytes, nameof(key));
        ArgumentOutOfRangeException.ThrowIfNotEqual(nonce.Length, NonceBytes, nameof(nonce));

        state[0] = Sigma0;
        state[1] = Sigma1;
        state[2] = Sigma2;
        state[3] = Sigma3;

        for (int i = 0; i < 8; i++)
            state[4 + i] = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(i * sizeof(uint)));

        state[CounterWord] = 0;
        state[13] = BinaryPrimitives.ReadUInt32LittleEndian(nonce);
        state[14] = BinaryPrimitives.ReadUInt32LittleEndian(nonce[4..]);
        state[15] = BinaryPrimitives.ReadUInt32LittleEndian(nonce[8..]);
    }

    /// <summary>
    /// Produces one keystream block: the ChaCha20 block function over the state, with the specified block counter.
    /// </summary>
    /// <param name="state">The sixteen-word state; its counter word is ignored.</param>
    /// <param name="counter">The block counter.</param>
    /// <param name="destination">Receives the 64-byte block in its first 64 bytes.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="state" /> holds fewer than sixteen words, or <paramref name="destination" /> fewer than 64
    /// bytes.
    /// </exception>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1107:Code should not contain multiple statements on one line", Justification = "The sixteen state words are loaded four to a line, as the 4×4 matrix RFC 8439 draws.")]
    internal static void Block(ReadOnlySpan<uint> state, uint counter, Span<byte> destination)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(state.Length, StateWords, nameof(state));
        ArgumentOutOfRangeException.ThrowIfLessThan(destination.Length, BlockBytes, nameof(destination));

        uint j0 = state[0], j1 = state[1], j2 = state[2], j3 = state[3];
        uint j4 = state[4], j5 = state[5], j6 = state[6], j7 = state[7];
        uint j8 = state[8], j9 = state[9], j10 = state[10], j11 = state[11];
        uint j12 = counter, j13 = state[13], j14 = state[14], j15 = state[15];

        uint x0 = j0, x1 = j1, x2 = j2, x3 = j3;
        uint x4 = j4, x5 = j5, x6 = j6, x7 = j7;
        uint x8 = j8, x9 = j9, x10 = j10, x11 = j11;
        uint x12 = j12, x13 = j13, x14 = j14, x15 = j15;

        for (int round = 0; round < DoubleRounds; round++)
        {
            // Column round.
            QuarterRound(ref x0, ref x4, ref x8, ref x12);
            QuarterRound(ref x1, ref x5, ref x9, ref x13);
            QuarterRound(ref x2, ref x6, ref x10, ref x14);
            QuarterRound(ref x3, ref x7, ref x11, ref x15);

            // Diagonal round.
            QuarterRound(ref x0, ref x5, ref x10, ref x15);
            QuarterRound(ref x1, ref x6, ref x11, ref x12);
            QuarterRound(ref x2, ref x7, ref x8, ref x13);
            QuarterRound(ref x3, ref x4, ref x9, ref x14);
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
    /// Combines whole blocks of input with the ChaCha20 keystream by XOR, using the kernel dispatch selects.
    /// </summary>
    /// <param name="state">The sixteen-word state; its counter word is ignored.</param>
    /// <param name="counter">
    /// The block counter of the first block; each later block counts up by one, modulo 2^32.
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
    internal static void XorBlocks(ReadOnlySpan<uint> state, uint counter, ReadOnlySpan<byte> input, Span<byte> output) =>
        XorBlocks(KernelKind.Auto, state, counter, input, output);

    /// <summary>
    /// Combines whole blocks of input with the ChaCha20 keystream by XOR, using the specified kernel: runs of the
    /// widest vectors it offers first, then narrower runs, then single blocks.
    /// </summary>
    /// <param name="kernel">The kernel, or <see cref="KernelKind.Auto" /> for the one dispatch selects.</param>
    /// <param name="state">The sixteen-word state; its counter word is ignored.</param>
    /// <param name="counter">
    /// The block counter of the first block; each later block counts up by one, modulo 2^32.
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
    internal static void XorBlocks(KernelKind kernel, ReadOnlySpan<uint> state, uint counter, ReadOnlySpan<byte> input, Span<byte> output)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(state.Length, StateWords, nameof(state));
        if (input.Length % BlockBytes != 0) throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Crypt_Invalid_InputLengthBlockMultiple, BlockBytes), nameof(input));
        ArgumentOutOfRangeException.ThrowIfLessThan(output.Length, input.Length, nameof(output));

        if (kernel == KernelKind.Auto)
            kernel = SelectKernel();

        ref uint words = ref MemoryMarshal.GetReference(state);
        int blocks = input.Length / BlockBytes;
        int done = 0;

        while (done < blocks)
        {
            int remaining = blocks - done;
            int lanes = LanesFor(kernel, remaining);
            int groups = remaining / lanes;
            int offset = done * BlockBytes;
            ref byte source = ref Unsafe.Add(ref MemoryMarshal.GetReference(input), offset);
            ref byte destination = ref Unsafe.Add(ref MemoryMarshal.GetReference(output), offset);
            uint first = counter + (uint)done;

            switch (kernel)
            {
                case KernelKind.Avx512Wide when lanes == 16:
                    Vector512Kernel.XorBlocks(ref words, first, ref source, ref destination, groups);
                    break;

                case KernelKind.Avx512Wide or KernelKind.Avx512 when lanes == 8:
                    Vector256Kernel<VectorRotation.Avx512>.XorBlocks(ref words, first, ref source, ref destination, groups);
                    break;

                case KernelKind.Avx2 when lanes == 8:
                    Vector256Kernel<VectorRotation.Avx2>.XorBlocks(ref words, first, ref source, ref destination, groups);
                    break;

                case KernelKind.Avx512Wide or KernelKind.Avx512 when lanes == 4:
                    Vector128Kernel<VectorRotation.Avx512>.XorBlocks(ref words, first, ref source, ref destination, groups);
                    break;

                case KernelKind.Avx2 or KernelKind.Ssse3 when lanes == 4:
                    Vector128Kernel<VectorRotation.Ssse3>.XorBlocks(ref words, first, ref source, ref destination, groups);
                    break;

                case KernelKind.AdvSimd when lanes == 4:
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
    /// Selects the widest kernel the processor supports and the process allows: AVX-512, then AVX2, then AdvSimd on
    /// ARM64, then SSSE3, then the scalar kernel.
    /// </summary>
    /// <returns>The kernel dispatch runs; never <see cref="KernelKind.Auto" />.</returns>
    /// <remarks>
    /// <para>
    /// Every gate honors the <see cref="SimdCapabilities.DisableSimdSwitchName" /> switch, which pins the scalar
    /// kernel.
    /// </para>
    /// <para>
    /// AVX-512 takes the sixteen-block kernel only where <see cref="Vector512.IsHardwareAccelerated" /> holds. The
    /// runtime clears it on processors whose clock drops under sustained 512-bit work, so there the eight-block kernel
    /// runs instead; <c>DOTNET_PreferredVectorBitWidth=512</c> opts such a processor in.
    /// </para>
    /// </remarks>
    internal static KernelKind SelectKernel()
    {
        if (SimdCapabilities.Avx512FVL)
            return Vector512.IsHardwareAccelerated ? KernelKind.Avx512Wide : KernelKind.Avx512;

        if (SimdCapabilities.Avx2)
            return KernelKind.Avx2;

        if (SimdCapabilities.AdvSimd)
            return KernelKind.AdvSimd;

        return SimdCapabilities.Ssse3 ? KernelKind.Ssse3 : KernelKind.Scalar;
    }

    /// <summary>
    /// Determines whether the processor can run the specified kernel, whether or not the process allows vector code.
    /// </summary>
    /// <param name="kernel">The kernel.</param>
    /// <returns>
    /// <see langword="true" /> if the processor supports every instruction the kernel uses; otherwise,
    /// <see langword="false" />.
    /// </returns>
    internal static bool IsSupported(KernelKind kernel) => kernel switch
    {
        KernelKind.Auto or KernelKind.Scalar => true,
        KernelKind.Ssse3 => System.Runtime.Intrinsics.X86.Ssse3.IsSupported,
        KernelKind.AdvSimd => System.Runtime.Intrinsics.Arm.AdvSimd.Arm64.IsSupported,
        KernelKind.Avx2 => System.Runtime.Intrinsics.X86.Avx2.IsSupported,
        KernelKind.Avx512 or KernelKind.Avx512Wide => System.Runtime.Intrinsics.X86.Avx2.IsSupported && System.Runtime.Intrinsics.X86.Avx512F.VL.IsSupported,
        _ => false,
    };

    /// <summary>
    /// Returns the number of blocks a run of the specified kernel takes at once, given how many remain: the widest it
    /// offers that fits, down to one for the scalar block function.
    /// </summary>
    /// <param name="kernel">The kernel; not <see cref="KernelKind.Auto" />.</param>
    /// <param name="remaining">The number of blocks left.</param>
    /// <returns>16, 8, 4 or 1.</returns>
    /// <remarks>
    /// Inlined, so that for a kernel known when the caller is compiled, as the Poly1305 AEADs' keystream is, the widths
    /// fold to constants.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int LanesFor(KernelKind kernel, int remaining) => kernel switch
    {
        KernelKind.Avx512Wide when remaining >= 16 => 16,
        KernelKind.Avx512Wide or KernelKind.Avx512 or KernelKind.Avx2 when remaining >= 8 => 8,
        KernelKind.Avx512Wide or KernelKind.Avx512 or KernelKind.Avx2 or KernelKind.Ssse3 or KernelKind.AdvSimd when remaining >= 4 => 4,
        _ => 1,
    };

    /// <summary>
    /// Estimates the time one step of the specified kernel takes over the specified number of lanes, in halves of the
    /// time the block function takes over one block.
    /// </summary>
    /// <param name="kernel">The kernel; not <see cref="KernelKind.Auto" />.</param>
    /// <param name="lanes">
    /// The number of blocks the step computes: 1, 4, 8 or 16, as <see cref="LanesFor" /> returns.
    /// </param>
    /// <returns>
    /// 2 for the block function; 2 for four or eight blocks with AVX-512VL, and 3 for sixteen; 4 for four or eight
    /// blocks on the other kernels.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The estimates follow measurements on .NET 8 and .NET 10, each step against the block function over one block.
    /// With AVX-512VL, whose 32 vector registers hold a kernel's sixteen state vectors and its temporaries, a step of
    /// four or eight blocks takes 0.9 to 1.2 times as long, and a step of sixteen 1.4 to 1.6 times as long, for
    /// ChaCha20 and Salsa20 alike. With 16 registers the AVX2 and SSSE3 kernels spill: a step takes 1.7 to 2.0 times as
    /// long for ChaCha20, and 2.0 to 3.5 times for Salsa20, whose rotations all take two shifts. The estimate follows
    /// ChaCha20. The ARM64 kernel is taken to cost as the AVX2 and SSSE3 kernels do; on a Neoverse N2, a 64-byte
    /// message sealed as fast under its plan as under the scalar kernel's.
    /// </para>
    /// <para>
    /// The keystream a step produces is the same whichever kernel runs it, so the estimates only choose between ways of
    /// drawing the same blocks; <see cref="Poly1305AeadCore" /> draws a short message's keystream the cheaper way.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int StepCost(KernelKind kernel, int lanes) => lanes switch
    {
        1 => 2,
        16 => 3,
        _ => kernel is KernelKind.Avx512 or KernelKind.Avx512Wide ? 2 : 4,
    };

    /// <summary>
    /// Estimates the time
    /// <see cref="XorBlocks(KernelKind, ReadOnlySpan{uint}, uint, ReadOnlySpan{byte}, Span{byte})" /> takes over a run
    /// of blocks, in the units of <see cref="StepCost" />.
    /// </summary>
    /// <param name="kernel">The kernel; not <see cref="KernelKind.Auto" />.</param>
    /// <param name="blocks">The number of blocks in the run.</param>
    /// <returns>The sum of the costs of the steps the run is divided into; zero for an empty run.</returns>
    /// <remarks>
    /// The run is divided as the kernel divides it: into as many groups of the widest lanes <see cref="LanesFor" />
    /// offers as fit, then of the next widest, down to the blocks left over, which the block function takes one at a
    /// time.
    /// </remarks>
    internal static int CostFor(KernelKind kernel, int blocks)
    {
        int cost = 0;

        while (blocks > 0)
        {
            int lanes = LanesFor(kernel, blocks);
            int groups = blocks / lanes;
            cost += groups * StepCost(kernel, lanes);
            blocks -= groups * lanes;
        }

        return cost;
    }

    /// <summary>
    /// Applies the ChaCha20 quarter round (RFC 8439, Section 2.1) to four state words in place.
    /// </summary>
    /// <param name="a">The first word.</param>
    /// <param name="b">The second word.</param>
    /// <param name="c">The third word.</param>
    /// <param name="d">The fourth word.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1107:Code should not contain multiple statements on one line", Justification = "The grouped add / XOR / rotate steps mirror the RFC 8439 quarter-round definition and preserve a compact, specification-like layout.")]
    internal static void QuarterRound(ref uint a, ref uint b, ref uint c, ref uint d)
    {
        a += b; d ^= a; d = d.RotateBitsLeftUnchecked(16);
        c += d; b ^= c; b = b.RotateBitsLeftUnchecked(12);
        a += b; d ^= a; d = d.RotateBitsLeftUnchecked(8);
        c += d; b ^= c; b = b.RotateBitsLeftUnchecked(7);
    }

    /// <summary>
    /// Combines whole blocks of input with the keystream one block at a time, through <see cref="Block" />.
    /// </summary>
    /// <param name="state">The sixteen-word state.</param>
    /// <param name="counter">The block counter of the first block.</param>
    /// <param name="input">The input, a whole number of blocks.</param>
    /// <param name="output">The destination, as long as <paramref name="input" />.</param>
    private static void XorBlocksScalar(ReadOnlySpan<uint> state, uint counter, ReadOnlySpan<byte> input, Span<byte> output)
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
