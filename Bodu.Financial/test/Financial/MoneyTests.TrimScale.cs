// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyTests.TrimScale.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial;

public partial class MoneyTests
{
    /// <summary>
    /// Verifies that <see cref="Money.TrimScale" /> removes purely trailing-zero precision down to the currency's
    /// registered minor units.
    /// </summary>
    [TestMethod]
    public void TrimScale_WhenAllSubRegistryDigitsAreZero_ShouldTrimToRegistryPrecision()
    {
        Money trimmed = Money.FromExplicitScale(12.5m, CurrencyCode.USD, 6).TrimScale();

        Assert.AreEqual(2, trimmed.MinorUnits);
        Assert.AreEqual("USD 12.50", trimmed.ToString("R"));
    }

    /// <summary>
    /// Verifies that <see cref="Money.TrimScale" /> stops at the last significant digit, trimming only the trailing
    /// zeros beyond it.
    /// </summary>
    [TestMethod]
    public void TrimScale_WhenTrailingZerosFollowSignificantDigits_ShouldTrimPartially()
    {
        Money trimmed = Money.FromExplicitScale(12.340010m, CurrencyCode.USD, 6).TrimScale();

        Assert.AreEqual(5, trimmed.MinorUnits);
        Assert.AreEqual("USD 12.34001", trimmed.ToString("R"));
    }

    /// <summary>
    /// Verifies that <see cref="Money.TrimScale" /> never reports a scale below the currency's registered minor units,
    /// even when the amount is a whole number.
    /// </summary>
    [TestMethod]
    public void TrimScale_WhenAmountIsWholeNumber_ShouldFloorAtRegistryPrecision()
    {
        Money trimmed = Money.FromExplicitScale(20m, CurrencyCode.USD, 6).TrimScale();

        Assert.AreEqual(2, trimmed.MinorUnits);
        Assert.AreEqual("USD 20.00", trimmed.ToString("R"));
    }

    /// <summary>
    /// Verifies that <see cref="Money.TrimScale" /> leaves a value with significant sub-registry digits unchanged.
    /// </summary>
    [TestMethod]
    public void TrimScale_WhenFinestDigitsAreSignificant_ShouldReturnValueUnchanged()
    {
        Money price = Money.FromExplicitScale(12.345678m, CurrencyCode.USD, 6);

        Money trimmed = price.TrimScale();

        Assert.AreEqual(6, trimmed.MinorUnits);
        Assert.AreEqual(price, trimmed);
    }

    /// <summary>
    /// Verifies that <see cref="Money.TrimScale" /> is a no-op for ordinary money already at the registered precision.
    /// </summary>
    [TestMethod]
    public void TrimScale_WhenValueAtRegistryPrecision_ShouldBeNoOp()
    {
        Money money = new Money(19.99m, CurrencyCode.USD);

        Money trimmed = money.TrimScale();

        Assert.AreEqual(2, trimmed.MinorUnits);
        Assert.AreEqual(money, trimmed);
    }

    /// <summary>
    /// Verifies that trimming never changes the numeric amount — the trimmed value remains equal to the original
    /// under the type's numeric equality.
    /// </summary>
    [TestMethod]
    public void TrimScale_WhenTrimmed_ShouldPreserveNumericEquality()
    {
        Money original = Money.FromExplicitScale(12.500000m, CurrencyCode.USD, 6);

        Assert.AreEqual(original, original.TrimScale());
    }

    /// <summary>
    /// Verifies that <see cref="Money.TrimScale" /> on a default-initialised, currency-less value throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void TrimScale_WhenValueIsDefault_ShouldThrowInvalidOperationException() =>
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = default(Money).TrimScale();
        });
}
