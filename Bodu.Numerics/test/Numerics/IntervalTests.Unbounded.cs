// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalTests.Unbounded.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Numerics;

public partial class IntervalTests
{
    /// <summary>
    /// Verifies that the unbounded factories report the expected boundedness and unbounded-side flags.
    /// </summary>
    [TestMethod]
    public void Unbounded_WhenConstructed_ShouldReportExpectedFlags()
    {
        Assert.IsFalse(Interval<double>.All.IsBounded);
        Assert.IsTrue(Interval<double>.All.LowerUnbounded);
        Assert.IsTrue(Interval<double>.All.UpperUnbounded);

        Assert.IsFalse(Interval<double>.AtLeast(0.0).IsBounded);
        Assert.IsTrue(Interval<double>.AtLeast(0.0).UpperUnbounded);
        Assert.IsFalse(Interval<double>.AtLeast(0.0).LowerUnbounded);

        Assert.IsTrue(Interval<double>.AtMost(5.0).LowerUnbounded);
        Assert.IsFalse(Interval<double>.Closed(0.0, 1.0).LowerUnbounded);
        Assert.IsFalse(Interval<double>.Closed(0.0, 1.0).UpperUnbounded);
        Assert.IsTrue(Interval<double>.Closed(0.0, 1.0).IsBounded);
    }

    /// <summary>
    /// Verifies that an unbounded interval is never empty and includes values on the unbounded side.
    /// </summary>
    [TestMethod]
    public void Unbounded_WhenTestedForMembership_ShouldExtendToInfinity()
    {
        Assert.IsFalse(Interval<double>.AtLeast(0.0).IsEmpty);
        Assert.IsTrue(Interval<double>.AtLeast(0.0).Contains(0.0));
        Assert.IsTrue(Interval<double>.AtLeast(0.0).Contains(1e300));
        Assert.IsFalse(Interval<double>.AtLeast(0.0).Contains(-1.0));

        Assert.IsFalse(Interval<double>.GreaterThan(0.0).Contains(0.0));
        Assert.IsTrue(Interval<double>.GreaterThan(0.0).Contains(1e-300));

        Assert.IsTrue(Interval<double>.AtMost(5.0).Contains(-1e300));
        Assert.IsTrue(Interval<double>.AtMost(5.0).Contains(5.0));
        Assert.IsFalse(Interval<double>.LessThan(5.0).Contains(5.0));

        Assert.IsTrue(Interval<double>.All.Contains(double.MinValue));
        Assert.IsTrue(Interval<double>.All.Contains(double.MaxValue));
    }

    /// <summary>
    /// Verifies that reading <see cref="Interval{T}.Length" /> on an unbounded interval throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Length_WhenUnbounded_ShouldThrowInvalidOperationException()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = Interval<double>.AtLeast(0.0).Length;
        });
    }

    /// <summary>
    /// Verifies that intersecting complementary unbounded intervals yields the expected bounded or unbounded result.
    /// </summary>
    [TestMethod]
    public void Intersect_WhenUnbounded_ShouldTightenToTheOverlap()
    {
        Assert.AreEqual(Interval<int>.Closed(0, 10), Interval<int>.AtLeast(0).Intersect(Interval<int>.AtMost(10)));
        Assert.AreEqual(Interval<int>.Closed(3, 5), Interval<int>.All.Intersect(Interval<int>.Closed(3, 5)));
        Assert.AreEqual(Interval<int>.AtLeast(5), Interval<int>.AtLeast(0).Intersect(Interval<int>.AtLeast(5)));
    }

    /// <summary>
    /// Verifies that the union of two overlapping half-lines covering the whole line is <see cref="Interval{T}.All" />.
    /// </summary>
    [TestMethod]
    public void TryUnion_WhenHalfLinesCoverTheLine_ShouldReturnAll()
    {
        bool ok = Interval<int>.AtLeast(0).TryUnion(Interval<int>.AtMost(10), out Interval<int> result);

        Assert.IsTrue(ok);
        Assert.AreEqual(Interval<int>.All, result);
    }

    /// <summary>
    /// Verifies that unbounded intervals format with the infinity glyphs and round-trip through <c>Parse</c>.
    /// </summary>
    /// <param name="text">The canonical text form.</param>
    [TestMethod]
    [DataRow("[0, +∞)")]
    [DataRow("(0, +∞)")]
    [DataRow("(-∞, 5]")]
    [DataRow("(-∞, 5)")]
    [DataRow("(-∞, +∞)")]
    public void Unbounded_WhenFormattedAndParsed_ShouldRoundTrip(string text)
    {
        var value = Interval<int>.Parse(text, CultureInfo.InvariantCulture);

        Assert.AreEqual(text, value.ToString(null, CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Verifies that an inclusive bracket against an infinity token fails to parse (infinity is never included).
    /// </summary>
    [TestMethod]
    [DataRow("[0, +∞]")]
    [DataRow("[-∞, 5]")]
    public void Parse_WhenInfinityBracketIsInclusive_ShouldFail(string text)
    {
        Assert.IsFalse(Interval<int>.TryParse(text, CultureInfo.InvariantCulture, out _));
    }
}
