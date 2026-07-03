// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalTests.Laws.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Numerics;

public partial class IntervalTests
{
    // Sample points on a half-integer grid spanning and overflowing the endpoint universe {0,1,2,3}, so membership is
    // probed at interior points, at endpoints, and outside the range.
    private static readonly double[] SamplePoints = { -1.0, 0.0, 0.5, 1.0, 1.5, 2.0, 2.5, 3.0, 4.0 };

    /// <summary>
    /// Enumerates a representative universe of continuous intervals over <see cref="double" /> endpoints in
    /// <c>{0,1,2,3}</c> with every inclusivity combination, including the empty and unbounded shapes.
    /// </summary>
    /// <returns>The interval universe.</returns>
    private static IEnumerable<Interval<double>> IntervalUniverse()
    {
        for (int lower = 0; lower <= 3; lower++)
        {
            for (int upper = 0; upper <= 3; upper++)
            {
                yield return new Interval<double>(lower, upper, lowerInclusive: true, upperInclusive: true);
                yield return new Interval<double>(lower, upper, lowerInclusive: true, upperInclusive: false);
                yield return new Interval<double>(lower, upper, lowerInclusive: false, upperInclusive: true);
                yield return new Interval<double>(lower, upper, lowerInclusive: false, upperInclusive: false);
            }
        }

        yield return Interval<double>.All;
        yield return Interval<double>.AtLeast(1.0);
        yield return Interval<double>.GreaterThan(1.0);
        yield return Interval<double>.AtMost(2.0);
        yield return Interval<double>.LessThan(2.0);
        yield return Interval<double>.Empty;
    }

    private static bool PairContains(IntervalPair<double> pair, double x)
    {
        foreach (Interval<double> piece in pair)
        {
            if (piece.Contains(x))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Verifies, exhaustively over the interval universe and a half-integer sample grid, that
    /// <see cref="Interval{T}.Intersect(Interval{T})" />, <see cref="Interval{T}.Difference(Interval{T})" />,
    /// <see cref="Interval{T}.SymmetricDifference(Interval{T})" />, and the contiguous
    /// <see cref="Interval{T}.TryUnion(Interval{T}, out Interval{T})" /> agree with the pointwise set predicates they
    /// implement — the algebraic contract, not just fixed examples.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void SetOperations_OverFiniteDomain_ShouldSatisfyMembershipLaws()
    {
        Interval<double>[] universe = IntervalUniverse().ToArray();

        foreach (Interval<double> a in universe)
        {
            foreach (Interval<double> b in universe)
            {
                Interval<double> intersection = a.Intersect(b);
                IntervalPair<double> difference = a.Difference(b);
                IntervalPair<double> symmetric = a.SymmetricDifference(b);
                bool contiguous = a.TryUnion(b, out Interval<double> union);

                foreach (double x in SamplePoints)
                {
                    bool inA = a.Contains(x);
                    bool inB = b.Contains(x);

                    Assert.AreEqual(inA && inB, intersection.Contains(x), $"Intersect: a={a}, b={b}, x={x}");
                    Assert.AreEqual(inA && !inB, PairContains(difference, x), $"Difference: a={a}, b={b}, x={x}");
                    Assert.AreEqual(inA ^ inB, PairContains(symmetric, x), $"SymmetricDifference: a={a}, b={b}, x={x}");

                    if (contiguous)
                        Assert.AreEqual(inA || inB, union.Contains(x), $"Union: a={a}, b={b}, x={x}");
                }
            }
        }
    }

    /// <summary>
    /// Verifies that intersection is commutative and idempotent across the interval universe.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Intersect_OverFiniteDomain_ShouldBeCommutativeAndIdempotent()
    {
        Interval<double>[] universe = IntervalUniverse().ToArray();

        foreach (Interval<double> a in universe)
        {
            Assert.AreEqual(a, a.Intersect(a), $"Idempotence: a={a}");

            foreach (Interval<double> b in universe)
                Assert.AreEqual(a.Intersect(b), b.Intersect(a), $"Commutativity: a={a}, b={b}");
        }
    }

    /// <summary>
    /// Verifies that a NaN endpoint is rejected with <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenEndpointIsNaN_ShouldThrowArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new Interval<double>(double.NaN, 1.0, lowerInclusive: true, upperInclusive: true);
        });
    }

    /// <summary>
    /// Verifies that a decimal interval round-trips through format and parse under comma-decimal cultures, using the
    /// culture-aware endpoint separator.
    /// </summary>
    /// <param name="cultureName">The culture whose decimal separator is a comma.</param>
    [TestMethod]
    [DataRow("de-DE")]
    [DataRow("fr-FR")]
    public void DecimalInterval_WhenCommaDecimalCulture_ShouldRoundTrip(string cultureName)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        var value = Interval<decimal>.Closed(1.5m, 2.5m);

        string text = value.ToString(null, culture);

        Assert.IsTrue(Interval<decimal>.TryParse(text, culture, out Interval<decimal> parsed));
        Assert.AreEqual(value, parsed);
    }
}
