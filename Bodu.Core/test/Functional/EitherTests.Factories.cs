// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EitherTests.Factories.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class EitherTests
{
    /// <summary>
    /// Verifies that Left stores the supplied value and activates the left side.
    /// </summary>
    [TestMethod]
    public void Left_WhenValueIsValid_ShouldStoreValueOnLeftSide()
    {
        var either = Either<int, string>.Left(42);

        Assert.IsTrue(either.IsLeft);
        Assert.IsTrue(either.TryGetLeft(out var value));
        Assert.AreEqual(42, value);
    }

    /// <summary>
    /// Verifies that Right stores the supplied value and activates the right side.
    /// </summary>
    [TestMethod]
    public void Right_WhenValueIsValid_ShouldStoreValueOnRightSide()
    {
        var either = Either<int, string>.Right("text");

        Assert.IsTrue(either.IsRight);
        Assert.IsTrue(either.TryGetRight(out var value));
        Assert.AreEqual("text", value);
    }

    /// <summary>
    /// Verifies that the strict Left factory rejects <see langword="null" /> with
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Left_WhenValueIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Either<string, int>.Left(null!);
        });

        Assert.AreEqual("value", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the strict Right factory rejects <see langword="null" /> with
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Right_WhenValueIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Either<int, string>.Right(null!);
        });

        Assert.AreEqual("value", ex.ParamName);
    }
}
