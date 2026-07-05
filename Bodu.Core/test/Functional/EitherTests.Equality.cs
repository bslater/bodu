// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EitherTests.Equality.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class EitherTests
{
    /// <summary>
    /// Verifies that two left-carrying eithers with equal values compare equal through Equals and both operators.
    /// </summary>
    [TestMethod]
    public void Equals_WhenBothLeftWithEqualValues_ShouldBeEqual()
    {
        var first = Either<int, string>.Left(42);
        var second = Either<int, string>.Left(42);

        Assert.IsTrue(first.Equals(second));
        Assert.IsTrue(first == second);
        Assert.IsFalse(first != second);
    }

    /// <summary>
    /// Verifies that two right-carrying eithers with equal values compare equal.
    /// </summary>
    [TestMethod]
    public void Equals_WhenBothRightWithEqualValues_ShouldBeEqual()
    {
        var first = Either<int, string>.Right("text");
        var second = Either<int, string>.Right("text");

        Assert.IsTrue(first.Equals(second));
        Assert.IsTrue(first == second);
    }

    /// <summary>
    /// Verifies that eithers carrying the same side with different values compare not equal.
    /// </summary>
    [TestMethod]
    public void Equals_WhenSameSideWithDifferentValues_ShouldNotBeEqual()
    {
        var first = Either<int, string>.Left(1);
        var second = Either<int, string>.Left(2);

        Assert.IsFalse(first.Equals(second));
        Assert.IsTrue(first != second);
    }

    /// <summary>
    /// Verifies that the same value carried on different sides compares not equal, even when the side types match.
    /// </summary>
    [TestMethod]
    public void Equals_WhenSameValueOnDifferentSides_ShouldNotBeEqual()
    {
        var left = Either<int, int>.Left(42);
        var right = Either<int, int>.Right(42);

        Assert.IsFalse(left.Equals(right));
        Assert.IsTrue(left != right);
    }

    /// <summary>
    /// Verifies that a left-carrying either does not compare equal to an uninitialized either.
    /// </summary>
    [TestMethod]
    public void Equals_WhenLeftComparedToDefault_ShouldNotBeEqual()
    {
        var left = Either<int, string>.Left(42);
        var uninitialized = default(Either<int, string>);

        Assert.IsFalse(left.Equals(uninitialized));
        Assert.IsFalse(uninitialized.Equals(left));
    }

    /// <summary>
    /// Verifies that equal eithers produce identical hash codes.
    /// </summary>
    [TestMethod]
    public void GetHashCode_WhenEithersAreEqual_ShouldMatch()
    {
        var first = Either<int, string>.Right("text");
        var second = Either<int, string>.Right("text");

        Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
    }

    /// <summary>
    /// Verifies that the same value on different sides produces different hash codes when the side types match.
    /// </summary>
    [TestMethod]
    public void GetHashCode_WhenSameValueOnDifferentSides_ShouldDiffer()
    {
        var left = Either<int, int>.Left(42);
        var right = Either<int, int>.Right(42);

        Assert.AreNotEqual(left.GetHashCode(), right.GetHashCode());
    }

    /// <summary>
    /// Verifies that the boxed Equals overload recognises an equal either and rejects other types and
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Equals_WhenComparedAsObject_ShouldMatchOnlyEqualEither()
    {
        var either = Either<int, string>.Left(42);
        object boxedEqual = Either<int, string>.Left(42);
        object boxedOther = Either<int, string>.Right("text");

        Assert.IsTrue(either.Equals(boxedEqual));
        Assert.IsFalse(either.Equals(boxedOther));
        Assert.IsFalse(either.Equals("Left(42)"));
        Assert.IsFalse(either.Equals(null));
    }
}
