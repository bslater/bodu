// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DiscreteIntervalTests.Equality.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public partial class DiscreteIntervalTests
{
    /// <summary>
    /// Verifies that intervals describing the same integer set compare equal even when built from different endpoint
    /// shapes, and that their hash codes agree.
    /// </summary>
    [TestMethod]
    public void Equals_WhenSameIntegerSet_ShouldBeEqualWithMatchingHash()
    {
        DiscreteInterval<int> a = DiscreteInterval<int>.Closed(2, 4);
        DiscreteInterval<int> b = DiscreteInterval<int>.Open(1, 5);

        Assert.IsTrue(a.Equals(b));
        Assert.IsTrue(a.Equals((object)b));
        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }

    /// <summary>
    /// Verifies that intervals describing different integer sets are not equal, and that a non-interval object is not
    /// equal.
    /// </summary>
    [TestMethod]
    public void Equals_WhenDifferentSetOrType_ShouldNotBeEqual()
    {
        Assert.IsFalse(DiscreteInterval<int>.Closed(1, 5).Equals(DiscreteInterval<int>.Closed(1, 6)));
        Assert.IsFalse(DiscreteInterval<int>.AtLeast(0).Equals(DiscreteInterval<int>.AtMost(0)));
        Assert.IsFalse(DiscreteInterval<int>.Closed(1, 5).Equals("not an interval"));
    }

    /// <summary>
    /// Verifies that the <c>==</c> and <c>!=</c> operators reflect set equality.
    /// </summary>
    [TestMethod]
    public void EqualityOperators_WhenCompared_ShouldReflectSetEquality()
    {
        Assert.IsTrue(DiscreteInterval<int>.Closed(2, 4) == DiscreteInterval<int>.OpenClosed(1, 4));
        Assert.IsFalse(DiscreteInterval<int>.Closed(2, 4) != DiscreteInterval<int>.OpenClosed(1, 4));
        Assert.IsTrue(DiscreteInterval<int>.Closed(1, 5) != DiscreteInterval<int>.Closed(1, 4));
        Assert.IsTrue(DiscreteInterval<int>.Empty == default);
    }
}
