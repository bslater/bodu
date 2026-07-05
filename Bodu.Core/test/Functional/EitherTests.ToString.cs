// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EitherTests.ToString.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class EitherTests
{
    /// <summary>
    /// Verifies that a left-carrying either renders as <c>"Left(value)"</c>.
    /// </summary>
    [TestMethod]
    public void ToString_WhenLeft_ShouldRenderLeftWithValue()
    {
        var either = Either<int, string>.Left(42);

        Assert.AreEqual("Left(42)", either.ToString());
    }

    /// <summary>
    /// Verifies that a right-carrying either renders as <c>"Right(value)"</c>.
    /// </summary>
    [TestMethod]
    public void ToString_WhenRight_ShouldRenderRightWithValue()
    {
        var either = Either<int, string>.Right("text");

        Assert.AreEqual("Right(text)", either.ToString());
    }

    /// <summary>
    /// Verifies that an uninitialized either renders as <c>"Either()"</c>.
    /// </summary>
    [TestMethod]
    public void ToString_WhenDefault_ShouldRenderEmptyEither()
    {
        var either = default(Either<int, string>);

        Assert.AreEqual("Either()", either.ToString());
    }
}
