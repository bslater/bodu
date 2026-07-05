// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OptionTests.GetValueOrDefault.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class OptionTests
{
    /// <summary>
    /// Verifies that the parameterless overload returns the contained value when present and the type default when
    /// not.
    /// </summary>
    [TestMethod]
    public void GetValueOrDefault_WhenParameterless_ShouldReturnValueOrTypeDefault()
    {
        Assert.AreEqual(5, Option<int>.Some(5).GetValueOrDefault());
        Assert.AreEqual(0, Option<int>.None.GetValueOrDefault());
        Assert.IsNull(Option<string>.None.GetValueOrDefault());
    }

    /// <summary>
    /// Verifies that the fallback-value overload returns the contained value when present and the fallback when not.
    /// </summary>
    [TestMethod]
    public void GetValueOrDefault_WhenFallbackValueSupplied_ShouldReturnValueOrFallback()
    {
        Assert.AreEqual(5, Option<int>.Some(5).GetValueOrDefault(99));
        Assert.AreEqual(99, Option<int>.None.GetValueOrDefault(99));
    }

    /// <summary>
    /// Verifies that the factory overload invokes the factory only when no value is present.
    /// </summary>
    [TestMethod]
    public void GetValueOrDefault_WhenFactorySupplied_ShouldInvokeFactoryOnlyWhenNone()
    {
        var invoked = false;

        var fromSome = Option<int>.Some(5).GetValueOrDefault(() =>
        {
            invoked = true;
            return 99;
        });

        Assert.AreEqual(5, fromSome);
        Assert.IsFalse(invoked);
        Assert.AreEqual(99, Option<int>.None.GetValueOrDefault(() => 99));
    }

    /// <summary>
    /// Verifies that the factory overload rejects a <see langword="null" /> factory.
    /// </summary>
    [TestMethod]
    public void GetValueOrDefault_WhenFactoryIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Option<int>.Some(1).GetValueOrDefault((Func<int>)null!);
        });

        Assert.AreEqual("defaultFactory", ex.ParamName);
    }
}
