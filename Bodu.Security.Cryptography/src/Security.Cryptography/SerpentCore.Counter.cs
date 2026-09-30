// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerpentCore.Counter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Bodu.Extensions;

namespace Bodu.Security.Cryptography;

// Serpent-128 in counter mode: the keystream of successive big-endian 128-bit counter blocks, combined with the input by
// XOR. The kernels form each counter block in registers and combine its keystream with the input as they store it, so
// no run of counter blocks is laid out in memory and no second pass combines the keystream.
internal static partial class SerpentCore
{
    /// <summary>
    /// Combines the input by XOR with the keystream of successive counter blocks under an expanded Serpent-128 key,
    /// with the kernel dispatch selects, and advances the counter past the blocks used.
    /// </summary>
    /// <param name="roundKeys">The 132 round-key words.</param>
    /// <param name="counterHigh">The counter's high 64 bits, the first eight bytes of the counter block.</param>
    /// <param name="counterLow">The counter's low 64 bits, the last eight bytes of the counter block.</param>
    /// <param name="input">The input; its last block may be partial.</param>
    /// <param name="output">
    /// The destination, at least as long as <paramref name="input" />; it may be the same memory, but must not
    /// partially overlap it.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="roundKeys" /> holds fewer than 132 words, or <paramref name="output" /> is shorter than
    /// <paramref name="input" />.
    /// </exception>
    internal static void XorCounterKeystream(ReadOnlySpan<uint> roundKeys, ref ulong counterHigh, ref ulong counterLow, ReadOnlySpan<byte> input, Span<byte> output) =>
        XorCounterKeystream(KernelKind.Auto, roundKeys, ref counterHigh, ref counterLow, input, output);

    /// <summary>
    /// Combines the input by XOR with the keystream of successive counter blocks under an expanded Serpent-128 key,
    /// with the specified kernel, and advances the counter past the blocks used: runs of the widest vectors the kernel
    /// offers first, then narrower runs, then single blocks, then a final partial block.
    /// </summary>
    /// <param name="kernel">The kernel, or <see cref="KernelKind.Auto" /> for the one dispatch selects.</param>
    /// <param name="roundKeys">The 132 round-key words.</param>
    /// <param name="counterHigh">The counter's high 64 bits, the first eight bytes of the counter block.</param>
    /// <param name="counterLow">The counter's low 64 bits, the last eight bytes of the counter block.</param>
    /// <param name="input">The input; its last block may be partial.</param>
    /// <param name="output">
    /// The destination, at least as long as <paramref name="input" />; it may be the same memory, but must not
    /// partially overlap it.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="roundKeys" /> holds fewer than 132 words, or <paramref name="output" /> is shorter than
    /// <paramref name="input" />.
    /// </exception>
    /// <remarks>
    /// The counter block is the big-endian 128-bit integer <c>counterHigh · 2⁶⁴ + counterLow</c>, which increases by
    /// one for every whole or partial block, modulo <c>2¹²⁸</c>. Every kind produces the same output.
    /// </remarks>
    internal static void XorCounterKeystream(KernelKind kernel, ReadOnlySpan<uint> roundKeys, ref ulong counterHigh, ref ulong counterLow, ReadOnlySpan<byte> input, Span<byte> output)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(roundKeys.Length, RoundKeyWords, nameof(roundKeys));
        ArgumentOutOfRangeException.ThrowIfLessThan(output.Length, input.Length, nameof(output));

        if (kernel == KernelKind.Auto)
            kernel = SelectKernel();

        ref uint keys = ref MemoryMarshal.GetReference(roundKeys);
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

            switch (kernel)
            {
                case KernelKind.Avx512 when lanes == 8:
                    Vector256Kernel<VectorRotation.Avx512>.XorCounterBlocks(ref keys, ref counterHigh, ref counterLow, ref source, ref destination, groups);
                    break;

                case KernelKind.Avx2 when lanes == 8:
                    Vector256Kernel<VectorRotation.Avx2>.XorCounterBlocks(ref keys, ref counterHigh, ref counterLow, ref source, ref destination, groups);
                    break;

                case KernelKind.Avx512 when lanes == 4:
                    Vector128Kernel<VectorRotation.Avx512>.XorCounterBlocks(ref keys, ref counterHigh, ref counterLow, ref source, ref destination, groups);
                    break;

                case KernelKind.Avx2 or KernelKind.Ssse3 when lanes == 4:
                    Vector128Kernel<VectorRotation.Ssse3>.XorCounterBlocks(ref keys, ref counterHigh, ref counterLow, ref source, ref destination, groups);
                    break;

                case KernelKind.AdvSimd when lanes == 4:
                    Vector128Kernel<VectorRotation.AdvSimd>.XorCounterBlocks(ref keys, ref counterHigh, ref counterLow, ref source, ref destination, groups);
                    break;

                default:
                    XorCounterBlocks(ref keys, ref counterHigh, ref counterLow, input.Slice(offset, groups * BlockBytes), output.Slice(offset, groups * BlockBytes));
                    break;
            }

            done += groups * lanes;
        }

        int whole = blocks * BlockBytes;
        if (whole < input.Length)
            XorCounterPartialBlock(ref keys, ref counterHigh, ref counterLow, input[whole..], output[whole..input.Length]);
    }

    /// <summary>
    /// Adds a number of blocks to a 128-bit counter held as two 64-bit halves, modulo <c>2¹²⁸</c>, without branching on
    /// the counter.
    /// </summary>
    /// <param name="counterHigh">The counter's high 64 bits.</param>
    /// <param name="counterLow">The counter's low 64 bits.</param>
    /// <param name="blocks">The number of blocks to add.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AdvanceCounter(ref ulong counterHigh, ref ulong counterLow, ulong blocks)
    {
        ulong sum = counterLow + blocks;

        // The top bit of this expression is the carry out of the low half: set when both addends have their top bit
        // set, or when either has and the sum has not.
        counterHigh += ((counterLow & blocks) | ((counterLow | blocks) & ~sum)) >> 63;
        counterLow = sum;
    }

    /// <summary>
    /// Combines whole blocks of input by XOR with the keystream of successive counter blocks, one block at a time, and
    /// advances the counter past them.
    /// </summary>
    /// <param name="roundKeys">The first of the 132 round-key words.</param>
    /// <param name="counterHigh">The counter's high 64 bits; advanced past the blocks used.</param>
    /// <param name="counterLow">The counter's low 64 bits; advanced past the blocks used.</param>
    /// <param name="input">The input, a whole number of blocks.</param>
    /// <param name="output">The destination, as long as <paramref name="input" />; it may be the same memory.</param>
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void XorCounterBlocks(ref uint roundKeys, ref ulong counterHigh, ref ulong counterLow, ReadOnlySpan<byte> input, Span<byte> output)
    {
        ulong high = counterHigh;
        ulong low = counterLow;

        for (int offset = 0; offset < input.Length; offset += BlockBytes)
        {
            // Serpent reads a block as four little-endian words, so each word of a big-endian counter block is the byte
            // reversal of a 32-bit quarter of the counter.
            uint x0 = BinaryPrimitives.ReverseEndianness((uint)(high >> 32));
            uint x1 = BinaryPrimitives.ReverseEndianness((uint)high);
            uint x2 = BinaryPrimitives.ReverseEndianness((uint)(low >> 32));
            uint x3 = BinaryPrimitives.ReverseEndianness((uint)low);

            EncryptRounds(ref x0, ref x1, ref x2, ref x3, ref roundKeys);

            ReadOnlySpan<byte> source = input.Slice(offset, BlockBytes);
            x0 ^= BinaryPrimitives.ReadUInt32LittleEndian(source);
            x1 ^= BinaryPrimitives.ReadUInt32LittleEndian(source[4..]);
            x2 ^= BinaryPrimitives.ReadUInt32LittleEndian(source[8..]);
            x3 ^= BinaryPrimitives.ReadUInt32LittleEndian(source[12..]);

            Span<byte> destination = output.Slice(offset, BlockBytes);
            BinaryPrimitives.WriteUInt32LittleEndian(destination, x0);
            BinaryPrimitives.WriteUInt32LittleEndian(destination[4..], x1);
            BinaryPrimitives.WriteUInt32LittleEndian(destination[8..], x2);
            BinaryPrimitives.WriteUInt32LittleEndian(destination[12..], x3);

            AdvanceCounter(ref high, ref low, 1);
        }

        counterHigh = high;
        counterLow = low;
    }

    /// <summary>
    /// Combines a final partial block by XOR with the keystream of one counter block, through a block-sized buffer on
    /// the stack, and advances the counter past it.
    /// </summary>
    /// <param name="roundKeys">The first of the 132 round-key words.</param>
    /// <param name="counterHigh">The counter's high 64 bits; advanced past the block.</param>
    /// <param name="counterLow">The counter's low 64 bits; advanced past the block.</param>
    /// <param name="input">The input: fewer bytes than a block.</param>
    /// <param name="output">The destination, as long as <paramref name="input" />; it may be the same memory.</param>
    /// <remarks>
    /// The bytes of the buffer past the input receive bare keystream, so the buffer is cleared before returning.
    /// </remarks>
    private static void XorCounterPartialBlock(ref uint roundKeys, ref ulong counterHigh, ref ulong counterLow, ReadOnlySpan<byte> input, Span<byte> output)
    {
        Span<byte> block = stackalloc byte[BlockBytes];
        block.Clear();
        input.CopyTo(block);

        XorCounterBlocks(ref roundKeys, ref counterHigh, ref counterLow, block, block);
        block[..input.Length].CopyTo(output);
        CryptographyHelper.Clear(block);
    }
}
