// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequenceGenerator.Leibniz.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Sequences;

public static partial class SequenceGenerator
{
    /// <summary>
    /// Yields terms of the Leibniz series <c>F(n) = (-1)<sup>n</sup> / (2n + 1)</c> whose absolute magnitudes lie
    /// within the requested half-open interval.
    /// </summary>
    /// <param name="min">
    /// The inclusive lower bound on the absolute value of emitted terms. Because term magnitudes strictly decrease,
    /// iteration ends as soon as <c>|F(n)| &lt; <paramref name="min" /></c> — no later term can re-enter the window.
    /// Must be non-negative; a value of <c>0</c> produces an unbounded sequence (bound consumption with <c>Take</c>).
    /// </param>
    /// <param name="max">
    /// The exclusive upper bound on the absolute value of emitted terms. Iteration stops as soon as
    /// <c>|F(n)| ≥ <paramref name="max" /></c>. Must be non-negative and not less than <paramref name="min" />.
    /// </param>
    /// <returns>
    /// A lazily evaluated sequence of <see cref="double" /> values drawn from the Leibniz series in their original
    /// (signed) alternating order. The sequence is finite whenever <paramref name="min" /> is greater than zero.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="min" /> or <paramref name="max" /> is negative.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="min" /> is greater than <paramref name="max" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The Leibniz series is the alternating series whose partial sums converge to <c>π/4</c>. Use this generator when
    /// illustrating convergence behavior, when computing rough approximations to π by summing terms and multiplying by
    /// <c>4</c>, or when demonstrating how slowly such an alternating series converges.
    /// </para>
    /// <para>
    /// The window is applied to absolute magnitudes so that the alternating sign is preserved in the output. Both
    /// bounds are terminating: as soon as a term's magnitude reaches <paramref name="max" /> or falls below
    /// <paramref name="min" />, iteration ends. Setting <paramref name="min" /> to <c>0</c> emits every term below
    /// <paramref name="max" /> without terminating.
    /// </para>
    /// <para>
    /// Because the magnitude sequence <c>1, 1/3, 1/5, …</c> starts at one and is monotonically decreasing, the upper
    /// bound only ever gates the first term — a <paramref name="max" /> of <c>1</c> or less terminates immediately with
    /// an empty sequence — and once a term drops below <paramref name="min" /> no later term can return to the window,
    /// so ending iteration there is what keeps the sequence finite. The iterator is deferred, deterministic, and
    /// allocates only its own state.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// // Approximate π by summing the first hundred terms whose magnitude is at least 1e-3.
    /// double partial = 0;
    /// foreach (double term in SequenceGenerator.Leibniz(1e-3, 1.1).Take(100))
    ///     partial += term;
    ///
    /// double pi = partial * 4; // => pi ≈ 3.139... (slow convergence, expected)
    ///]]>
    /// </code>
    /// </example>
    public static IEnumerable<double> Leibniz(double min, double max)
    {
        ThrowHelper.ThrowIfLessThan(min, 0);
        ThrowHelper.ThrowIfLessThan(max, 0);
        ThrowHelper.ThrowIfGreaterThanOther(min, max);

        for (int n = 0; ; n++)
        {
            double magnitude = 1.0 / ((2 * n) + 1);

            // Magnitudes strictly decrease from 1, so a term at or above the upper bound can only be the first term,
            // and a term below the lower bound can never be followed by one back inside the window — both end iteration.
            if (magnitude >= max || magnitude < min)
                yield break;

            yield return (n & 1) == 0 ? magnitude : -magnitude;
        }
    }
}
