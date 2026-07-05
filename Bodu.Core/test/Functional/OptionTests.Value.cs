// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OptionTests.Value.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class OptionTests
{
    /// <summary>
    /// Verifies that Value returns the contained value when one is present.
    /// </summary>
    [TestMethod]
    public void Value_WhenSome_ShouldReturnContainedValue()
    {
        var option = Option<int>.Some(7);

        Assert.AreEqual(7, option.Value);
    }

    /// <summary>
    /// Verifies that accessing Value on None throws <see cref="InvalidOperationException" /> with the documented
    /// message.
    /// </summary>
    [TestMethod]
    public void Value_WhenNone_ShouldThrowInvalidOperationException()
    {
        var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = Option<int>.None.Value;
        });

        Assert.IsTrue(ex.Message.Contains("does not contain a value", StringComparison.Ordinal));
    }
}
