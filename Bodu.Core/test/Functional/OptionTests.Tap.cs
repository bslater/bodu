// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OptionTests.Tap.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class OptionTests
{
    /// <summary>
    /// Verifies that Tap invokes the action with the contained value and returns the same option when a value is
    /// present.
    /// </summary>
    [TestMethod]
    public void Tap_WhenSome_ShouldInvokeActionAndReturnSameOption()
    {
        var observed = 0;
        var option = Option<int>.Some(42);

        var returned = option.Tap(v => observed = v);

        Assert.AreEqual(42, observed);
        Assert.AreEqual(option, returned);
    }

    /// <summary>
    /// Verifies that Tap does not invoke the action and still returns the same option when no value is present.
    /// </summary>
    [TestMethod]
    public void Tap_WhenNone_ShouldNotInvokeActionAndReturnNone()
    {
        var invoked = false;
        var option = Option<int>.None;

        var returned = option.Tap(_ => invoked = true);

        Assert.IsFalse(invoked);
        Assert.IsTrue(returned.IsNone);
    }

    /// <summary>
    /// Verifies that Tap rejects a <see langword="null" /> action even when no value is present.
    /// </summary>
    [TestMethod]
    public void Tap_WhenActionIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Option<int>.None.Tap(null!);
        });

        Assert.AreEqual("action", ex.ParamName);
    }
}
