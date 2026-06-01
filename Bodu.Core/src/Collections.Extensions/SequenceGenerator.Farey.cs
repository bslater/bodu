// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequenceGenerator.Farey.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Extensions;

public static partial class SequenceGenerator
{
    /// <summary>
    /// Yields the Farey sequence <c>F<sub>n</sub></c> as ordered <c>(numerator, denominator)</c> pairs in lowest terms.
    /// </summary>
    /// <param name="order">The order <c>n</c> of the Farey sequence. Must be at least <c>1</c>.</param>
    /// <returns>
    /// A lazily evaluated, finite sequence of tuples <c>(Numerator, Denominator)</c> covering every reduced fraction
    /// <c>a/b</c> with <c>0 ≤ a ≤ b ≤ <paramref name="order" /></c> and <c>gcd(a, b) = 1</c>, emitted in strict
    /// ascending order from <c>0/1</c> to <c>1/1</c>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="order" /> is less than <c>1</c>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Reach for this generator instead of building Farey sequences with nested loops or LINQ filtering on
    /// <see cref="System.Linq.Enumerable.Range(int, int)" />: it uses the standard mediant recurrence and runs in time
    /// linear in the size of <c>F<sub>n</sub></c>, avoiding the cost of computing GCDs for rejected fractions.
    /// </para>
    /// <para>
    /// Both endpoints are inclusive: the sequence always begins at <c>(0, 1)</c> and ends at <c>(1, 1)</c>. The pairs
    /// are emitted in strictly increasing rational order so consumers can rely on monotonic ordering without
    /// re-sorting.
    /// </para>
    /// <para>
    /// Iteration is deferred and allocates only the iterator state. The implementation is deterministic and thread-safe
    /// in the usual sense: each enumerator carries its own state.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// foreach (var (num, den) in SequenceGenerator.Farey(5))
    ///     Console.Write($"{num}/{den} "); // => 0/1 1/5 1/4 1/3 2/5 1/2 3/5 2/3 3/4 4/5 1/1
    ///]]>
    /// </code>
    /// </example>
    public static IEnumerable<(int Numerator, int Denominator)> Farey(int order)
    {
        ThrowHelper.ThrowIfLessThan(order, 1);

        yield return (0, 1);
        int a = 0, b = 1, c = 1, d = order;

        while (c <= order)
        {
            var k = (order + b) / d;
            var tempA = c;
            var tempB = d;

            c = (k * c) - a;
            d = (k * d) - b;

            a = tempA;
            b = tempB;

            yield return (a, b);
        }
    }
}
