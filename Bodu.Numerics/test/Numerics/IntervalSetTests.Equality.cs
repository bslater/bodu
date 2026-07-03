// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalSetTests.Equality.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public partial class IntervalSetTests
{
    /// <summary>
    /// Verifies that sets with the same normalized pieces are equal — regardless of input order — and share a hash
    /// code.
    /// </summary>
    [TestMethod]
    public void Equals_WhenSameNormalizedPieces_ShouldBeEqualWithMatchingHash()
    {
        IntervalSet<int> a = IntervalSet<int>.Of(Interval<int>.Closed(1, 3), Interval<int>.Closed(2, 5), Interval<int>.Closed(8, 9));
        IntervalSet<int> b = IntervalSet<int>.Of(Interval<int>.Closed(8, 9), Interval<int>.Closed(1, 5));

        Assert.IsTrue(a.Equals(b));
        Assert.IsTrue(a.Equals((object)b));
        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }

    /// <summary>
    /// Verifies that sets with different pieces, and a non-set object, are not equal.
    /// </summary>
    [TestMethod]
    public void Equals_WhenDifferentPiecesOrType_ShouldNotBeEqual()
    {
        IntervalSet<int> a = IntervalSet<int>.Of(Interval<int>.Closed(1, 5));

        Assert.IsFalse(a.Equals(IntervalSet<int>.Of(Interval<int>.Closed(1, 6))));
        Assert.IsFalse(a.Equals(IntervalSet<int>.Of(Interval<int>.Closed(1, 5), Interval<int>.Closed(8, 9))));
        Assert.IsFalse(a.Equals("not a set"));
    }

    /// <summary>
    /// Verifies that the <c>==</c> and <c>!=</c> operators reflect set equality, including the empty set.
    /// </summary>
    [TestMethod]
    public void EqualityOperators_WhenCompared_ShouldReflectSetEquality()
    {
        Assert.IsTrue(IntervalSet<int>.Of(Interval<int>.Closed(1, 3), Interval<int>.Closed(3, 5)) == IntervalSet<int>.Of(Interval<int>.Closed(1, 5)));
        Assert.IsTrue(IntervalSet<int>.Of(Interval<int>.Closed(1, 5)) != IntervalSet<int>.Of(Interval<int>.Closed(1, 6)));
        Assert.IsTrue(IntervalSet<int>.Empty == default);
    }
}
