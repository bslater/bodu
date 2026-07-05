// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResultGenericTests.Tap.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class ResultGenericTests
{
    /// <summary>
    /// Verifies that Tap invokes the action with the carried value and returns the same result for a success.
    /// </summary>
    [TestMethod]
    public void Tap_WhenSuccess_ShouldInvokeActionAndReturnSameResult()
    {
        var observed = 0;
        var source = Result.Success(42);

        var returned = source.Tap(v => observed = v);

        Assert.AreEqual(42, observed);
        Assert.AreEqual(source, returned);
    }

    /// <summary>
    /// Verifies that Tap does not invoke the action and still returns the same result for a failure.
    /// </summary>
    [TestMethod]
    public void Tap_WhenFailure_ShouldNotInvokeActionAndReturnSameResult()
    {
        var invoked = false;
        var source = Result.Failure<int>("boom");

        var returned = source.Tap(_ => invoked = true);

        Assert.IsFalse(invoked);
        Assert.AreEqual(source, returned);
    }

    /// <summary>
    /// Verifies that Tap rejects a <see langword="null" /> action even for a failure.
    /// </summary>
    [TestMethod]
    public void Tap_WhenActionIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Result.Failure<int>("boom").Tap(null!);
        });

        Assert.AreEqual("action", ex.ParamName);
    }
}
