// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StripedLongCounter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.InteropServices;

namespace Bodu.Collections.Generic.Concurrent;

/// <summary>
/// Provides a contention-resistant monotonic counter that stripes increments across cache-line-padded cells indexed by
/// the calling thread's current processor.
/// </summary>
/// <remarks>
/// <para>
/// A single shared <see cref="Interlocked" /> counter on a hot read path bounces its cache line between every
/// incrementing core. This counter instead gives each processor (modulo the stripe count) its own padded cell, so
/// concurrent increments from different cores proceed without inter-core traffic; <see cref="Sum" /> aggregates the
/// cells on demand.
/// </para>
/// <para>
/// Increments are exact — a thread that migrates between processors mid-call still lands its increment in exactly one
/// cell via <see cref="Interlocked.Increment(ref long)" />. <see cref="Sum" /> reads the cells without a global
/// snapshot, so under concurrent increments the returned total may not correspond to any single instant; it is exact
/// once incrementing has quiesced.
/// </para>
/// <para>
/// This type is a standalone class rather than a nested member of its consumer because
/// <see cref="LayoutKind.Explicit" /> — required for the cache-line padding — is not permitted on types nested inside a
/// generic type.
/// </para>
/// </remarks>
internal sealed class StripedLongCounter
{
    /// <summary>The upper bound applied to the stripe count so the cell array stays small on high-core machines.</summary>
    private const int MaxStripes = 32;

    /// <summary>The padded cells; increments target the cell for the caller's current processor.</summary>
    private readonly PaddedLong[] _cells;

    /// <summary>The bitmask mapping a processor id onto a cell index; the cell count is a power of two.</summary>
    private readonly int _mask;

    /// <summary>
    /// Initializes a new instance of the <see cref="StripedLongCounter" /> class with one padded cell per processor,
    /// clamped and rounded up to a power of two.
    /// </summary>
    internal StripedLongCounter()
    {
        int stripes = RoundUpToPowerOfTwo(Math.Clamp(Environment.ProcessorCount, 1, MaxStripes));

        _cells = new PaddedLong[stripes];
        _mask = stripes - 1;
    }

    /// <summary>
    /// Adds one to the counter, targeting the cell for the calling thread's current processor.
    /// </summary>
    internal void Increment() =>
        Interlocked.Increment(ref _cells[Thread.GetCurrentProcessorId() & _mask].Value);

    /// <summary>
    /// Resets every cell to zero.
    /// </summary>
    /// <remarks>
    /// Increments racing the reset may land before or after their cell is zeroed; the counter is exactly zero only once
    /// incrementing has quiesced.
    /// </remarks>
    internal void Reset()
    {
        for (int i = 0; i < _cells.Length; i++)
            Interlocked.Exchange(ref _cells[i].Value, 0);
    }

    /// <summary>
    /// Returns the sum of every cell.
    /// </summary>
    /// <returns>The aggregated count. Exact when no increments are in flight.</returns>
    internal long Sum()
    {
        long sum = 0;
        for (int i = 0; i < _cells.Length; i++)
            sum += Volatile.Read(ref _cells[i].Value);

        return sum;
    }

    /// <summary>
    /// Rounds a positive value up to the next power of two so processor ids can be mapped onto cells with a mask
    /// instead of a modulo.
    /// </summary>
    /// <param name="value">The positive value to round.</param>
    /// <returns>The smallest power of two greater than or equal to <paramref name="value" />.</returns>
    private static int RoundUpToPowerOfTwo(int value) =>
        (int)System.Numerics.BitOperations.RoundUpToPowerOf2((uint)value);

    /// <summary>
    /// Holds one counter cell padded to occupy its own cache line, preventing false sharing between cells that sit
    /// adjacently in the array.
    /// </summary>
    /// <remarks>
    /// The value sits at offset 64 inside a 128-byte struct, so it is isolated from both the preceding and following
    /// cells regardless of where the array's first element lands relative to a cache-line boundary.
    /// </remarks>
    [StructLayout(LayoutKind.Explicit, Size = 128)]
    private struct PaddedLong
    {
        /// <summary>The counter value for one stripe.</summary>
        [FieldOffset(64)]
        public long Value;
    }
}
