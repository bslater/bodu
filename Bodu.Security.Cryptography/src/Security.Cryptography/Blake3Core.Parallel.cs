// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake3Core.Parallel.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Globalization;
using System.Numerics;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

internal static partial class Blake3Core
{
    /// <summary>The fewest chunks worth dividing among threads: four parts of 64 chunks, 256 KiB of input. Below it, waking other threads costs more than they save.</summary>
    internal const int MinimumParallelChunks = 4 * BatchChunks;

    /// <summary>
    /// Computes the chaining values of consecutive complete subtrees, dividing their chunks among up to the specified
    /// number of threads, with the kernel dispatch selects.
    /// </summary>
    /// <param name="input">The subtrees' chunks, one subtree after another.</param>
    /// <param name="subtreeChunks">The number of chunks in each subtree, each a power of two.</param>
    /// <param name="key">The eight-word key.</param>
    /// <param name="counter">The counter of the first subtree's first chunk.</param>
    /// <param name="flags">The mode's flags.</param>
    /// <param name="maxDegreeOfParallelism">
    /// The greatest number of threads to use, the calling thread included; <c>-1</c> for up to one per processor.
    /// </param>
    /// <param name="chainingValues">The subtrees' encoded chaining values, 32 bytes each, in order.</param>
    /// <exception cref="ArgumentException">
    /// A count in <paramref name="subtreeChunks" /> is not a power of two, or the counts do not add up to the length of
    /// <paramref name="input" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="key" /> holds fewer than eight words, <paramref name="chainingValues" /> fewer than 32 bytes per
    /// subtree, or <paramref name="maxDegreeOfParallelism" /> is zero or less than <c>-1</c>.
    /// </exception>
    internal static void CompressSubtrees(ReadOnlySpan<byte> input, ReadOnlySpan<int> subtreeChunks, ReadOnlySpan<uint> key, ulong counter, uint flags, int maxDegreeOfParallelism, Span<byte> chainingValues) =>
        CompressSubtrees(SelectKernel(), input, subtreeChunks, key, counter, flags, maxDegreeOfParallelism, chainingValues);

    /// <summary>
    /// Computes the chaining values of consecutive complete subtrees, dividing their chunks among up to the specified
    /// number of threads, with the specified kernel.
    /// </summary>
    /// <param name="kernel">
    /// The kernel; <see cref="KernelKind.Auto" /> for the one dispatch selects. Any other kind must be one the
    /// processor supports.
    /// </param>
    /// <param name="input">The subtrees' chunks, one subtree after another.</param>
    /// <param name="subtreeChunks">The number of chunks in each subtree, each a power of two.</param>
    /// <param name="key">The eight-word key.</param>
    /// <param name="counter">The counter of the first subtree's first chunk.</param>
    /// <param name="flags">The mode's flags.</param>
    /// <param name="maxDegreeOfParallelism">
    /// The greatest number of threads to use, the calling thread included; <c>-1</c> for up to one per processor.
    /// </param>
    /// <param name="chainingValues">The subtrees' encoded chaining values, 32 bytes each, in order.</param>
    /// <exception cref="ArgumentException">
    /// A count in <paramref name="subtreeChunks" /> is not a power of two, or the counts do not add up to the length of
    /// <paramref name="input" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="key" /> holds fewer than eight words, <paramref name="chainingValues" /> fewer than 32 bytes per
    /// subtree, or <paramref name="maxDegreeOfParallelism" /> is zero or less than <c>-1</c>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Each subtree is divided into parts of at most 64 chunks — the batch one thread compresses on its stack — and the
    /// parts of every subtree are claimed from one shared counter by the calling thread and up to
    /// <paramref name="maxDegreeOfParallelism" /> − 1 workers, so one call pays for one hand-off however many subtrees
    /// it holds. Each subtree's parts are then joined, level by level, on the calling thread. Every chaining value is
    /// the one
    /// <see cref="CompressSubtree(KernelKind, ReadOnlySpan{byte}, ReadOnlySpan{uint}, ulong, uint, Span{uint})" />
    /// computes, whatever the bound.
    /// </para>
    /// <para>
    /// The input stays on the calling thread, and one subtree after another, when the bound is <c>1</c>, when the
    /// subtrees hold fewer than <see cref="MinimumParallelChunks" /> chunks in all, or when they make fewer than two
    /// parts. The calling thread claims parts too and runs them all if no worker arrives, so a starved thread pool
    /// slows the call down but never stalls it, and a fault in a part surfaces as itself rather than wrapped in an
    /// <see cref="AggregateException" />, once every worker has stopped.
    /// </para>
    /// </remarks>
    internal static void CompressSubtrees(KernelKind kernel, ReadOnlySpan<byte> input, ReadOnlySpan<int> subtreeChunks, ReadOnlySpan<uint> key, ulong counter, uint flags, int maxDegreeOfParallelism, Span<byte> chainingValues)
    {
        long totalChunks = 0;
        foreach (int chunks in subtreeChunks)
        {
            if (chunks <= 0 || !BitOperations.IsPow2(chunks)) throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Arg_Invalid_Blake3SubtreeChunkCount, ChunkBytes), nameof(subtreeChunks));
            totalChunks += chunks;
        }

        if (totalChunks * ChunkBytes != input.Length) throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Arg_Invalid_Blake3SubtreeChunkCount, ChunkBytes), nameof(input));
        ThrowHelper.ThrowIfLessThan(key.Length, ChainingValueWords, nameof(key));
        ThrowHelper.ThrowIfLessThan(chainingValues.Length, subtreeChunks.Length * ChainingValueBytes, nameof(chainingValues));
        CryptographyThrowHelper.ThrowIfDegreeOfParallelismInvalid(maxDegreeOfParallelism);

        if (kernel == KernelKind.Auto)
            kernel = SelectKernel();

        int parts = 0;
        foreach (int chunks in subtreeChunks)
            parts += PartCount(chunks);

        int workers = Math.Min(maxDegreeOfParallelism == -1 ? Environment.ProcessorCount : maxDegreeOfParallelism, parts);
        if (workers < 2 || totalChunks < MinimumParallelChunks)
        {
            CompressSubtreesInOrder(kernel, input, subtreeChunks, key, counter, flags, chainingValues);
            return;
        }

        CompressSubtreesInParallel(kernel, input, subtreeChunks, key, counter, flags, parts, workers, chainingValues);
    }

    /// <summary>
    /// Computes the chaining values of consecutive complete subtrees with their parts divided among threads.
    /// </summary>
    /// <param name="kernel">The kernel; never <see cref="KernelKind.Auto" />.</param>
    /// <param name="input">The subtrees' chunks, one subtree after another.</param>
    /// <param name="subtreeChunks">The number of chunks in each subtree, each a power of two.</param>
    /// <param name="key">The eight-word key.</param>
    /// <param name="counter">The counter of the first subtree's first chunk.</param>
    /// <param name="flags">The mode's flags.</param>
    /// <param name="parts">The number of parts the subtrees divide into.</param>
    /// <param name="workers">The number of threads, the calling thread included; at least two.</param>
    /// <param name="chainingValues">The subtrees' encoded chaining values, 32 bytes each, in order.</param>
    /// <remarks>
    /// It is a method of its own so that the state the workers share is allocated only when the input is divided: the
    /// compiler allocates a lambda's captures on entry to the method that declares them.
    /// </remarks>
    private static unsafe void CompressSubtreesInParallel(KernelKind kernel, ReadOnlySpan<byte> input, ReadOnlySpan<int> subtreeChunks, ReadOnlySpan<uint> key, ulong counter, uint flags, int parts, int workers, Span<byte> chainingValues)
    {
        // Where each subtree's parts, chunks and bytes begin, for the workers to find a claimed part's subtree.
        int[] firstPart = new int[subtreeChunks.Length + 1];
        for (int subtree = 0; subtree < subtreeChunks.Length; subtree++)
            firstPart[subtree + 1] = firstPart[subtree] + PartCount(subtreeChunks[subtree]);

        int[] sizes = subtreeChunks.ToArray();
        uint[] keyWords = key[..ChainingValueWords].ToArray();
        byte[] partValues = ArrayPool<byte>.Shared.Rent(parts * ChainingValueBytes);

        try
        {
            fixed (byte* pinned = input)
            {
                nint address = (nint)pinned;
                int nextPart = -1;
                var options = new ParallelOptions { MaxDegreeOfParallelism = workers, TaskScheduler = TaskScheduler.Default };

                try
                {
                    Parallel.For(0, workers, options, _ =>
                    {
                        Span<uint> partValue = stackalloc uint[ChainingValueWords];
                        int subtree = 0;
                        long subtreeStart = 0;

                        for (int part = Interlocked.Increment(ref nextPart); part < parts; part = Interlocked.Increment(ref nextPart))
                        {
                            // Parts are claimed in order, so a thread's next subtree is never before its last.
                            while (part >= firstPart[subtree + 1])
                                subtreeStart += sizes[subtree++];

                            int partChunks = sizes[subtree] / PartCount(sizes[subtree]);
                            long start = subtreeStart + ((long)(part - firstPart[subtree]) * partChunks);
                            var partInput = new ReadOnlySpan<byte>((byte*)address + (start * ChunkBytes), partChunks * ChunkBytes);

                            CompressSubtreeCore(kernel, partInput, keyWords, counter + (ulong)start, flags, partValue);
                            StoreChainingValue(partValue, partValues.AsSpan(part * ChainingValueBytes, ChainingValueBytes));
                        }

                        partValue.Clear();
                    });
                }
                catch (AggregateException ex) when (ex.InnerExceptions.Count > 0)
                {
                    ExceptionDispatchInfo.Capture(ex.InnerExceptions[0]).Throw();
                }
            }

            // Join each subtree's parts, level by level in place, as a batch joins its chunks.
            for (int subtree = 0; subtree < sizes.Length; subtree++)
            {
                Span<byte> values = partValues.AsSpan(firstPart[subtree] * ChainingValueBytes, (firstPart[subtree + 1] - firstPart[subtree]) * ChainingValueBytes);
                for (int count = values.Length / ChainingValueBytes; count > 1; count /= 2)
                    CompressParents(kernel, values[..(count * ChainingValueBytes)], keyWords, flags, values);

                values[..ChainingValueBytes].CopyTo(chainingValues[(subtree * ChainingValueBytes)..]);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(partValues.AsSpan(0, parts * ChainingValueBytes));
            ArrayPool<byte>.Shared.Return(partValues);
            CryptographicOperations.ZeroMemory(MemoryMarshal.AsBytes(keyWords.AsSpan()));
        }
    }

    /// <summary>
    /// Computes the chaining values of consecutive complete subtrees one after another on the calling thread.
    /// </summary>
    /// <param name="kernel">The kernel; never <see cref="KernelKind.Auto" />.</param>
    /// <param name="input">The subtrees' chunks, one subtree after another.</param>
    /// <param name="subtreeChunks">The number of chunks in each subtree, each a power of two.</param>
    /// <param name="key">The eight-word key.</param>
    /// <param name="counter">The counter of the first subtree's first chunk.</param>
    /// <param name="flags">The mode's flags.</param>
    /// <param name="chainingValues">The subtrees' encoded chaining values, 32 bytes each, in order.</param>
    private static void CompressSubtreesInOrder(KernelKind kernel, ReadOnlySpan<byte> input, ReadOnlySpan<int> subtreeChunks, ReadOnlySpan<uint> key, ulong counter, uint flags, Span<byte> chainingValues)
    {
        Span<uint> chainingValue = stackalloc uint[ChainingValueWords];
        int offset = 0;

        for (int subtree = 0; subtree < subtreeChunks.Length; subtree++)
        {
            int length = subtreeChunks[subtree] * ChunkBytes;
            CompressSubtreeCore(kernel, input.Slice(offset, length), key, counter, flags, chainingValue);
            StoreChainingValue(chainingValue, chainingValues[(subtree * ChainingValueBytes)..]);

            counter += (ulong)subtreeChunks[subtree];
            offset += length;
        }

        chainingValue.Clear();
    }

    /// <summary>
    /// Returns the number of parts a subtree divides into: one per 64 chunks, and one for a smaller subtree.
    /// </summary>
    /// <param name="chunks">The number of chunks in the subtree, a power of two.</param>
    /// <returns>The number of parts, a power of two.</returns>
    private static int PartCount(int chunks) =>
        Math.Max(1, chunks / BatchChunks);
}
