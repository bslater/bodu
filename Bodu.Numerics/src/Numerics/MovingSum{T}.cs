// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MovingSum{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Globalization;
using System.Numerics;

namespace Bodu.Numerics;

/// <summary>
/// Maintains the sum and mean of the most recent N samples of a stream, updating in amortized O(1) as each new sample
/// displaces the oldest.
/// </summary>
/// <typeparam name="T">The numeric type of the samples.</typeparam>
/// <remarks>
/// <para>
/// This is the finance / telemetry "rolling window" idiom: the window holds the last <see cref="Capacity" /> samples,
/// <see cref="Sum" /> and <see cref="Mean" /> always describe exactly those samples, and until the window fills (<see cref="IsFull" />)
/// they describe the samples received so far. The sum is maintained in <typeparamref name="T" /> — exact for integer,
/// <see cref="decimal" />, and <see cref="BigInteger" /> samples — while <see cref="Mean" /> is computed in
/// <see cref="double" />. Addition follows <typeparamref name="T" />'s own overflow semantics (unchecked wrap-around
/// for the fixed-width binary integers).
/// </para>
/// <para>
/// For floating-point samples the subtract-on-evict update accumulates rounding drift, so the sum is transparently
/// recomputed from the buffered window after every full window turnover, bounding the drift to one window's worth of
/// rounding. Samples must be finite: NaN and infinite values are rejected by <see cref="Add" />. For min/max over the
/// same window, pair this type with <see cref="MovingMinMax{T}" />; for whole-stream moments use
/// <see cref="RunningStatistics{T}" />.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// var window = new MovingSum<double>(3);
/// window.Add(1.0);   // window: {1}          Sum = 1
/// window.Add(2.0);   // window: {1, 2}       Sum = 3
/// window.Add(3.0);   // window: {1, 2, 3}    Sum = 6
/// window.Add(4.0);   // window: {2, 3, 4}    Sum = 9, Mean = 3
///]]>
/// </code>
/// </example>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class MovingSum<T>
    where T : INumber<T>
{
    /// <summary>The ring buffer holding the current window contents.</summary>
    private readonly T[] _buffer;

    /// <summary>The index of the oldest sample in <see cref="_buffer" />.</summary>
    private int _head;

    /// <summary>The number of samples currently in the window.</summary>
    private int _count;

    /// <summary>The running sum of the window contents, maintained incrementally in <typeparamref name="T" />.</summary>
    private T _sum;

    /// <summary>The number of evictions since the sum was last recomputed exactly from the buffer; at <see cref="Capacity" /> evictions the sum is rebuilt to bound floating-point drift.</summary>
    private int _evictionsSinceRebuild;

    /// <summary>
    /// Initializes a new instance of the <see cref="MovingSum{T}" /> class with the given window size.
    /// </summary>
    /// <param name="capacity">The number of most-recent samples the window covers.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity" /> ≤ 0.</exception>
    public MovingSum(int capacity)
    {
        ThrowHelper.ThrowIfLessThanOrEqual(capacity, 0);

        _buffer = new T[capacity];
        _sum = T.Zero;
    }

    /// <summary>
    /// Gets the number of most-recent samples the window covers once full.
    /// </summary>
    /// <value>The fixed window size supplied at construction.</value>
    public int Capacity =>
        _buffer.Length;

    /// <summary>
    /// Gets the number of samples currently in the window.
    /// </summary>
    /// <value>The sample count, at most <see cref="Capacity" />.</value>
    public int Count =>
        _count;

    /// <summary>
    /// Gets a value indicating whether the window contains no samples.
    /// </summary>
    /// <value><see langword="true" /> when <see cref="Count" /> is zero; otherwise <see langword="false" />.</value>
    public bool IsEmpty =>
        _count == 0;

    /// <summary>
    /// Gets a value indicating whether the window has filled to <see cref="Capacity" /> and every new sample now evicts
    /// the oldest.
    /// </summary>
    /// <value><see langword="true" /> when <see cref="Count" /> equals <see cref="Capacity" />.</value>
    public bool IsFull =>
        _count == _buffer.Length;

    /// <summary>
    /// Gets the sum of the samples currently in the window.
    /// </summary>
    /// <value>The window sum in <typeparamref name="T" />; <c>T.Zero</c> when the window is empty.</value>
    public T Sum =>
        _sum;

    /// <summary>
    /// Gets the arithmetic mean of the samples currently in the window.
    /// </summary>
    /// <value>The window mean, computed in <see cref="double" />.</value>
    /// <exception cref="InvalidOperationException">The window is empty.</exception>
    public double Mean
    {
        get
        {
            NumericsThrowHelper.ThrowIfNoSamples(_count);

            return double.CreateChecked(_sum) / _count;
        }
    }

    /// <summary>
    /// Adds a sample to the window, evicting the oldest sample once the window is full.
    /// </summary>
    /// <param name="value">The sample to add. Must be finite.</param>
    /// <exception cref="ArgumentException"><paramref name="value" /> is NaN or infinite.</exception>
    public void Add(T value)
    {
        NumericsThrowHelper.ThrowIfNotFinite(value);

        if (_count < _buffer.Length)
        {
            _buffer[(_head + _count) % _buffer.Length] = value;
            _count++;
            _sum += value;
            return;
        }

        _sum -= _buffer[_head];
        _buffer[_head] = value;
        _head = (_head + 1) % _buffer.Length;
        _sum += value;

        // One full window turnover of subtract-on-evict updates is the drift budget for floating-point T; rebuild
        // the sum exactly from the buffered samples before starting the next turnover. Exact types recompute to the
        // identical value, so the rebuild is a no-op for them beyond its O(Capacity) cost.
        _evictionsSinceRebuild++;
        if (_evictionsSinceRebuild == _buffer.Length)
        {
            _evictionsSinceRebuild = 0;
            var rebuilt = T.Zero;
            foreach (var sample in _buffer)
                rebuilt += sample;

            _sum = rebuilt;
        }
    }

    /// <summary>
    /// Resets the window to the empty state, discarding every buffered sample.
    /// </summary>
    public void Reset()
    {
        Array.Clear(_buffer);
        _head = 0;
        _count = 0;
        _sum = T.Zero;
        _evictionsSinceRebuild = 0;
    }

    /// <summary>
    /// Returns a culture-invariant summary of the window state for diagnostics.
    /// </summary>
    /// <returns>A string such as <c>"Capacity = 3, Count = 2, Sum = 7"</c>.</returns>
    public override string ToString() =>
        string.Format(CultureInfo.InvariantCulture, "Capacity = {0}, Count = {1}, Sum = {2}", _buffer.Length, _count, _sum);
}
