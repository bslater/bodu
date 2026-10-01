// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake3Core.Subtree.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

internal static partial class Blake3Core
{
    /// <summary>The number of chunks a subtree compresses as one batch, its chaining values reduced on the stack: 64 chunks, whose chaining values take 2 KiB.</summary>
    private const int BatchChunks = 64;

    /// <summary>
    /// Compresses whole chunks, many at once, into their chaining values with the specified kernel.
    /// </summary>
    /// <param name="kernel">
    /// The kernel; <see cref="KernelKind.Auto" /> for the one dispatch selects. Any other kind must be one the
    /// processor supports.
    /// </param>
    /// <param name="chunks">The chunks, 1024 bytes each.</param>
    /// <param name="key">The eight-word key every chunk starts from.</param>
    /// <param name="counter">The counter of the first chunk; each later chunk's is one more.</param>
    /// <param name="flags">The mode's flags, on every block.</param>
    /// <param name="chainingValues">The chunks' chaining values, 32 bytes each, in order.</param>
    /// <exception cref="ArgumentException">
    /// The length of <paramref name="chunks" /> is not a multiple of 1024 bytes.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="key" /> holds fewer than eight words, or <paramref name="chainingValues" /> fewer than 32 bytes
    /// per chunk.
    /// </exception>
    /// <remarks>
    /// Every chunk is compressed as a non-root chunk: its first block carries <see cref="ChunkStart" /> and its last
    /// <see cref="ChunkEnd" />.
    /// </remarks>
    internal static void CompressChunks(KernelKind kernel, ReadOnlySpan<byte> chunks, ReadOnlySpan<uint> key, ulong counter, uint flags, Span<byte> chainingValues)
    {
        if (chunks.Length % ChunkBytes != 0) throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Crypt_Invalid_InputLengthBlockMultiple, ChunkBytes), nameof(chunks));
        ThrowHelper.ThrowIfLessThan(key.Length, ChainingValueWords, nameof(key));
        ThrowHelper.ThrowIfLessThan(chainingValues.Length, chunks.Length / ChunkBytes * ChainingValueBytes, nameof(chainingValues));

        HashMany(
            kernel,
            ref MemoryMarshal.GetReference(chunks),
            ChunkBytes,
            chunks.Length / ChunkBytes,
            ChunkBytes / BlockBytes,
            ref MemoryMarshal.GetReference(key),
            counter,
            incrementCounter: true,
            flags,
            ChunkStart,
            ChunkEnd,
            ref MemoryMarshal.GetReference(chainingValues));
    }

    /// <summary>
    /// Compresses parent nodes, many at once, into their chaining values with the specified kernel.
    /// </summary>
    /// <param name="kernel">
    /// The kernel; <see cref="KernelKind.Auto" /> for the one dispatch selects. Any other kind must be one the
    /// processor supports.
    /// </param>
    /// <param name="children">
    /// The parents' blocks, 64 bytes each: the left child's encoded chaining value followed by the right child's.
    /// </param>
    /// <param name="key">The eight-word key every parent starts from.</param>
    /// <param name="flags">The mode's flags; <see cref="Parent" /> is added to them.</param>
    /// <param name="chainingValues">
    /// The parents' chaining values, 32 bytes each, in order. It may start at the first byte of
    /// <paramref name="children" />, which reduces one level of a tree in place.
    /// </param>
    /// <exception cref="ArgumentException">
    /// The length of <paramref name="children" /> is not a multiple of 64 bytes.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="key" /> holds fewer than eight words, or <paramref name="chainingValues" /> fewer than 32 bytes
    /// per parent.
    /// </exception>
    /// <remarks>
    /// Every parent is compressed as a non-root parent. In place, each group of parents is read before its chaining
    /// values are written, and a chaining value never lies past its own block, so no parent's block is overwritten
    /// before it is read.
    /// </remarks>
    internal static void CompressParents(KernelKind kernel, ReadOnlySpan<byte> children, ReadOnlySpan<uint> key, uint flags, Span<byte> chainingValues)
    {
        if (children.Length % BlockBytes != 0) throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Crypt_Invalid_InputLengthBlockMultiple, BlockBytes), nameof(children));
        ThrowHelper.ThrowIfLessThan(key.Length, ChainingValueWords, nameof(key));
        ThrowHelper.ThrowIfLessThan(chainingValues.Length, children.Length / BlockBytes * ChainingValueBytes, nameof(chainingValues));

        HashMany(
            kernel,
            ref MemoryMarshal.GetReference(children),
            BlockBytes,
            children.Length / BlockBytes,
            1,
            ref MemoryMarshal.GetReference(key),
            0,
            incrementCounter: false,
            flags | Parent,
            0,
            0,
            ref MemoryMarshal.GetReference(chainingValues));
    }

    /// <summary>
    /// Computes the chaining value of a complete subtree with the kernel dispatch selects.
    /// </summary>
    /// <param name="input">The subtree's chunks: a power of two of whole 1024-byte chunks.</param>
    /// <param name="key">The eight-word key.</param>
    /// <param name="counter">The counter of the subtree's first chunk.</param>
    /// <param name="flags">The mode's flags.</param>
    /// <param name="chainingValue">The eight words that receive the subtree's chaining value.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="input" /> does not hold a power of two of whole chunks.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="key" /> or <paramref name="chainingValue" /> holds fewer than eight words.
    /// </exception>
    internal static void CompressSubtree(ReadOnlySpan<byte> input, ReadOnlySpan<uint> key, ulong counter, uint flags, Span<uint> chainingValue) =>
        CompressSubtree(SelectKernel(), input, key, counter, flags, chainingValue);

    /// <summary>
    /// Computes the chaining value of a complete subtree with the specified kernel.
    /// </summary>
    /// <param name="kernel">
    /// The kernel; <see cref="KernelKind.Auto" /> for the one dispatch selects. Any other kind must be one the
    /// processor supports.
    /// </param>
    /// <param name="input">The subtree's chunks: a power of two of whole 1024-byte chunks.</param>
    /// <param name="key">The eight-word key.</param>
    /// <param name="counter">The counter of the subtree's first chunk.</param>
    /// <param name="flags">The mode's flags.</param>
    /// <param name="chainingValue">The eight words that receive the subtree's chaining value.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="input" /> does not hold a power of two of whole chunks.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="key" /> or <paramref name="chainingValue" /> holds fewer than eight words.
    /// </exception>
    /// <remarks>
    /// <para>
    /// A subtree of <c>2^k</c> chunks starting at a counter that is a multiple of <c>2^k</c> is a node of every BLAKE3
    /// tree that extends past it, so its chaining value is the one the specification's tree computes there; the root is
    /// never such a subtree, and no node here carries <see cref="Root" />.
    /// </para>
    /// <para>
    /// Up to 64 chunks are compressed as one batch, as many at once as the kernel's lanes allow, and their chaining
    /// values reduced level by level, as many parents at once. A larger subtree is split into halves, each computed the
    /// same way, and joined by one parent.
    /// </para>
    /// </remarks>
    internal static void CompressSubtree(KernelKind kernel, ReadOnlySpan<byte> input, ReadOnlySpan<uint> key, ulong counter, uint flags, Span<uint> chainingValue)
    {
        if (input.Length % ChunkBytes != 0 || !BitOperations.IsPow2(input.Length / ChunkBytes)) throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Arg_Invalid_Blake3SubtreeChunkCount, ChunkBytes), nameof(input));
        ThrowHelper.ThrowIfLessThan(key.Length, ChainingValueWords, nameof(key));
        ThrowHelper.ThrowIfLessThan(chainingValue.Length, ChainingValueWords, nameof(chainingValue));

        CompressSubtreeCore(kernel == KernelKind.Auto ? SelectKernel() : kernel, input, key, counter, flags, chainingValue);
    }

    /// <summary>
    /// Computes the chaining value of a validated subtree, splitting it into halves until each fits one batch.
    /// </summary>
    /// <param name="kernel">The kernel; never <see cref="KernelKind.Auto" />.</param>
    /// <param name="input">The subtree's chunks: a power of two of whole chunks.</param>
    /// <param name="key">The eight-word key.</param>
    /// <param name="counter">The counter of the subtree's first chunk.</param>
    /// <param name="flags">The mode's flags.</param>
    /// <param name="chainingValue">The eight words that receive the subtree's chaining value.</param>
    private static void CompressSubtreeCore(KernelKind kernel, ReadOnlySpan<byte> input, ReadOnlySpan<uint> key, ulong counter, uint flags, Span<uint> chainingValue)
    {
        int chunks = input.Length / ChunkBytes;
        if (chunks <= BatchChunks)
        {
            CompressBatch(kernel, input, key, counter, flags, chainingValue);
            return;
        }

        Span<uint> left = stackalloc uint[ChainingValueWords];
        Span<byte> children = stackalloc byte[BlockBytes];
        int half = input.Length / 2;

        CompressSubtreeCore(kernel, input[..half], key, counter, flags, left);
        CompressSubtreeCore(kernel, input[half..], key, counter + (ulong)(chunks / 2), flags, chainingValue);

        StoreChainingValue(left, children);
        StoreChainingValue(chainingValue, children[ChainingValueBytes..]);
        key[..ChainingValueWords].CopyTo(chainingValue);
        Compress(kernel, chainingValue, children, 0, BlockBytes, flags | Parent);

        left.Clear();
        CryptographicOperations.ZeroMemory(children);
    }

    /// <summary>
    /// Computes the chaining value of a subtree of at most 64 chunks: every chunk compressed, then every level of
    /// parents, each level in place over the one below it.
    /// </summary>
    /// <param name="kernel">The kernel; never <see cref="KernelKind.Auto" />.</param>
    /// <param name="input">The subtree's chunks: a power of two of whole chunks, at most 64.</param>
    /// <param name="key">The eight-word key.</param>
    /// <param name="counter">The counter of the subtree's first chunk.</param>
    /// <param name="flags">The mode's flags.</param>
    /// <param name="chainingValue">The eight words that receive the subtree's chaining value.</param>
    private static void CompressBatch(KernelKind kernel, ReadOnlySpan<byte> input, ReadOnlySpan<uint> key, ulong counter, uint flags, Span<uint> chainingValue)
    {
        int count = input.Length / ChunkBytes;
        Span<byte> chainingValues = stackalloc byte[BatchChunks * ChainingValueBytes];
        ref byte values = ref MemoryMarshal.GetReference(chainingValues);
        ref uint keyWords = ref MemoryMarshal.GetReference(key);

        HashMany(kernel, ref MemoryMarshal.GetReference(input), ChunkBytes, count, ChunkBytes / BlockBytes, ref keyWords, counter, incrementCounter: true, flags, ChunkStart, ChunkEnd, ref values);
        for (; count > 1; count /= 2)
            HashMany(kernel, ref values, BlockBytes, count / 2, 1, ref keyWords, 0, incrementCounter: false, flags | Parent, 0, 0, ref values);

        LoadChainingValue(chainingValues, chainingValue);
        CryptographicOperations.ZeroMemory(chainingValues[..(input.Length / ChunkBytes * ChainingValueBytes)]);
    }

    /// <summary>
    /// Compresses equally spaced inputs of whole blocks, each from the key to its chaining value: sixteen at a time on
    /// the 512-bit kernel, eight on the 256-bit kernel, up to four on the 128-bit kernel, and a lone input block by
    /// block.
    /// </summary>
    /// <param name="kernel">
    /// The kernel; <see cref="KernelKind.Auto" /> for the one dispatch selects. Any other kind must be one the
    /// processor supports.
    /// </param>
    /// <param name="input">The first byte of the first input.</param>
    /// <param name="stride">The distance, in bytes, between the starts of consecutive inputs.</param>
    /// <param name="count">The number of inputs.</param>
    /// <param name="blocks">The number of 64-byte blocks in every input.</param>
    /// <param name="key">The first of the eight key words every input starts from.</param>
    /// <param name="counter">The counter of the first input.</param>
    /// <param name="incrementCounter">
    /// <see langword="true" /> to give input <c>i</c> the counter <paramref name="counter" /> + <c>i</c>;
    /// <see langword="false" /> to give every input <paramref name="counter" />.
    /// </param>
    /// <param name="flags">The flags on every block.</param>
    /// <param name="flagsStart">The flags added to every input's first block.</param>
    /// <param name="flagsEnd">The flags added to every input's last block.</param>
    /// <param name="output">The first byte of the chaining values, 32 bytes to an input.</param>
    private static void HashMany(
        KernelKind kernel,
        ref byte input,
        nint stride,
        int count,
        int blocks,
        ref uint key,
        ulong counter,
        bool incrementCounter,
        uint flags,
        uint flagsStart,
        uint flagsEnd,
        ref byte output)
    {
        if (kernel == KernelKind.Auto)
            kernel = SelectKernel();

        while (count > 0)
        {
            int lanes = kernel switch
            {
                KernelKind.Avx512Wide when count > 8 => Math.Min(count, 16),
                KernelKind.Avx512Wide or KernelKind.Avx512 or KernelKind.Avx2 when count > 4 => Math.Min(count, 8),
                KernelKind.Avx512Wide or KernelKind.Avx512 or KernelKind.Avx2 or KernelKind.Ssse3 or KernelKind.AdvSimd when count > 1 => Math.Min(count, 4),
                _ => 1,
            };

            switch (kernel)
            {
                case KernelKind.Avx512Wide when lanes > 8:
                    Vector512Kernel.HashMany(ref input, stride, lanes, blocks, ref key, counter, incrementCounter, flags, flagsStart, flagsEnd, ref output);
                    break;

                case KernelKind.Avx512Wide or KernelKind.Avx512 when lanes > 4:
                    Vector256Kernel<Avx512Isa>.HashMany(ref input, stride, lanes, blocks, ref key, counter, incrementCounter, flags, flagsStart, flagsEnd, ref output);
                    break;

                case KernelKind.Avx2 when lanes > 4:
                    Vector256Kernel<Avx2Isa>.HashMany(ref input, stride, lanes, blocks, ref key, counter, incrementCounter, flags, flagsStart, flagsEnd, ref output);
                    break;

                case KernelKind.Avx512Wide or KernelKind.Avx512 when lanes > 1:
                    Vector128Kernel<Blake2sCore.Avx512Isa>.HashMany(ref input, stride, lanes, blocks, ref key, counter, incrementCounter, flags, flagsStart, flagsEnd, ref output);
                    break;

                case KernelKind.Avx2 or KernelKind.Ssse3 when lanes > 1:
                    Vector128Kernel<Blake2sCore.Ssse3Isa>.HashMany(ref input, stride, lanes, blocks, ref key, counter, incrementCounter, flags, flagsStart, flagsEnd, ref output);
                    break;

                case KernelKind.AdvSimd when lanes > 1:
                    Vector128Kernel<Blake2sCore.AdvSimdIsa>.HashMany(ref input, stride, lanes, blocks, ref key, counter, incrementCounter, flags, flagsStart, flagsEnd, ref output);
                    break;

                default:
                    HashOne(kernel, ref input, blocks, ref key, counter, flags, flagsStart, flagsEnd, ref output);
                    break;
            }

            input = ref Unsafe.Add(ref input, lanes * stride);
            output = ref Unsafe.Add(ref output, lanes * ChainingValueBytes);
            if (incrementCounter)
                counter += (ulong)lanes;

            count -= lanes;
        }
    }

    /// <summary>
    /// Compresses one input of whole blocks, block by block, from the key to its chaining value.
    /// </summary>
    /// <param name="kernel">
    /// The kernel that compresses several inputs at once; never <see cref="KernelKind.Auto" />. The blocks run on the
    /// kernel it compresses a single block with.
    /// </param>
    /// <param name="input">The first byte of the input.</param>
    /// <param name="blocks">The number of 64-byte blocks in the input.</param>
    /// <param name="key">The first of the eight key words.</param>
    /// <param name="counter">The input's counter.</param>
    /// <param name="flags">The flags on every block.</param>
    /// <param name="flagsStart">The flags added to the first block.</param>
    /// <param name="flagsEnd">The flags added to the last block.</param>
    /// <param name="output">The first of the 32 bytes that receive the chaining value.</param>
    private static void HashOne(KernelKind kernel, ref byte input, int blocks, ref uint key, ulong counter, uint flags, uint flagsStart, uint flagsEnd, ref byte output)
    {
        Span<uint> chainingValue = stackalloc uint[ChainingValueWords];
        MemoryMarshal.CreateReadOnlySpan(ref key, ChainingValueWords).CopyTo(chainingValue);
        KernelKind blockKernel = SingleBlockKernel(kernel);

        for (int block = 0; block < blocks; block++)
        {
            uint blockFlags = flags;
            if (block == 0) blockFlags |= flagsStart;
            if (block == blocks - 1) blockFlags |= flagsEnd;

            Compress(blockKernel, chainingValue, MemoryMarshal.CreateReadOnlySpan(ref Unsafe.Add(ref input, block * BlockBytes), BlockBytes), counter, BlockBytes, blockFlags);
        }

        StoreChainingValue(chainingValue, MemoryMarshal.CreateSpan(ref output, ChainingValueBytes));
        chainingValue.Clear();
    }
}
