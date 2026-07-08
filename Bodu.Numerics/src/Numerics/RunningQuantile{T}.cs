// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RunningQuantile{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Bodu.Numerics;

/// <summary>
/// Estimates a single quantile of a sample stream in one forward pass and constant space, using the P² algorithm of
/// Jain and Chlamtac (1985).
/// </summary>
/// <typeparam name="T">The numeric type of the samples.</typeparam>
/// <remarks>
/// <para>
/// The estimator maintains five markers that track the minimum, the maximum, the target quantile, and the two quantiles
/// halfway between it and the extremes, adjusting the middle markers with piecewise-parabolic (hence "P²")
/// interpolation as samples arrive. Each <see cref="Add" /> is O(1) and the state is a fixed scalar block — the samples
/// themselves are never stored. Samples are widened to <see cref="double" /> with
/// <see cref="double.CreateChecked{TOther}(TOther)" />, so <see cref="Estimate" /> is always a floating-point estimate.
/// </para>
/// <para>
/// The first five samples are held exactly; while fewer than five have been observed <see cref="Estimate" /> returns
/// the linearly interpolated empirical quantile, and from the fifth sample onward the P² markers take over. The
/// estimate is an approximation whose accuracy improves with stream length; for exact quantiles over small data, sort
/// and index instead.
/// </para>
/// <para>
/// This is a <b>mutable value type</b>. Store it in a mutable field or local and pass it by <see langword="ref" />; do
/// not capture it in a lambda or iterator that expects reference semantics — each copy accumulates independently from
/// the point of the copy, which is also the supported way to checkpoint. The <see langword="default" /> value is a
/// valid empty <em>median</em> estimator; use the constructor for any other probability.
/// </para>
/// <para>
/// Unlike <see cref="RunningStatistics{T}" />, two P² estimators cannot be merged — the marker states of two partitions
/// do not compose. Partition-and-combine workloads should carry the mergeable moments in
/// <see cref="RunningStatistics{T}" /> and reserve this type for single-stream use. Samples must be finite: NaN and
/// infinite values are rejected by <see cref="Add" />.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// var median = RunningQuantile<double>.CreateMedian();
/// foreach (var sample in new[] { 0.02, 0.15, 0.74, 3.39, 0.83, 22.37, 10.15, 15.43, 38.62, 15.92,
///                                34.60, 10.28, 1.47, 0.40, 0.05, 11.39, 0.27, 0.42, 0.09, 11.37 })
///     median.Add(sample);
///
/// median.Estimate;   // ≈ 4.44 (exact sorted median of this stream: 2.43)
///
/// var p95 = new RunningQuantile<int>(0.95);
///]]>
/// </code>
/// </example>
[DebuggerDisplay("{ToString(),nq}")]
public partial struct RunningQuantile<T>
    where T : INumber<T>
{
    /// <summary>The target probability stored offset by −0.5, so the all-zero <see langword="default" /> instance is a valid median estimator rather than an unusable zero-probability one.</summary>
    private double _probabilityOffset;

    /// <summary>The number of samples accumulated so far.</summary>
    private long _count;

    /// <summary>The five marker heights q₀…q₄. During warm-up (fewer than five samples) the first <see cref="_count" /> slots hold the samples in ascending order; afterwards they are the P² marker estimates.</summary>
    private MarkerHeights _heights;

    /// <summary>The five actual marker positions n₀…n₄ (0-based sample ranks); meaningful from the fifth sample.</summary>
    private MarkerPositions _positions;

    /// <summary>The five desired marker positions n′₀…n′₄; meaningful from the fifth sample.</summary>
    private DesiredPositions _desired;

    /// <summary>
    /// Initializes a new instance of the <see cref="RunningQuantile{T}" /> struct targeting the given quantile.
    /// </summary>
    /// <param name="probability">
    /// The quantile to estimate, strictly between 0 and 1 (for example 0.5 for the median).
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="probability" /> is not greater than 0 and less than 1 (NaN is rejected).
    /// </exception>
    public RunningQuantile(double probability)
    {
        if (!(probability > 0.0 && probability < 1.0)) throw new ArgumentOutOfRangeException(nameof(probability), NumericsResourceStrings.Arg_OutOfRange_QuantileProbability);

        _probabilityOffset = probability - 0.5;
    }

    /// <summary>
    /// Creates an empty estimator targeting the median (probability 0.5).
    /// </summary>
    /// <returns>An empty median estimator, equivalent to <c>new RunningQuantile&lt;T&gt;(0.5)</c>.</returns>
    public static RunningQuantile<T> CreateMedian() =>
        default;

    /// <summary>
    /// Adds a sample to the estimator, updating the marker state in O(1).
    /// </summary>
    /// <param name="value">The sample to accumulate. Must be finite.</param>
    /// <exception cref="ArgumentException"><paramref name="value" /> is NaN or infinite.</exception>
    /// <exception cref="OverflowException">
    /// <paramref name="value" /> is finite but outside the range representable by <see cref="double" /> (possible only
    /// for unbounded integer sample types such as <see cref="BigInteger" />).
    /// </exception>
    public void Add(T value)
    {
        NumericsThrowHelper.ThrowIfNotFinite(value);

        var x = double.CreateChecked(value);
        NumericsThrowHelper.ThrowIfWideningOverflowed(x);

        if (_count < 5)
        {
            AddWarmUp(x);
            return;
        }

        AddSteadyState(x);
    }

    /// <summary>
    /// Resets the estimator to the empty state, discarding all samples but preserving <see cref="Probability" />.
    /// </summary>
    public void Reset()
    {
        var probabilityOffset = _probabilityOffset;

        this = default;
        _probabilityOffset = probabilityOffset;
    }

    /// <summary>
    /// Returns a culture-invariant summary of the estimator state for diagnostics.
    /// </summary>
    /// <returns>
    /// A string such as <c>"p = 0.5, Count = 20, Estimate = 4.44"</c>, or <c>"p = 0.5, Count = 0"</c> when empty.
    /// </returns>
    public override readonly string ToString() =>
        _count == 0
            ? string.Format(CultureInfo.InvariantCulture, "p = {0}, Count = 0", Probability)
            : string.Format(CultureInfo.InvariantCulture, "p = {0}, Count = {1}, Estimate = {2}", Probability, _count, Estimate);

    /// <summary>
    /// Absorbs one of the first five samples, keeping the held samples in ascending order and switching to marker mode
    /// when the fifth arrives.
    /// </summary>
    /// <param name="x">The widened sample value.</param>
    private void AddWarmUp(double x)
    {
        var index = (int)_count;

        while (index > 0 && _heights[index - 1] > x)
        {
            _heights[index] = _heights[index - 1];
            index--;
        }

        _heights[index] = x;
        _count++;

        if (_count == 5)
            InitializeMarkers();
    }

    /// <summary>
    /// Seeds the marker positions once the first five samples are held in sorted order: actual positions are the ranks
    /// 0…4 and desired positions the P² targets for <see cref="Probability" />.
    /// </summary>
    private void InitializeMarkers()
    {
        var p = Probability;

        for (var i = 0; i < 5; i++)
            _positions[i] = i;

        _desired[0] = 0.0;
        _desired[1] = 2.0 * p;
        _desired[2] = 4.0 * p;
        _desired[3] = 2.0 + (2.0 * p);
        _desired[4] = 4.0;
    }

    /// <summary>
    /// Performs one steady-state P² update: locates the sample's cell, shifts the affected marker positions, advances
    /// every desired position by its per-observation increment, and re-centres the three middle markers.
    /// </summary>
    /// <param name="x">The widened sample value.</param>
    private void AddSteadyState(double x)
    {
        // Step 1 — find the cell k with q[k] <= x < q[k+1], clamping a new extreme into the outer marker.
        int k;
        if (x < _heights[0])
        {
            _heights[0] = x;
            k = 0;
        }
        else if (x >= _heights[4])
        {
            _heights[4] = x;
            k = 3;
        }
        else
        {
            k = 0;
            while (k < 3 && x >= _heights[k + 1])
                k++;
        }

        // Step 2 — shift the actual positions above the cell, and advance every desired position.
        for (var i = k + 1; i < 5; i++)
            _positions[i]++;

        var p = Probability;
        _desired[1] += p / 2.0;
        _desired[2] += p;
        _desired[3] += (1.0 + p) / 2.0;
        _desired[4] += 1.0;

        // Step 3 — re-centre the middle markers toward their desired positions, one rank at a time.
        for (var i = 1; i <= 3; i++)
        {
            var offset = _desired[i] - _positions[i];

            if ((offset >= 1.0 && _positions[i + 1] - _positions[i] > 1) ||
                (offset <= -1.0 && _positions[i - 1] - _positions[i] < -1))
            {
                var direction = Math.Sign(offset);
                var candidate = ParabolicEstimate(i, direction);

                // The parabolic candidate is accepted only while it preserves marker ordering; otherwise fall back
                // to linear interpolation toward the neighbouring marker, per the published algorithm.
                _heights[i] = _heights[i - 1] < candidate && candidate < _heights[i + 1]
                    ? candidate
                    : LinearEstimate(i, direction);

                _positions[i] += direction;
            }
        }

        _count++;
    }

    /// <summary>
    /// Computes the piecewise-parabolic (P²) height for moving marker <paramref name="i" /> one rank in
    /// <paramref name="direction" />.
    /// </summary>
    /// <param name="i">The middle-marker index (1, 2, or 3).</param>
    /// <param name="direction">The rank shift, −1 or +1.</param>
    /// <returns>The parabolic height candidate.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly double ParabolicEstimate(int i, int direction)
    {
        double d = direction;
        double below = _positions[i] - _positions[i - 1];
        double above = _positions[i + 1] - _positions[i];
        double span = _positions[i + 1] - _positions[i - 1];

        return _heights[i] + (d / span * (((below + d) * (_heights[i + 1] - _heights[i]) / above)
            + ((above - d) * (_heights[i] - _heights[i - 1]) / below)));
    }

    /// <summary>
    /// Computes the linear-interpolation fallback height for moving marker <paramref name="i" /> one rank in
    /// <paramref name="direction" />.
    /// </summary>
    /// <param name="i">The middle-marker index (1, 2, or 3).</param>
    /// <param name="direction">The rank shift, −1 or +1.</param>
    /// <returns>The linearly interpolated height.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly double LinearEstimate(int i, int direction) =>
        _heights[i] + (direction * (_heights[i + direction] - _heights[i])
            / (_positions[i + direction] - _positions[i]));

    /// <summary>
    /// Inline storage for the five marker heights, kept in the struct so copies snapshot the full estimator state.
    /// </summary>
    [InlineArray(5)]
    private struct MarkerHeights
    {
        /// <summary>The first height of the inline block; the <see cref="InlineArrayAttribute" /> expands it to five.</summary>
        private double _element0;
    }

    /// <summary>
    /// Inline storage for the five actual marker positions.
    /// </summary>
    [InlineArray(5)]
    private struct MarkerPositions
    {
        /// <summary>The first position of the inline block; the <see cref="InlineArrayAttribute" /> expands it to five.</summary>
        private long _element0;
    }

    /// <summary>
    /// Inline storage for the five desired marker positions.
    /// </summary>
    [InlineArray(5)]
    private struct DesiredPositions
    {
        /// <summary>The first position of the inline block; the <see cref="InlineArrayAttribute" /> expands it to five.</summary>
        private double _element0;
    }
}
