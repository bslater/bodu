// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OptionTests.Map.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class OptionTests
{
    /// <summary>
    /// Verifies that Map projects the contained value when one is present.
    /// </summary>
    [TestMethod]
    public void Map_WhenSome_ShouldProjectValue()
    {
        var option = Option<string>.Some("railway");

        var mapped = option.Map(s => s.Length);

        Assert.AreEqual(Option<int>.Some(7), mapped);
    }

    /// <summary>
    /// Verifies that Map propagates None without invoking the selector.
    /// </summary>
    [TestMethod]
    public void Map_WhenNone_ShouldReturnNoneWithoutInvokingSelector()
    {
        var invoked = false;

        var mapped = Option<string>.None.Map(s =>
        {
            invoked = true;
            return s.Length;
        });

        Assert.IsTrue(mapped.IsNone);
        Assert.IsFalse(invoked);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> projection result maps to None (the lenient lift).
    /// </summary>
    [TestMethod]
    public void Map_WhenSelectorReturnsNull_ShouldReturnNone()
    {
        var option = Option<int>.Some(1);

        var mapped = option.Map<string>(_ => null!);

        Assert.IsTrue(mapped.IsNone);
    }

    /// <summary>
    /// Verifies that Map rejects a <see langword="null" /> selector.
    /// </summary>
    [TestMethod]
    public void Map_WhenSelectorIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Option<int>.Some(1).Map<string>(null!);
        });

        Assert.AreEqual("selector", ex.ParamName);
    }
}
