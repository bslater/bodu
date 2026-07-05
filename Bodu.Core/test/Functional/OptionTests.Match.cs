// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OptionTests.Match.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class OptionTests
{
    /// <summary>
    /// Verifies that the value-returning Match invokes the Some branch when a value is present.
    /// </summary>
    [TestMethod]
    public void Match_WhenSome_ShouldInvokeOnSomeBranch()
    {
        var result = Option<int>.Some(5).Match(v => v * 2, () => -1);

        Assert.AreEqual(10, result);
    }

    /// <summary>
    /// Verifies that the value-returning Match invokes the None branch when no value is present.
    /// </summary>
    [TestMethod]
    public void Match_WhenNone_ShouldInvokeOnNoneBranch()
    {
        var result = Option<int>.None.Match(v => v * 2, () => -1);

        Assert.AreEqual(-1, result);
    }

    /// <summary>
    /// Verifies that the void Match invokes exactly the Some action when a value is present.
    /// </summary>
    [TestMethod]
    public void Match_WhenSome_ForActionOverload_ShouldInvokeOnSomeOnly()
    {
        var someValue = 0;
        var noneInvoked = false;

        Option<int>.Some(9).Match(v => someValue = v, () => noneInvoked = true);

        Assert.AreEqual(9, someValue);
        Assert.IsFalse(noneInvoked);
    }

    /// <summary>
    /// Verifies that the void Match invokes exactly the None action when no value is present.
    /// </summary>
    [TestMethod]
    public void Match_WhenNone_ForActionOverload_ShouldInvokeOnNoneOnly()
    {
        var someInvoked = false;
        var noneInvoked = false;

        Option<int>.None.Match(_ => someInvoked = true, () => noneInvoked = true);

        Assert.IsFalse(someInvoked);
        Assert.IsTrue(noneInvoked);
    }

    /// <summary>
    /// Verifies that Match rejects a <see langword="null" /> Some branch.
    /// </summary>
    [TestMethod]
    public void Match_WhenOnSomeIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Option<int>.Some(1).Match(null!, () => 0);
        });

        Assert.AreEqual("onSome", ex.ParamName);
    }

    /// <summary>
    /// Verifies that Match rejects a <see langword="null" /> None branch even when a value is present.
    /// </summary>
    [TestMethod]
    public void Match_WhenOnNoneIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Option<int>.Some(1).Match(v => v, null!);
        });

        Assert.AreEqual("onNone", ex.ParamName);
    }
}
