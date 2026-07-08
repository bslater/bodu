// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RunningStatistics{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Globalization;
using System.Numerics;

namespace Bodu.Numerics;

/// <summary>
/// Accumulates the count, minimum, maximum, mean, and variance of a sample stream in a single forward pass, using
/// Welford's numerically stable online algorithm.
/// </summary>
/// <typeparam name="T">The numeric type of the samples.</typeparam>
/// <remarks>
/// <para>
/// Samples are absorbed one at a time through <see cref="Add" /> in O(1) time and O(1) space — the accumulator never
/// stores the samples themselves. The minimum and maximum are tracked exactly in <typeparamref name="T" />; the mean
/// and the variance moments are accumulated in <see cref="double" /> (each sample is widened with
/// <see cref="double.CreateChecked{TOther}(TOther)" />), so those results are floating-point estimates regardless of
/// the sample type. Welford's recurrence avoids the catastrophic cancellation of the naive sum-of-squares formulation,
/// keeping the variance accurate even when the samples are large and closely clustered.
/// </para>
/// <para>
/// This is a <b>mutable value type</b>. Store it in a mutable field or local and pass it by <see langword="ref" />; do
/// not capture it in a lambda or iterator that expects reference semantics — each copy accumulates independently from
/// the point of the copy. That copy behaviour is also the supported way to checkpoint: assigning the accumulator to
/// another variable snapshots its state. The <see langword="default" /> value is the valid empty accumulator.
/// </para>
/// <para>
/// Two independently filled accumulators merge losslessly with <see cref="Combine" />, so a stream can be partitioned,
/// accumulated in parallel, and recombined. Samples must be finite: NaN and infinite values are rejected by
/// <see cref="Add" /> because they would poison every subsequent moment irrecoverably. For a streaming quantile
/// estimate, pair this type with <see cref="RunningQuantile{T}" />.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// var stats = new RunningStatistics<double>();
/// foreach (var sample in new[] { 2.0, 4.0, 4.0, 4.0, 5.0, 5.0, 7.0, 9.0 })
///     stats.Add(sample);
///
/// stats.Count;                        // 8
/// stats.Mean;                         // 5
/// stats.PopulationStandardDeviation;  // 2
/// stats.Minimum;                      // 2
/// stats.Maximum;                      // 9
///]]>
/// </code>
/// </example>
[DebuggerDisplay("{ToString(),nq}")]
public partial struct RunningStatistics<T>
    where T : INumber<T>
{
    /// <summary>The number of samples accumulated so far.</summary>
    private long _count;

    /// <summary>The smallest sample seen; only meaningful when <see cref="_count" /> is at least one.</summary>
    private T _minimum;

    /// <summary>The largest sample seen; only meaningful when <see cref="_count" /> is at least one.</summary>
    private T _maximum;

    /// <summary>The running mean of the widened samples (Welford's recurrence).</summary>
    private double _mean;

    /// <summary>The running sum of squared deviations from the mean (Welford's M2 moment).</summary>
    private double _m2;

    /// <summary>
    /// Adds a sample to the accumulator, updating the count, minimum, maximum, mean, and variance moments in O(1).
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

        if (_count == 0)
        {
            _minimum = value;
            _maximum = value;
        }
        else
        {
            _minimum = T.Min(_minimum, value);
            _maximum = T.Max(_maximum, value);
        }

        // Welford's update: the second delta uses the already-updated mean, which is what keeps M2 numerically stable.
        _count++;
        var delta = x - _mean;
        _mean += delta / _count;
        _m2 += delta * (x - _mean);
    }

    /// <summary>
    /// Resets the accumulator to the empty state, discarding every accumulated moment.
    /// </summary>
    public void Reset() =>
        this = default;

    /// <summary>
    /// Merges two independently filled accumulators into one whose moments equal those of a single accumulator fed both
    /// sample streams.
    /// </summary>
    /// <param name="left">The first accumulator to merge.</param>
    /// <param name="right">The second accumulator to merge.</param>
    /// <returns>
    /// An accumulator equivalent to accumulating both operands' sample streams in sequence. When either operand is
    /// empty the other is returned unchanged.
    /// </returns>
    /// <remarks>
    /// Uses the parallel-variance merge of Chan et al., so a stream may be partitioned across workers, accumulated
    /// independently, and recombined without loss beyond ordinary floating-point rounding.
    /// </remarks>
    public static RunningStatistics<T> Combine(RunningStatistics<T> left, RunningStatistics<T> right)
    {
        if (left._count == 0)
            return right;
        if (right._count == 0)
            return left;

        var count = left._count + right._count;
        var delta = right._mean - left._mean;

        return new RunningStatistics<T>
        {
            _count = count,
            _minimum = T.Min(left._minimum, right._minimum),
            _maximum = T.Max(left._maximum, right._maximum),
            _mean = left._mean + (delta * right._count / count),
            _m2 = left._m2 + right._m2 + (delta * delta * ((double)left._count * right._count / count)),
        };
    }

    /// <summary>
    /// Returns a culture-invariant summary of the accumulator state for diagnostics.
    /// </summary>
    /// <returns>
    /// A string such as <c>"Count = 8, Mean = 5, Min = 2, Max = 9"</c>, or <c>"Count = 0"</c> when empty.
    /// </returns>
    public override readonly string ToString() =>
        _count == 0
            ? "Count = 0"
            : string.Format(
                CultureInfo.InvariantCulture,
                "Count = {0}, Mean = {1}, Min = {2}, Max = {3}",
                _count,
                _mean,
                _minimum,
                _maximum);
}
