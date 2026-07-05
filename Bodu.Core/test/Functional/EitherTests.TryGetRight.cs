// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EitherTests.TryGetRight.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class EitherTests
{
    /// <summary>
    /// Verifies that TryGetRight returns <see langword="true" /> and the stored value when the right side is active.
    /// </summary>
    [TestMethod]
    public void TryGetRight_WhenRight_ShouldReturnTrueAndValue()
    {
        var either = Either<int, string>.Right("text");

        var found = either.TryGetRight(out var value);

        Assert.IsTrue(found);
        Assert.AreEqual("text", value);
    }

    /// <summary>
    /// Verifies that TryGetRight returns <see langword="false" /> and the default value when the left side is active.
    /// </summary>
    [TestMethod]
    public void TryGetRight_WhenLeft_ShouldReturnFalseAndDefault()
    {
        var either = Either<int, string>.Left(42);

        var found = either.TryGetRight(out var value);

        Assert.IsFalse(found);
        Assert.IsNull(value);
    }

    /// <summary>
    /// Verifies that TryGetRight returns <see langword="false" /> and the default value on an uninitialized either
    /// without throwing.
    /// </summary>
    [TestMethod]
    public void TryGetRight_WhenDefault_ShouldReturnFalseAndDefault()
    {
        var either = default(Either<string, int>);

        var found = either.TryGetRight(out var value);

        Assert.IsFalse(found);
        Assert.AreEqual(0, value);
    }
}
