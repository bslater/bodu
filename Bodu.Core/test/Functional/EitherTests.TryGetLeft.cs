// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EitherTests.TryGetLeft.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class EitherTests
{
    /// <summary>
    /// Verifies that TryGetLeft returns <see langword="true" /> and the stored value when the left side is active.
    /// </summary>
    [TestMethod]
    public void TryGetLeft_WhenLeft_ShouldReturnTrueAndValue()
    {
        var either = Either<int, string>.Left(42);

        var found = either.TryGetLeft(out var value);

        Assert.IsTrue(found);
        Assert.AreEqual(42, value);
    }

    /// <summary>
    /// Verifies that TryGetLeft returns <see langword="false" /> and the default value when the right side is active.
    /// </summary>
    [TestMethod]
    public void TryGetLeft_WhenRight_ShouldReturnFalseAndDefault()
    {
        var either = Either<int, string>.Right("text");

        var found = either.TryGetLeft(out var value);

        Assert.IsFalse(found);
        Assert.AreEqual(0, value);
    }

    /// <summary>
    /// Verifies that TryGetLeft returns <see langword="false" /> and the default value on an uninitialized either
    /// without throwing.
    /// </summary>
    [TestMethod]
    public void TryGetLeft_WhenDefault_ShouldReturnFalseAndDefault()
    {
        var either = default(Either<string, int>);

        var found = either.TryGetLeft(out var value);

        Assert.IsFalse(found);
        Assert.IsNull(value);
    }
}
