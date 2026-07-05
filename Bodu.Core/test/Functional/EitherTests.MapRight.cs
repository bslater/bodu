// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EitherTests.MapRight.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class EitherTests
{
    /// <summary>
    /// Verifies that MapRight projects the right value when the right side is active.
    /// </summary>
    [TestMethod]
    public void MapRight_WhenRight_ShouldProjectRightValue()
    {
        var either = Either<int, string>.Right("text");

        var mapped = either.MapRight(r => r.Length);

        Assert.IsTrue(mapped.IsRight);
        Assert.IsTrue(mapped.TryGetRight(out var value));
        Assert.AreEqual(4, value);
    }

    /// <summary>
    /// Verifies that MapRight passes a left value through unchanged and does not invoke the selector.
    /// </summary>
    [TestMethod]
    public void MapRight_WhenLeft_ShouldPassLeftValueThroughUnchanged()
    {
        var invoked = false;
        var either = Either<int, string>.Left(42);

        var mapped = either.MapRight(r =>
        {
            invoked = true;
            return r.Length;
        });

        Assert.IsFalse(invoked);
        Assert.IsTrue(mapped.IsLeft);
        Assert.IsTrue(mapped.TryGetLeft(out var value));
        Assert.AreEqual(42, value);
    }

    /// <summary>
    /// Verifies that MapRight rejects a <see langword="null" /> selector.
    /// </summary>
    [TestMethod]
    public void MapRight_WhenSelectorIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Either<int, string>.Right("text").MapRight<int>(null!);
        });

        Assert.AreEqual("selector", ex.ParamName);
    }

    /// <summary>
    /// Verifies that MapRight on an uninitialized either throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void MapRight_WhenDefault_ShouldThrowInvalidOperationException()
    {
        var either = default(Either<int, string>);

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = either.MapRight(r => r.Length);
        });
    }
}
