// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OptionTests.Filter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class OptionTests
{
    /// <summary>
    /// Verifies that Filter retains the value when the predicate is satisfied.
    /// </summary>
    [TestMethod]
    public void Filter_WhenSomeAndPredicateMatches_ShouldReturnSameOption()
    {
        var option = Option<int>.Some(10);

        var filtered = option.Filter(v => v > 5);

        Assert.AreEqual(option, filtered);
    }

    /// <summary>
    /// Verifies that Filter returns None when the predicate is not satisfied.
    /// </summary>
    [TestMethod]
    public void Filter_WhenSomeAndPredicateFails_ShouldReturnNone()
    {
        var filtered = Option<int>.Some(3).Filter(v => v > 5);

        Assert.IsTrue(filtered.IsNone);
    }

    /// <summary>
    /// Verifies that Filter propagates None without invoking the predicate.
    /// </summary>
    [TestMethod]
    public void Filter_WhenNone_ShouldReturnNoneWithoutInvokingPredicate()
    {
        var invoked = false;

        var filtered = Option<int>.None.Filter(_ =>
        {
            invoked = true;
            return true;
        });

        Assert.IsTrue(filtered.IsNone);
        Assert.IsFalse(invoked);
    }

    /// <summary>
    /// Verifies that Filter rejects a <see langword="null" /> predicate.
    /// </summary>
    [TestMethod]
    public void Filter_WhenPredicateIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Option<int>.Some(1).Filter(null!);
        });

        Assert.AreEqual("predicate", ex.ParamName);
    }
}
