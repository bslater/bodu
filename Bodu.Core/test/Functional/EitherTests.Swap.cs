// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EitherTests.Swap.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class EitherTests
{
    /// <summary>
    /// Verifies that Swap moves a left value to the right side.
    /// </summary>
    [TestMethod]
    public void Swap_WhenLeft_ShouldProduceRightWithSameValue()
    {
        var either = Either<int, string>.Left(42);

        var swapped = either.Swap();

        Assert.IsTrue(swapped.IsRight);
        Assert.IsTrue(swapped.TryGetRight(out var value));
        Assert.AreEqual(42, value);
    }

    /// <summary>
    /// Verifies that Swap moves a right value to the left side.
    /// </summary>
    [TestMethod]
    public void Swap_WhenRight_ShouldProduceLeftWithSameValue()
    {
        var either = Either<int, string>.Right("text");

        var swapped = either.Swap();

        Assert.IsTrue(swapped.IsLeft);
        Assert.IsTrue(swapped.TryGetLeft(out var value));
        Assert.AreEqual("text", value);
    }

    /// <summary>
    /// Verifies that swapping twice yields an either equal to the original.
    /// </summary>
    [TestMethod]
    public void Swap_WhenAppliedTwice_ShouldEqualOriginal()
    {
        var either = Either<int, string>.Left(42);

        var roundTripped = either.Swap().Swap();

        Assert.AreEqual(either, roundTripped);
    }

    /// <summary>
    /// Verifies that Swap on an uninitialized either throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Swap_WhenDefault_ShouldThrowInvalidOperationException()
    {
        var either = default(Either<int, string>);

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = either.Swap();
        });
    }
}
