// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EitherTests.MapLeft.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class EitherTests
{
    /// <summary>
    /// Verifies that MapLeft projects the left value when the left side is active.
    /// </summary>
    [TestMethod]
    public void MapLeft_WhenLeft_ShouldProjectLeftValue()
    {
        var either = Either<int, string>.Left(21);

        var mapped = either.MapLeft(l => $"value {l * 2}");

        Assert.IsTrue(mapped.IsLeft);
        Assert.IsTrue(mapped.TryGetLeft(out var value));
        Assert.AreEqual("value 42", value);
    }

    /// <summary>
    /// Verifies that MapLeft passes a right value through unchanged and does not invoke the selector.
    /// </summary>
    [TestMethod]
    public void MapLeft_WhenRight_ShouldPassRightValueThroughUnchanged()
    {
        var invoked = false;
        var either = Either<int, string>.Right("untouched");

        var mapped = either.MapLeft(l =>
        {
            invoked = true;
            return l * 2;
        });

        Assert.IsFalse(invoked);
        Assert.IsTrue(mapped.IsRight);
        Assert.IsTrue(mapped.TryGetRight(out var value));
        Assert.AreEqual("untouched", value);
    }

    /// <summary>
    /// Verifies that MapLeft rejects a <see langword="null" /> selector.
    /// </summary>
    [TestMethod]
    public void MapLeft_WhenSelectorIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Either<int, string>.Left(1).MapLeft<string>(null!);
        });

        Assert.AreEqual("selector", ex.ParamName);
    }

    /// <summary>
    /// Verifies that MapLeft on an uninitialized either throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void MapLeft_WhenDefault_ShouldThrowInvalidOperationException()
    {
        var either = default(Either<int, string>);

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = either.MapLeft(l => l * 2);
        });
    }
}
