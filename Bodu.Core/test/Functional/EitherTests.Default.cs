// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EitherTests.Default.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class EitherTests
{
    /// <summary>
    /// Verifies that an uninitialized either reports neither side active.
    /// </summary>
    [TestMethod]
    public void Default_WhenObserved_ShouldReportNeitherSideActive()
    {
        var either = default(Either<int, string>);

        Assert.IsFalse(either.IsLeft);
        Assert.IsFalse(either.IsRight);
    }

    /// <summary>
    /// Verifies that both TryGet accessors return <see langword="false" /> on an uninitialized either without
    /// throwing.
    /// </summary>
    [TestMethod]
    public void Default_WhenQueriedViaTryGet_ShouldReturnFalseOnBothSides()
    {
        var either = default(Either<int, string>);

        Assert.IsFalse(either.TryGetLeft(out _));
        Assert.IsFalse(either.TryGetRight(out _));
    }

    /// <summary>
    /// Verifies that the value-returning Match on an uninitialized either throws
    /// <see cref="InvalidOperationException" /> with the uninitialized-contract message.
    /// </summary>
    [TestMethod]
    public void Default_WhenMatched_ShouldThrowInvalidOperationException()
    {
        var either = default(Either<int, string>);

        var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = either.Match(l => l, r => -1);
        });

        Assert.IsTrue(ex.Message.Contains("has not been initialized", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that the void Match on an uninitialized either throws <see cref="InvalidOperationException" /> with
    /// the uninitialized-contract message.
    /// </summary>
    [TestMethod]
    public void Default_WhenMatched_ForActionOverload_ShouldThrowInvalidOperationException()
    {
        var either = default(Either<int, string>);

        var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            either.Match(_ => { }, _ => { });
        });

        Assert.IsTrue(ex.Message.Contains("has not been initialized", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that MapLeft on an uninitialized either throws <see cref="InvalidOperationException" /> with the
    /// uninitialized-contract message.
    /// </summary>
    [TestMethod]
    public void Default_WhenMapLeftInvoked_ShouldThrowInvalidOperationException()
    {
        var either = default(Either<int, string>);

        var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = either.MapLeft(l => l * 2);
        });

        Assert.IsTrue(ex.Message.Contains("has not been initialized", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that MapRight on an uninitialized either throws <see cref="InvalidOperationException" /> with the
    /// uninitialized-contract message.
    /// </summary>
    [TestMethod]
    public void Default_WhenMapRightInvoked_ShouldThrowInvalidOperationException()
    {
        var either = default(Either<int, string>);

        var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = either.MapRight(r => r.Length);
        });

        Assert.IsTrue(ex.Message.Contains("has not been initialized", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that Swap on an uninitialized either throws <see cref="InvalidOperationException" /> with the
    /// uninitialized-contract message.
    /// </summary>
    [TestMethod]
    public void Default_WhenSwapped_ShouldThrowInvalidOperationException()
    {
        var either = default(Either<int, string>);

        var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = either.Swap();
        });

        Assert.IsTrue(ex.Message.Contains("has not been initialized", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that two uninitialized eithers compare equal.
    /// </summary>
    [TestMethod]
    public void Default_WhenComparedToDefault_ShouldBeEqual()
    {
        var first = default(Either<int, string>);
        var second = default(Either<int, string>);

        Assert.IsTrue(first.Equals(second));
        Assert.IsTrue(first == second);
    }

    /// <summary>
    /// Verifies that an uninitialized either renders as <c>"Either()"</c>.
    /// </summary>
    [TestMethod]
    public void Default_WhenFormatted_ShouldRenderEmptyEither()
    {
        var either = default(Either<int, string>);

        Assert.AreEqual("Either()", either.ToString());
    }
}
