// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OptionTests.Equality.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class OptionTests
{
    /// <summary>
    /// Verifies that two Some options with equal values compare equal and share a hash code.
    /// </summary>
    [TestMethod]
    public void Equals_WhenBothSomeWithEqualValues_ShouldBeEqual()
    {
        var left = Option<string>.Some("value");
        var right = Option<string>.Some("value");

        Assert.IsTrue(left.Equals(right));
        Assert.AreEqual(left.GetHashCode(), right.GetHashCode());
    }

    /// <summary>
    /// Verifies that Some options with different values are not equal.
    /// </summary>
    [TestMethod]
    public void Equals_WhenBothSomeWithDifferentValues_ShouldNotBeEqual()
    {
        Assert.IsFalse(Option<int>.Some(1).Equals(Option<int>.Some(2)));
    }

    /// <summary>
    /// Verifies that Some and None are never equal, in either direction.
    /// </summary>
    [TestMethod]
    public void Equals_WhenSomeComparedToNone_ShouldNotBeEqual()
    {
        Assert.IsFalse(Option<int>.Some(0).Equals(Option<int>.None));
        Assert.IsFalse(Option<int>.None.Equals(Option<int>.Some(0)));
    }

    /// <summary>
    /// Verifies that two None options compare equal and share a hash code.
    /// </summary>
    [TestMethod]
    public void Equals_WhenBothNone_ShouldBeEqual()
    {
        Assert.IsTrue(Option<int>.None.Equals(Option<int>.None));
        Assert.AreEqual(Option<int>.None.GetHashCode(), default(Option<int>).GetHashCode());
    }

    /// <summary>
    /// Verifies the boxed Equals overload accepts only options of the same type.
    /// </summary>
    [TestMethod]
    public void Equals_WhenComparedToBoxedObject_ShouldMatchOnlySameTypedOption()
    {
        var option = Option<int>.Some(1);

        Assert.IsTrue(option.Equals((object)Option<int>.Some(1)));
        Assert.IsFalse(option.Equals((object)1));
        Assert.IsFalse(option.Equals(null));
    }
}
