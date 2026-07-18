// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ValueRangeTests.Equality.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class ValueRangeTests
{
    /// <summary>
    /// Verifies that two entries with identical endpoints and equal values compare equal through
    /// <see cref="ValueRange{TKey, TValue}.Equals(ValueRange{TKey, TValue})" />.
    /// </summary>
    [TestMethod]
    public void Equals_WhenEndpointsAndValueMatch_ShouldReturnTrue()
    {
        var left = new ValueRange<int, string>(0, 10, "a");
        var right = new ValueRange<int, string>(0, 10, "a");

        Assert.IsTrue(left.Equals(right));
        Assert.IsTrue(left == right);
        Assert.IsFalse(left != right);
    }

    /// <summary>
    /// Verifies that entries differing in an endpoint compare unequal.
    /// </summary>
    [TestMethod]
    public void Equals_WhenEndpointsDiffer_ShouldReturnFalse()
    {
        var left = new ValueRange<int, string>(0, 10, "a");
        var right = new ValueRange<int, string>(0, 11, "a");

        Assert.IsFalse(left.Equals(right));
        Assert.IsFalse(left == right);
        Assert.IsTrue(left != right);
    }

    /// <summary>
    /// Verifies that entries with matching endpoints but different values compare unequal.
    /// </summary>
    [TestMethod]
    public void Equals_WhenValuesDiffer_ShouldReturnFalse()
    {
        var left = new ValueRange<int, string>(0, 10, "a");
        var right = new ValueRange<int, string>(0, 10, "b");

        Assert.IsFalse(left.Equals(right));
        Assert.IsTrue(left != right);
    }

    /// <summary>
    /// Verifies that <see cref="ValueRange{TKey, TValue}.Equals(object)" /> returns <see langword="false" /> for a
    /// <see langword="null" /> operand and for an operand of an unrelated type.
    /// </summary>
    [TestMethod]
    public void Equals_WhenComparedWithNullOrOtherType_ShouldReturnFalse()
    {
        var sut = new ValueRange<int, string>(0, 10, "a");

        Assert.IsFalse(sut.Equals(null));
        Assert.IsFalse(sut.Equals("not a value range"));
    }

    /// <summary>
    /// Verifies that <see cref="ValueRange{TKey, TValue}.Equals(object)" /> returns <see langword="true" /> for a boxed
    /// equal entry.
    /// </summary>
    [TestMethod]
    public void Equals_WhenBoxedEqualEntry_ShouldReturnTrue()
    {
        var sut = new ValueRange<int, string>(0, 10, "a");
        object boxed = new ValueRange<int, string>(0, 10, "a");

        Assert.IsTrue(sut.Equals(boxed));
    }

    /// <summary>
    /// Verifies that equal entries produce the same hash code.
    /// </summary>
    [TestMethod]
    public void GetHashCode_WhenEntriesAreEqual_ShouldMatch()
    {
        var left = new ValueRange<int, string>(0, 10, "a");
        var right = new ValueRange<int, string>(0, 10, "a");

        Assert.AreEqual(left.GetHashCode(), right.GetHashCode());
    }
}
