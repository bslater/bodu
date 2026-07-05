// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OptionTests.TryGetValue.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class OptionTests
{
    /// <summary>
    /// Verifies that TryGetValue returns <see langword="true" /> and the contained value when one is present.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenSome_ShouldReturnTrueAndValue()
    {
        var option = Option<string>.Some("value");

        var found = option.TryGetValue(out var value);

        Assert.IsTrue(found);
        Assert.AreEqual("value", value);
    }

    /// <summary>
    /// Verifies that TryGetValue returns <see langword="false" /> and the default value when no value is present.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenNone_ShouldReturnFalseAndDefault()
    {
        var option = Option<int>.None;

        var found = option.TryGetValue(out var value);

        Assert.IsFalse(found);
        Assert.AreEqual(0, value);
    }
}
