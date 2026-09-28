// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ScryptCore.MixOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

internal static partial class ScryptCore
{
    /// <summary>
    /// Describes how a derivation runs its ROMix units: how many threads it may use, from what unit size they are worth
    /// dividing among threads, which BlockMix kernel runs, and which pool supplies their memory. None of it changes the
    /// derived key.
    /// </summary>
    internal readonly struct MixOptions
    {
        /// <summary>The smallest <c>V</c>, in bytes, whose units are divided among threads by default: 1 MiB, as at <c>N = 1024</c> and <c>r = 8</c>. Below it, the cost of dispatching a unit to the thread pool outweighs the work it spreads.</summary>
        internal const long DefaultMinimumParallelUnitBytes = 1L << 20;

        /// <summary>The pool the options name, or <see langword="null" /> for the shared pool.</summary>
        private readonly NativeBufferPool? _pool;

        /// <summary>
        /// Initializes a new instance of the <see cref="MixOptions" /> struct.
        /// </summary>
        /// <param name="maxDegreeOfParallelism">
        /// The greatest number of threads the derivation may use, the calling thread included; <c>-1</c> for as many as
        /// there are processors.
        /// </param>
        /// <param name="minimumParallelUnitBytes">The smallest <c>V</c>, in bytes, whose units are divided.</param>
        /// <param name="kernel">
        /// The BlockMix kernel; <see cref="KernelKind.Auto" /> for the one dispatch selects. Any other kind must be one
        /// the processor supports.
        /// </param>
        /// <param name="pool">
        /// The pool every unit's workspace is taken from and returned to; <see langword="null" /> for the shared pool.
        /// </param>
        internal MixOptions(
            int maxDegreeOfParallelism,
            long minimumParallelUnitBytes = DefaultMinimumParallelUnitBytes,
            KernelKind kernel = KernelKind.Auto,
            NativeBufferPool? pool = null)
        {
            MaxDegreeOfParallelism = maxDegreeOfParallelism;
            MinimumParallelUnitBytes = minimumParallelUnitBytes;
            Kernel = kernel;
            _pool = pool;
        }

        /// <summary>
        /// Gets the greatest number of threads the derivation may use, the calling thread included, or <c>-1</c> for as
        /// many as there are processors.
        /// </summary>
        internal int MaxDegreeOfParallelism { get; }

        /// <summary>
        /// Gets the smallest <c>V</c>, in bytes, whose units are divided among threads.
        /// </summary>
        internal long MinimumParallelUnitBytes { get; }

        /// <summary>
        /// Gets the BlockMix kernel the units run, or <see cref="KernelKind.Auto" /> for the one dispatch selects.
        /// </summary>
        internal KernelKind Kernel { get; }

        /// <summary>
        /// Gets the pool every unit's workspace is taken from and returned to.
        /// </summary>
        internal NativeBufferPool Pool => _pool ?? NativeBufferPool.Shared;

        /// <summary>
        /// Returns the BlockMix kernel the units run: the one these options name, or the one dispatch selects.
        /// </summary>
        /// <returns>A kind other than <see cref="KernelKind.Auto" />.</returns>
        internal KernelKind ResolveKernel() =>
            Kernel == KernelKind.Auto ? SelectKernel() : Kernel;

        /// <summary>
        /// Returns the number of threads that run a derivation's units.
        /// </summary>
        /// <param name="parallelization">The number of units, <c>p</c>.</param>
        /// <param name="unitBytes">The size of one unit's <c>V</c>, <c>128 · N · r</c> bytes.</param>
        /// <returns>
        /// <c>1</c> when the units stay on the calling thread; otherwise the number of threads, the calling thread
        /// included, which never exceeds <paramref name="parallelization" />.
        /// </returns>
        /// <remarks>
        /// <para>
        /// The units stay on the calling thread when the bound is <c>1</c>, when there is a single unit, when a unit's
        /// <c>V</c> is smaller than <see cref="MinimumParallelUnitBytes" />, and on platforms without threads.
        /// </para>
        /// <para>
        /// Each unit that runs at once holds its own <c>V</c>, so the threads are also capped at the number of <c>V</c>s
        /// that fit in <see cref="ScryptParameters.MaxMemoryBytes" />, the ceiling on a single one: a derivation's
        /// working memory stays within that ceiling however high the bound, which is what keeps an untrusted encoded
        /// hash from multiplying it.
        /// </para>
        /// </remarks>
        internal int ResolveWorkers(int parallelization, long unitBytes)
        {
            if (MaxDegreeOfParallelism == 1 || parallelization == 1 || unitBytes < MinimumParallelUnitBytes)
                return 1;

            if (OperatingSystem.IsBrowser() || OperatingSystem.IsWasi())
                return 1;

            int bound = MaxDegreeOfParallelism == -1 ? Environment.ProcessorCount : MaxDegreeOfParallelism;
            long byMemory = Math.Max(1, ScryptParameters.MaxMemoryBytes / Math.Max(1, unitBytes));
            return (int)Math.Min(Math.Min(bound, parallelization), byMemory);
        }
    }
}
