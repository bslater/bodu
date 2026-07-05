// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OptionTests.Operators.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class OptionTests
{
    /// <summary>
    /// Verifies that the implicit conversion lifts a non-null reference to Some.
    /// </summary>
    [TestMethod]
    public void ImplicitConversion_WhenValueIsNotNull_ShouldProduceSome()
    {
        Option<string> option = "value";

        Assert.AreEqual(Option<string>.Some("value"), option);
    }

    /// <summary>
    /// Verifies the lenient lift: the implicit conversion maps a <see langword="null" /> reference to None rather
    /// than throwing.
    /// </summary>
    [TestMethod]
    public void ImplicitConversion_WhenValueIsNull_ShouldProduceNone()
    {
        string? value = null;

        Option<string> option = value;

        Assert.IsTrue(option.IsNone);
    }

    /// <summary>
    /// Verifies that FromNullable lifts a nullable value type in both states.
    /// </summary>
    [TestMethod]
    public void FromNullable_WhenNullableHasValueOrNot_ShouldProduceSomeOrNone()
    {
        int? present = 5;
        int? absent = null;

        Assert.AreEqual(Option<int>.Some(5), Option.FromNullable(present));
        Assert.IsTrue(Option.FromNullable(absent).IsNone);
    }

    /// <summary>
    /// Verifies that the equality operators agree with <see cref="Option{T}.Equals(Option{T})" />.
    /// </summary>
    [TestMethod]
    public void EqualityOperators_WhenComparingOptions_ShouldMatchEqualsSemantics()
    {
        Assert.IsTrue(Option<int>.Some(1) == Option<int>.Some(1));
        Assert.IsTrue(Option<int>.Some(1) != Option<int>.Some(2));
        Assert.IsTrue(Option<int>.None == default);
        Assert.IsTrue(Option<int>.Some(1) != Option<int>.None);
    }
}
