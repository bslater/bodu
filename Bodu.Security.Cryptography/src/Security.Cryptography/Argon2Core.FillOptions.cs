// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2Core.FillOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

internal static partial class Argon2Core
{
    /// <summary>
    /// Describes how a derivation fills its matrix: how many threads it may use, from what segment length a slice is
    /// worth dividing among them, and which compression kernel runs. None of it changes the tag.
    /// </summary>
    internal readonly struct FillOptions
    {
        /// <summary>The shortest segment, in blocks, whose slice is divided among threads by default: 192 blocks, a lane of 768 KiB, as in a 3 MiB matrix at four lanes.</summary>
        /// <remarks>
        /// From 192-block segments, dividing each slice among threads cut the wall time on every machine measured,
        /// under .NET 8 and .NET 10, at up to twice the processor time: a Neoverse N2, an Apple M1, an Intel Xeon
        /// Platinum 8370C, an AMD EPYC 9V74, and a four-core Xeon virtual machine at 2.8 GHz. The first four gained
        /// from 64-block segments, but on the fifth one thread ran 64-block segments faster, and 128-block segments as
        /// fast under .NET 8.
        /// </remarks>
        internal const int DefaultMinimumParallelSegmentLength = 192;

        /// <summary>
        /// Initializes a new instance of the <see cref="FillOptions" /> struct.
        /// </summary>
        /// <param name="maxDegreeOfParallelism">
        /// The greatest number of threads the fill may use, the calling thread included; <c>-1</c> for as many as there
        /// are processors.
        /// </param>
        /// <param name="minimumParallelSegmentLength">The shortest segment, in blocks, whose slice is divided.</param>
        /// <param name="kernel">
        /// The compression kernel; <see cref="KernelKind.Auto" /> for the one dispatch selects. Any other kind must be
        /// one the processor supports.
        /// </param>
        internal FillOptions(
            int maxDegreeOfParallelism,
            int minimumParallelSegmentLength = DefaultMinimumParallelSegmentLength,
            KernelKind kernel = KernelKind.Auto)
        {
            MaxDegreeOfParallelism = maxDegreeOfParallelism;
            MinimumParallelSegmentLength = minimumParallelSegmentLength;
            Kernel = kernel;
        }

        /// <summary>
        /// Gets the greatest number of threads the fill may use, the calling thread included, or <c>-1</c> for as many
        /// as there are processors.
        /// </summary>
        internal int MaxDegreeOfParallelism { get; }

        /// <summary>
        /// Gets the shortest segment, in blocks, whose slice is divided among threads.
        /// </summary>
        internal int MinimumParallelSegmentLength { get; }

        /// <summary>
        /// Gets the compression kernel the fill uses, or <see cref="KernelKind.Auto" /> for the one dispatch selects.
        /// </summary>
        internal KernelKind Kernel { get; }

        /// <summary>
        /// Returns the compression kernel that fills the matrix: the one these options name, or the one dispatch
        /// selects.
        /// </summary>
        /// <returns>A kind other than <see cref="KernelKind.Auto" />.</returns>
        internal KernelKind ResolveKernel() =>
            Kernel == KernelKind.Auto ? SelectKernel() : Kernel;

        /// <summary>
        /// Returns the number of threads that fill each slice of a matrix of the specified shape.
        /// </summary>
        /// <param name="lanes">The number of lanes, <c>p</c>.</param>
        /// <param name="segmentLength">The number of blocks in each segment.</param>
        /// <returns>
        /// <c>1</c> when the fill stays on the calling thread; otherwise the number of threads, the calling thread
        /// included, which never exceeds <paramref name="lanes" />.
        /// </returns>
        /// <remarks>
        /// The fill stays on the calling thread when the bound is <c>1</c>, when there is a single lane, when segments
        /// are shorter than <see cref="MinimumParallelSegmentLength" />, and on platforms without threads.
        /// </remarks>
        internal int ResolveWorkers(int lanes, int segmentLength)
        {
            if (MaxDegreeOfParallelism == 1 || lanes == 1 || segmentLength < MinimumParallelSegmentLength)
                return 1;

            if (OperatingSystem.IsBrowser() || OperatingSystem.IsWasi())
                return 1;

            int bound = MaxDegreeOfParallelism == -1 ? Environment.ProcessorCount : MaxDegreeOfParallelism;
            return Math.Min(bound, lanes);
        }
    }
}
