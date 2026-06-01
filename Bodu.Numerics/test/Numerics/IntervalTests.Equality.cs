// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalTests.Equality.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public partial class IntervalTests
{
    /// <summary>
    /// Verifies that intervals with identical bounds and inclusivity flags are equal and produce identical hashes.
    /// </summary>
    [TestMethod]
    public void Equals_WhenStructurallyIdentical_ShouldReturnTrue()
    {
        var a = Interval<int>.ClosedOpen(1, 5);
        var b = Interval<int>.ClosedOpen(1, 5);

        Assert.IsTrue(a.Equals(b));
        Assert.IsTrue(a == b);
        Assert.IsFalse(a != b);
        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }

    /// <summary>
    /// Verifies that intervals differing only in inclusivity flags are not equal.
    /// </summary>
    [TestMethod]
    public void Equals_WhenInclusivityFlagsDiffer_ShouldReturnFalse()
    {
        Assert.IsFalse(Interval<int>.Closed(1, 5).Equals(Interval<int>.ClosedOpen(1, 5)));
        Assert.IsFalse(Interval<int>.Closed(1, 5).Equals(Interval<int>.OpenClosed(1, 5)));
        Assert.IsFalse(Interval<int>.Closed(1, 5).Equals(Interval<int>.Open(1, 5)));
    }

    /// <summary>
    /// Verifies that all empty intervals — regardless of their constructed bounds — compare equal and hash equally.
    /// </summary>
    [TestMethod]
    public void Equals_WhenBothEmpty_ShouldReturnTrueRegardlessOfBounds()
    {
        Interval<int> a = Interval<int>.Empty;
        var b = new Interval<int>(5, 1, true, true);       // inverted bounds
        var c = new Interval<int>(5, 5, false, false);     // equal bounds, both open
        var d = new Interval<int>(100, 50, false, true);   // very different bounds

        Assert.IsTrue(a.Equals(b));
        Assert.IsTrue(a.Equals(c));
        Assert.IsTrue(a.Equals(d));
        Assert.IsTrue(b.Equals(c));

        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        Assert.AreEqual(a.GetHashCode(), c.GetHashCode());
        Assert.AreEqual(a.GetHashCode(), d.GetHashCode());
    }

    /// <summary>
    /// Verifies that an empty interval is not equal to any non-empty interval.
    /// </summary>
    [TestMethod]
    public void Equals_WhenEmptyComparedToNonEmpty_ShouldReturnFalse()
    {
        Assert.IsFalse(Interval<int>.Empty.Equals(Interval<int>.Closed(1, 5)));
        Assert.IsFalse(Interval<int>.Closed(1, 5).Equals(Interval<int>.Empty));
    }

    /// <summary>
    /// Verifies the boxed <see cref="object.Equals(object?)" /> override.
    /// </summary>
    [TestMethod]
    public void Equals_WhenComparedToObject_ShouldReturnExpected()
    {
        var a = Interval<int>.Closed(1, 5);

        Assert.IsTrue(a.Equals((object)Interval<int>.Closed(1, 5)));
        Assert.IsFalse(a.Equals((object?)null));
        Assert.IsFalse(a.Equals("not an interval"));
    }
}
