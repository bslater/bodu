// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AnchoredIntervalTests.Equality.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

public partial class AnchoredIntervalTests
{
    /// <summary>
    /// Verifies that two intervals with the same spacing are equal with equal hash codes, so a consumer can detect
    /// configuration changes by comparison rather than by re-parsing text.
    /// </summary>
    [TestMethod]
    public void Equals_WhenSameInterval_ShouldBeEqualWithSameHashCode()
    {
        var first = new AnchoredInterval(TimeSpan.FromHours(4));
        var second = AnchoredInterval.Parse("PT4H");

        Assert.IsTrue(first.Equals(second));
        Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
    }

    /// <summary>
    /// Verifies that intervals with different spacings are not equal.
    /// </summary>
    [TestMethod]
    public void Equals_WhenDifferentInterval_ShouldNotBeEqual()
    {
        var first = new AnchoredInterval(TimeSpan.FromHours(4));
        var second = new AnchoredInterval(TimeSpan.FromHours(6));

        Assert.IsFalse(first.Equals(second));
    }

    /// <summary>
    /// Verifies that an interval is not equal to <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Equals_WhenOtherIsNull_ShouldNotBeEqual()
    {
        var interval = new AnchoredInterval(TimeSpan.FromHours(4));

        Assert.IsFalse(interval.Equals(null));
        Assert.IsFalse(interval.Equals((object?)null));
    }

    /// <summary>
    /// Verifies that the <see cref="object" /> overload agrees with the typed overload.
    /// </summary>
    [TestMethod]
    public void Equals_WhenComparedAsObject_ShouldMatchTypedComparison()
    {
        var first = new AnchoredInterval(TimeSpan.FromHours(4));
        object second = new AnchoredInterval(TimeSpan.FromHours(4));

        Assert.IsTrue(first.Equals(second));
        Assert.IsFalse(first.Equals("PT4H"));
    }
}
