// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EitherTests.Properties.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class EitherTests
{
    /// <summary>
    /// Verifies that a left-carrying either reports IsLeft <see langword="true" /> and IsRight
    /// <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void IsLeft_WhenLeft_ShouldBeTrueAndIsRightFalse()
    {
        var either = Either<int, string>.Left(1);

        Assert.IsTrue(either.IsLeft);
        Assert.IsFalse(either.IsRight);
    }

    /// <summary>
    /// Verifies that a right-carrying either reports IsRight <see langword="true" /> and IsLeft
    /// <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void IsRight_WhenRight_ShouldBeTrueAndIsLeftFalse()
    {
        var either = Either<int, string>.Right("text");

        Assert.IsTrue(either.IsRight);
        Assert.IsFalse(either.IsLeft);
    }

    /// <summary>
    /// Verifies that an uninitialized either reports both IsLeft and IsRight as <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void IsLeft_WhenDefault_ShouldBeFalseAlongsideIsRight()
    {
        var either = default(Either<int, string>);

        Assert.IsFalse(either.IsLeft);
        Assert.IsFalse(either.IsRight);
    }
}
