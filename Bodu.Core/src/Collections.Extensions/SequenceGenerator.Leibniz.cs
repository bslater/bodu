// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequenceGenerator.Leibniz.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Extensions;

public static partial class SequenceGenerator
{
    /// <summary>
    /// Yields terms of the Leibniz series <c>F(n) = (-1)<sup>n</sup> / (2n + 1)</c> whose absolute magnitudes lie
    /// within the requested half-open interval.
    /// </summary>
    /// <param name="min">
    /// The inclusive lower bound on the absolute value of emitted terms. Terms with
    /// <c>|F(n)| &lt; <paramref name="min" /></c> are skipped rather than terminating iteration. Must be non-negative.
    /// </param>
    /// <param name="max">
    /// The exclusive upper bound on the absolute value of emitted terms. Iteration stops as soon as
    /// <c>|F(n)| ≥ <paramref name="max" /></c>. Must be non-negative and not less than <paramref name="min" />.
    /// </param>
    /// <returns>
    /// A lazily evaluated, finite sequence of <see cref="double" /> values drawn from the Leibniz series in their
    /// original (signed) alternating order.
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
    /// The window is applied to absolute magnitudes so that the alternating sign is preserved in the output. The lower
    /// bound behaves as a magnitude floor (terms below it are filtered, not terminating), while the upper bound behaves
    /// as a stop condition: as soon as a term's magnitude reaches <paramref name="max" /> iteration ends. Setting
    /// <paramref name="min" /> to <c>0</c> emits every term up to <paramref name="max" />.
    /// </para>
    /// <para>
    /// Because the magnitude sequence <c>1, 1/3, 1/5, …</c> is monotonically decreasing, a non-zero
    /// <paramref name="min" /> produces at most a finite leading run before the predicate filters everything else;
    /// iteration still progresses until the upper bound is reached. The iterator is deferred, deterministic, and
    /// allocates only its own state.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// Approximate π by summing the first hundred terms whose magnitude is at least 1e-3.
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

        for (var n = 0; ; n++)
        {
            var term = Math.Pow(-1, n) / ((2 * n) + 1);

            if (Math.Abs(term) < min)
                continue;

            if (Math.Abs(term) >= max)
                yield break;

            yield return term;
        }
    }
}
