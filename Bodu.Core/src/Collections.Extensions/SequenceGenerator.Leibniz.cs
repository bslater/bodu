// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequenceGenerator.Leibniz.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Bodu.Collections.Extensions;

public static partial class SequenceGenerator
{
    /// <summary>
    /// Generates terms from the <b>Leibniz series</b>: <c>F(n) = (-1) <sup>n</sup> / (2n + 1)</c>, which converges to π/4 as n → ∞.
    /// </summary>
    /// <param name="min">
    /// The minimum absolute value (inclusive) a term must have to be included in the output. Must be greater than or equal to 0.
    /// </param>
    /// <param name="max">
    /// The maximum absolute value (exclusive) a term may have before generation stops. Must be greater than or equal to 0 and not less
    /// than <paramref name="min"/>.
    /// </param>
    /// <returns>
    /// A lazily-evaluated sequence of <see cref="double"/> values from the Leibniz series, each within the specified range of absolute values.
    /// </returns>
    /// <remarks>
    /// <para>The Leibniz series is an infinite alternating series defined as:
    /// <code language="csharp">
    ///<![CDATA[
    /// F(n) = (-1)^n / (2n + 1)
    ///]]>
    /// </code>
    /// It converges to π/4 as n approaches infinity.
    /// </para>
    /// <para>This generator emits only terms where <c>abs(term) ≥ min</c> and <c>abs(term) &lt; max</c>.</para>
    /// <para>
    /// This method is suitable for illustrating convergence, alternating series behavior, or computing partial approximations of π via
    /// summation and multiplying the result by 4.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="min"/> or <paramref name="max"/> is less than 0.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="min"/> is greater than <paramref name="max"/>.</exception>
    public static IEnumerable<double> Leibniz(double min, double max)
    {
        ThrowHelper.ThrowIfLessThan(min, 0);
        ThrowHelper.ThrowIfLessThan(max, 0);
        ThrowHelper.ThrowIfGreaterThanOther(min, max);

        for (int n = 0; ; n++)
        {
            double term = Math.Pow(-1, n) / ((2 * n) + 1);

            if (Math.Abs(term) < min)
                continue;

            if (Math.Abs(term) >= max)
                yield break;

            yield return term;
        }
    }
}
