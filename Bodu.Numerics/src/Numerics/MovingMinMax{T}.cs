// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MovingMinMax{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Globalization;
using System.Numerics;

namespace Bodu.Numerics;

/// <summary>
/// Maintains the minimum and maximum of the most recent N samples of a stream, updating in amortized O(1) as each new
/// sample displaces the oldest.
/// </summary>
/// <typeparam name="T">The numeric type of the samples.</typeparam>
/// <remarks>
/// <para>
/// This is the sliding-window extrema idiom: the window covers the last <see cref="Capacity" /> samples,
/// <see cref="Minimum" /> and <see cref="Maximum" /> always describe exactly those samples, and until the window fills
/// (<see cref="IsFull" />) they describe the samples received so far. Both extrema are tracked exactly in
/// <typeparamref name="T" /> using the classic pair of monotonic deques, so each sample is pushed and popped at most
/// once per deque — amortized O(1) per <see cref="Add" /> with no per-sample allocation.
/// </para>
/// <para>
/// Samples must be finite: NaN and infinite values are rejected by <see cref="Add" />. For the sum and mean over the
/// same window, pair this type with <see cref="MovingSum{T}" />; for whole-stream extrema use
/// <see cref="RunningStatistics{T}" />.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// var window = new MovingMinMax<int>(3);
/// window.Add(5);   // window: {5}          Min = 5, Max = 5
/// window.Add(1);   // window: {5, 1}       Min = 1, Max = 5
/// window.Add(4);   // window: {5, 1, 4}    Min = 1, Max = 5
/// window.Add(2);   // window: {1, 4, 2}    Min = 1, Max = 4
///]]>
/// </code>
/// </example>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class MovingMinMax<T>
    where T : INumber<T>
{
    /// <summary>The monotonic non-decreasing deque whose front is the current window minimum.</summary>
    private readonly Entry[] _minDeque;

    /// <summary>The monotonic non-increasing deque whose front is the current window maximum.</summary>
    private readonly Entry[] _maxDeque;

    /// <summary>The index of the front entry of the minimum deque.</summary>
    private int _minHead;

    /// <summary>The number of live entries in the minimum deque.</summary>
    private int _minCount;

    /// <summary>The index of the front entry of the maximum deque.</summary>
    private int _maxHead;

    /// <summary>The number of live entries in the maximum deque.</summary>
    private int _maxCount;

    /// <summary>The total number of samples added since construction or the last reset; also the next sequence number.</summary>
    private long _next;

    /// <summary>
    /// Initializes a new instance of the <see cref="MovingMinMax{T}" /> class with the given window size.
    /// </summary>
    /// <param name="capacity">The number of most-recent samples the window covers.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity" /> ≤ 0.</exception>
    public MovingMinMax(int capacity)
    {
        ThrowHelper.ThrowIfLessThanOrEqual(capacity, 0);

        _minDeque = new Entry[capacity];
        _maxDeque = new Entry[capacity];
    }

    /// <summary>
    /// Gets the number of most-recent samples the window covers once full.
    /// </summary>
    /// <value>The fixed window size supplied at construction.</value>
    public int Capacity =>
        _minDeque.Length;

    /// <summary>
    /// Gets the number of samples currently in the window.
    /// </summary>
    /// <value>The sample count, at most <see cref="Capacity" />.</value>
    public int Count =>
        (int)Math.Min(_next, _minDeque.Length);

    /// <summary>
    /// Gets a value indicating whether the window contains no samples.
    /// </summary>
    /// <value><see langword="true" /> when <see cref="Count" /> is zero; otherwise <see langword="false" />.</value>
    public bool IsEmpty =>
        _next == 0;

    /// <summary>
    /// Gets a value indicating whether the window has filled to <see cref="Capacity" /> and every new sample now
    /// displaces the oldest.
    /// </summary>
    /// <value><see langword="true" /> when <see cref="Count" /> equals <see cref="Capacity" />.</value>
    public bool IsFull =>
        _next >= _minDeque.Length;

    /// <summary>
    /// Gets the smallest sample currently in the window, tracked exactly in <typeparamref name="T" />.
    /// </summary>
    /// <value>The window minimum.</value>
    /// <exception cref="InvalidOperationException">The window is empty.</exception>
    public T Minimum
    {
        get
        {
            NumericsThrowHelper.ThrowIfNoSamples(_next);

            return _minDeque[_minHead].Value;
        }
    }

    /// <summary>
    /// Gets the largest sample currently in the window, tracked exactly in <typeparamref name="T" />.
    /// </summary>
    /// <value>The window maximum.</value>
    /// <exception cref="InvalidOperationException">The window is empty.</exception>
    public T Maximum
    {
        get
        {
            NumericsThrowHelper.ThrowIfNoSamples(_next);

            return _maxDeque[_maxHead].Value;
        }
    }

    /// <summary>
    /// Adds a sample to the window, displacing the oldest sample once the window is full.
    /// </summary>
    /// <param name="value">The sample to add. Must be finite.</param>
    /// <exception cref="ArgumentException"><paramref name="value" /> is NaN or infinite.</exception>
    public void Add(T value)
    {
        NumericsThrowHelper.ThrowIfNotFinite(value);

        var capacity = _minDeque.Length;
        var sequence = _next;
        _next++;

        // Expire entries that have slid out of the window (their sample is older than the last Capacity samples).
        while (_minCount > 0 && _minDeque[_minHead].Sequence <= sequence - capacity)
        {
            _minHead = (_minHead + 1) % capacity;
            _minCount--;
        }

        while (_maxCount > 0 && _maxDeque[_maxHead].Sequence <= sequence - capacity)
        {
            _maxHead = (_maxHead + 1) % capacity;
            _maxCount--;
        }

        // Pop dominated entries from the backs: an older sample that is >= (for min) or <= (for max) the new one can
        // never again be the window extremum, which is what keeps each deque monotonic and bounded by Capacity.
        while (_minCount > 0 && _minDeque[(_minHead + _minCount - 1) % capacity].Value >= value)
            _minCount--;

        while (_maxCount > 0 && _maxDeque[(_maxHead + _maxCount - 1) % capacity].Value <= value)
            _maxCount--;

        _minDeque[(_minHead + _minCount) % capacity] = new Entry(value, sequence);
        _minCount++;

        _maxDeque[(_maxHead + _maxCount) % capacity] = new Entry(value, sequence);
        _maxCount++;
    }

    /// <summary>
    /// Resets the window to the empty state, discarding every tracked sample.
    /// </summary>
    public void Reset()
    {
        Array.Clear(_minDeque);
        Array.Clear(_maxDeque);
        _minHead = 0;
        _minCount = 0;
        _maxHead = 0;
        _maxCount = 0;
        _next = 0;
    }

    /// <summary>
    /// Returns a culture-invariant summary of the window state for diagnostics.
    /// </summary>
    /// <returns>
    /// A string such as <c>"Capacity = 3, Count = 2, Min = 1, Max = 5"</c>, or <c>"Capacity = 3, Count = 0"</c> when
    /// empty.
    /// </returns>
    public override string ToString() =>
        _next == 0
            ? string.Format(CultureInfo.InvariantCulture, "Capacity = {0}, Count = 0", _minDeque.Length)
            : string.Format(
                CultureInfo.InvariantCulture,
                "Capacity = {0}, Count = {1}, Min = {2}, Max = {3}",
                _minDeque.Length,
                Count,
                Minimum,
                Maximum);

    /// <summary>
    /// A deque entry pairing a sample with its position in the overall stream, so expiry can be decided without
    /// buffering the window itself.
    /// </summary>
    /// <param name="Value">The sample value.</param>
    /// <param name="Sequence">The zero-based position of the sample in the overall stream.</param>
    private readonly record struct Entry(T Value, long Sequence);
}
