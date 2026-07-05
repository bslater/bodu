// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OptionTests.Bind.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class OptionTests
{
    /// <summary>
    /// Verifies that Bind returns the selector's option when a value is present.
    /// </summary>
    [TestMethod]
    public void Bind_WhenSomeAndSelectorReturnsSome_ShouldReturnSelectorResult()
    {
        var option = Option<string>.Some("42");

        var bound = option.Bind(s => Option<int>.Some(int.Parse(s)));

        Assert.AreEqual(Option<int>.Some(42), bound);
    }

    /// <summary>
    /// Verifies that Bind surfaces a None produced by the selector.
    /// </summary>
    [TestMethod]
    public void Bind_WhenSomeAndSelectorReturnsNone_ShouldReturnNone()
    {
        var option = Option<string>.Some("not a number");

        var bound = option.Bind(_ => Option<int>.None);

        Assert.IsTrue(bound.IsNone);
    }

    /// <summary>
    /// Verifies that Bind propagates None without invoking the selector.
    /// </summary>
    [TestMethod]
    public void Bind_WhenNone_ShouldReturnNoneWithoutInvokingSelector()
    {
        var invoked = false;

        var bound = Option<string>.None.Bind(_ =>
        {
            invoked = true;
            return Option<int>.Some(1);
        });

        Assert.IsTrue(bound.IsNone);
        Assert.IsFalse(invoked);
    }

    /// <summary>
    /// Verifies that Bind rejects a <see langword="null" /> selector.
    /// </summary>
    [TestMethod]
    public void Bind_WhenSelectorIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Option<int>.Some(1).Bind<string>(null!);
        });

        Assert.AreEqual("selector", ex.ParamName);
    }
}
