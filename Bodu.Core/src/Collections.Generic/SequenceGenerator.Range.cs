// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequenceGenerator.Range.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public static partial class SequenceGenerator
{
    /// <summary>
    /// Yields consecutive integers from <paramref name="start"/> through <paramref name="stop"/>, walking in whichever direction is
    /// implied by the two endpoints.
    /// </summary>
    /// <param name="start">The first value emitted by the sequence.</param>
    /// <param name="stop">The final value emitted by the sequence; this value is included in the output when reached exactly.</param>
    /// <returns>A lazily evaluated, finite sequence of <see cref="int"/> values inclusive of both endpoints.</returns>
    /// <remarks>
    /// <para>
    /// Prefer this overload over <see cref="System.Linq.Enumerable.Range(int, int)"/> when the natural way to express the range is
    /// "from <c>a</c> to <c>b</c>" rather than "<c>n</c> values starting at <c>a</c>", or when a descending sequence is required —
    /// <c>Enumerable.Range</c> only walks forwards and rejects negative counts.
    /// </para>
    /// <para>
    /// The step direction is chosen automatically: ascending when <paramref name="start"/> is less than <paramref name="stop"/>,
    /// descending otherwise. When <paramref name="start"/> equals <paramref name="stop"/> a single-element sequence is returned.
    /// </para>
    /// <para>
    /// Both endpoints are inclusive. The result is deferred — no values are produced until the sequence is enumerated — and the
    /// underlying iterator allocates only the per-enumeration state object.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    /// foreach (int n in SequenceGenerator.Range(10, 14))
    ///     Console.Write($"{n} ");
    /// // => 10 11 12 13 14
    ///
    /// foreach (int n in SequenceGenerator.Range(3, -2))
    ///     Console.Write($"{n} ");
    /// // => 3 2 1 0 -1 -2
    /// </code>
    /// </example>
    public static IEnumerable<int> Range(int start, int stop) => SequenceGenerator.Range(start, stop, start < stop ? 1 : -1);

    /// <summary>
    /// Yields integers between <paramref name="start"/> and <paramref name="stop"/> inclusive, advancing by <paramref name="step"/> at each iteration.
    /// </summary>
    /// <param name="start">The first value emitted by the sequence.</param>
    /// <param name="stop">The endpoint at which iteration terminates; included in the output when reached exactly.</param>
    /// <param name="step">
    /// The signed delta applied between successive values. Positive values produce an ascending sequence, negative values produce a
    /// descending sequence, and zero produces an unbounded sequence that yields <paramref name="start"/> forever.
    /// </param>
    /// <returns>
    /// A lazily evaluated sequence of <see cref="int"/> values; finite when <paramref name="step"/> is non-zero, otherwise infinite.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Use this overload when the caller needs explicit control over the stride, including descending ranges and non-unit steps that
    /// <see cref="System.Linq.Enumerable.Range(int, int)"/> cannot express. The two-argument <see cref="Range(int, int)"/> is more
    /// convenient when a step of <c>+1</c> or <c>-1</c> is sufficient.
    /// </para>
    /// <para>
    /// Iteration is bounded by <paramref name="stop"/>: the inclusive endpoint is yielded only when <paramref name="step"/> divides
    /// the interval evenly. If the next value would overflow <see cref="int.MaxValue"/> or <see cref="int.MinValue"/> the sequence
    /// terminates cleanly rather than throwing.
    /// </para>
    /// <para>
    /// When <paramref name="step"/> is <c>0</c> the method returns an infinite sequence — callers must compose it with an operator
    /// such as <c>Take</c> or <c>TakeWhile</c> to bound enumeration.
    /// </para>
    /// <para>The result is deferred and allocation-light; no array or list is materialized.</para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    /// foreach (int n in SequenceGenerator.Range(0, 20, 5))
    ///     Console.Write($"{n} ");
    /// // => 0 5 10 15 20
    ///
    /// foreach (int n in SequenceGenerator.Range(10, 1, -3))
    ///     Console.Write($"{n} ");
    /// // => 10 7 4 1
    ///
    /// // Step of zero yields an unbounded sequence — bound it with Take.
    /// var heartbeat = SequenceGenerator.Range(42, 0, 0).Take(3);
    /// // => 42, 42, 42
    /// </code>
    /// </example>
    public static IEnumerable<int> Range(int start, int stop, int step)
    {
        if (step == 0)
        {
            while (true)
                yield return start;
        }

        for (var i = start; step > 0 ? i <= stop : i >= stop;)
        {
            yield return i;

            try
            {
                i = checked(i + step);
            }
            catch (OverflowException)
            {
                yield break;
            }
        }
    }

    /// <summary>
    /// Yields a fixed number of consecutive 64-bit integers beginning at <paramref name="start"/>.
    /// </summary>
    /// <param name="start">The first value emitted by the sequence.</param>
    /// <param name="count">The number of values to produce. Must be non-negative.</param>
    /// <returns>
    /// A lazily evaluated, finite sequence of <see cref="long"/> values containing exactly <paramref name="count"/> elements.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="count"/> is negative, or when the inclusive range
    /// <c>[<paramref name="start"/>, <paramref name="start"/> + <paramref name="count"/> - 1]</c> would overflow
    /// <see cref="long.MaxValue"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Prefer this overload when the desired output is a count-bounded run of contiguous values, particularly for ranges that exceed
    /// the addressable region of <see cref="System.Linq.Enumerable.Range(int, int)"/> (which is restricted to <see cref="int"/>).
    /// </para>
    /// <para>
    /// A <paramref name="count"/> of <c>0</c> produces an empty sequence. The endpoint is exclusive in the sense that the final value
    /// emitted is <c><paramref name="start"/> + <paramref name="count"/> - 1</c>.
    /// </para>
    /// <para>
    /// Overflow is rejected up-front by the argument validation, so the iterator never throws mid-enumeration. Iteration is deferred,
    /// deterministic, and allocation-free apart from the iterator state.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    /// foreach (long n in SequenceGenerator.Range(1_000_000_000_000L, 4))
    ///     Console.Write($"{n} ");
    /// // => 1000000000000 1000000000001 1000000000002 1000000000003
    ///
    /// var empty = SequenceGenerator.Range(0L, 0).ToArray();
    /// // => empty.Length == 0
    /// </code>
    /// </example>
    public static IEnumerable<long> Range(long start, int count)
    {
        ThrowHelper.ThrowIfLessThan(count, 0);
        ThrowHelper.ThrowIfSequenceRangeOverflows(start, count);

        for (var i = 0; i < count; i++)
            yield return start + i;
    }
}
