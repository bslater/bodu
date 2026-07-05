// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EitherTests.Match.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class EitherTests
{
    /// <summary>
    /// Verifies that the value-returning Match invokes the left branch when the left side is active.
    /// </summary>
    [TestMethod]
    public void Match_WhenLeft_ShouldInvokeOnLeftBranch()
    {
        var result = Either<int, string>.Left(5).Match(l => l * 2, r => -1);

        Assert.AreEqual(10, result);
    }

    /// <summary>
    /// Verifies that the value-returning Match invokes the right branch when the right side is active.
    /// </summary>
    [TestMethod]
    public void Match_WhenRight_ShouldInvokeOnRightBranch()
    {
        var result = Either<int, string>.Right("text").Match(l => -1, r => r.Length);

        Assert.AreEqual(4, result);
    }

    /// <summary>
    /// Verifies that the void Match invokes exactly the left action when the left side is active.
    /// </summary>
    [TestMethod]
    public void Match_WhenLeft_ForActionOverload_ShouldInvokeOnLeftOnly()
    {
        var leftValue = 0;
        var rightInvoked = false;

        Either<int, string>.Left(9).Match(l => leftValue = l, _ => rightInvoked = true);

        Assert.AreEqual(9, leftValue);
        Assert.IsFalse(rightInvoked);
    }

    /// <summary>
    /// Verifies that the void Match invokes exactly the right action when the right side is active.
    /// </summary>
    [TestMethod]
    public void Match_WhenRight_ForActionOverload_ShouldInvokeOnRightOnly()
    {
        var leftInvoked = false;
        string? rightValue = null;

        Either<int, string>.Right("text").Match(_ => leftInvoked = true, r => rightValue = r);

        Assert.IsFalse(leftInvoked);
        Assert.AreEqual("text", rightValue);
    }

    /// <summary>
    /// Verifies that the value-returning Match rejects a <see langword="null" /> left branch.
    /// </summary>
    [TestMethod]
    public void Match_WhenOnLeftIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Either<int, string>.Left(1).Match(null!, r => 0);
        });

        Assert.AreEqual("onLeft", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the value-returning Match rejects a <see langword="null" /> right branch even when the left side
    /// is active.
    /// </summary>
    [TestMethod]
    public void Match_WhenOnRightIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Either<int, string>.Left(1).Match(l => l, null!);
        });

        Assert.AreEqual("onRight", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the void Match rejects a <see langword="null" /> left action.
    /// </summary>
    [TestMethod]
    public void Match_WhenOnLeftIsNull_ForActionOverload_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            Either<int, string>.Left(1).Match(null!, _ => { });
        });

        Assert.AreEqual("onLeft", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the void Match rejects a <see langword="null" /> right action even when the left side is active.
    /// </summary>
    [TestMethod]
    public void Match_WhenOnRightIsNull_ForActionOverload_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            Either<int, string>.Left(1).Match(_ => { }, null!);
        });

        Assert.AreEqual("onRight", ex.ParamName);
    }
}
