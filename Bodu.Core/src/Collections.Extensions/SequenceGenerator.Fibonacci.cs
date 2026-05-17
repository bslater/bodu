// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequenceGenerator.Fibonacci.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Extensions;

public static partial class SequenceGenerator
{
    /// <summary>
    /// Yields Fibonacci numbers that fall within the half-open interval
    /// <c>[<paramref name="min" />, <paramref name="max" />)</c>.
    /// </summary>
    /// <param name="min">
    /// The inclusive lower bound. Fibonacci numbers strictly less than this value are skipped. Must be non-negative.
    /// </param>
    /// <param name="max">
    /// The exclusive upper bound. Iteration stops as soon as the next Fibonacci number is greater than or equal to this
    /// value. Must be non-negative and not less than <paramref name="min" />.
    /// </param>
    /// <returns>
    /// A lazily evaluated, finite sequence of <see cref="long" /> Fibonacci numbers in ascending order.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="min" /> or <paramref name="max" /> is negative.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="min" /> is greater than <paramref name="max" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Use this generator when only the Fibonacci numbers within a known numeric window are required — building the
    /// full sequence up to <see cref="long.MaxValue" /> and filtering after the fact wastes iterations. The half-open
    /// interval matches the convention used elsewhere in <see cref="SequenceGenerator" />, so two consecutive ranges
    /// can be stitched together without overlap or gaps.
    /// </para>
    /// <para>
    /// Lower bound is inclusive, upper bound is exclusive. If no Fibonacci number lies in the requested range the
    /// result is an empty sequence. If the next Fibonacci number would overflow <see cref="long" /> the iterator
    /// terminates cleanly rather than throwing. The traditional seed values <c>0</c> and <c>1</c> are emitted only when
    /// the window includes them.
    /// </para>
    /// <para>
    /// Iteration is deferred and deterministic; allocation is limited to the iterator state.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    /// <![CDATA[ foreach (long f in SequenceGenerator.Fibonacci(10, 1000)) Console.Write($"{f} "); // =>
    /// 13 21 34 55 89 144 233 377 610 987 var none = SequenceGenerator.Fibonacci(50, 54).ToArray(); // => empty (no
    /// Fibonacci numbers between 50 and 54). ]]>
    /// </code>
    /// </example>
    public static IEnumerable<long> Fibonacci(long min, long max)
    {
        ThrowHelper.ThrowIfLessThan(min, 0);
        ThrowHelper.ThrowIfLessThan(max, 0);
        ThrowHelper.ThrowIfGreaterThanOther(min, max);

        long prev = -1, next = 1;

        while (true)
        {
            long sum;
            try
            {
                checked { sum = prev + next; }
            }
            catch (OverflowException)
            {
                yield break; // stop gracefully on overflow
            }

            if (sum >= max)
                yield break;

            prev = next;
            next = sum;

            if (sum >= min)
                yield return sum;
        }
    }
}
