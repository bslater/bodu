// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyTests.Default.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public partial class MoneyTests
{
    /// <summary>
    /// Verifies that a default-initialised <see cref="Money" /> reports <see cref="Money.IsDefault" /> as
    /// <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void IsDefault_WhenDefaultInitialised_ShouldReturnTrue()
    {
        Money money = default;

        Assert.IsTrue(money.IsDefault);
        Assert.IsFalse(money.HasCurrency);
    }

    /// <summary>
    /// Verifies that a constructed <see cref="Money" /> reports <see cref="Money.IsDefault" /> as
    /// <see langword="false" /> and <see cref="Money.HasCurrency" /> as <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void IsDefault_WhenConstructed_ShouldReturnFalse()
    {
        Money money = Money.From(0m, "USD");

        Assert.IsFalse(money.IsDefault);
        Assert.IsTrue(money.HasCurrency);
    }

    /// <summary>
    /// Verifies that multiplying a default-initialised, currency-less <see cref="Money" /> throws
    /// <see cref="InvalidOperationException" /> rather than silently producing a currency-less value.
    /// </summary>
    [TestMethod]
    public void Multiply_WhenDefault_ShouldThrowInvalidOperationException()
    {
        Money money = default;

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = money * 2m;
        });
    }

    /// <summary>
    /// Verifies that dividing a default-initialised, currency-less <see cref="Money" /> throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Divide_WhenDefault_ShouldThrowInvalidOperationException()
    {
        Money money = default;

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = money / 2m;
        });
    }

    /// <summary>
    /// Verifies that negating a default-initialised, currency-less <see cref="Money" /> throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Negate_WhenDefault_ShouldThrowInvalidOperationException()
    {
        Money money = default;

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = -money;
        });
    }

    /// <summary>
    /// Verifies that taking the absolute value of a default-initialised, currency-less <see cref="Money" /> throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Abs_WhenDefault_ShouldThrowInvalidOperationException()
    {
        Money money = default;

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = money.Abs;
        });
    }

    /// <summary>
    /// Verifies that allocating a default-initialised, currency-less <see cref="Money" /> throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Allocate_WhenDefault_ShouldThrowInvalidOperationException()
    {
        Money money = default;

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = money.Allocate(3);
        });
    }

    /// <summary>
    /// Verifies that equality remains usable on default-initialised values: two defaults compare equal and neither
    /// comparison throws.
    /// </summary>
    [TestMethod]
    public void Equals_WhenBothDefault_ShouldReturnTrue()
    {
        Money left = default;
        Money right = default;

        Assert.AreEqual(left, right);
        Assert.IsTrue(left == right);
    }

    /// <summary>
    /// Verifies that formatting a default-initialised value does not throw, so diagnostic surfaces (debugger, logging)
    /// remain safe.
    /// </summary>
    [TestMethod]
    public void ToString_WhenDefault_ShouldNotThrow()
    {
        Money money = default;

        string text = money.ToString();

        Assert.IsNotNull(text);
    }
}
