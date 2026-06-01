// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyDtoAdapterTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.Serialization;

/// <summary>
/// Verifies <see cref="MoneyDtoAdapter" /> converts between <see cref="Money" /> and the Google-compatible
/// <see cref="MoneyDto" /> transport shape.
/// </summary>
[TestClass]
public class MoneyDtoAdapterTests
{
    /// <summary>
    /// Verifies that a fractional amount is split into whole units and nano-units.
    /// </summary>
    [TestMethod]
    public void ToDto_WhenAmountHasFraction_ShouldSplitUnitsAndNanos()
    {
        MoneyDto dto = MoneyDtoAdapter.ToDto(new Money(19.99m, "USD"));

        Assert.AreEqual("USD", dto.CurrencyCode);
        Assert.AreEqual(19L, dto.Units);
        Assert.AreEqual(990_000_000, dto.Nanos);
    }

    /// <summary>
    /// Verifies that a negative amount produces consistently signed units and nanos.
    /// </summary>
    [TestMethod]
    public void ToDto_WhenAmountNegative_ShouldSignBothComponents()
    {
        MoneyDto dto = MoneyDtoAdapter.ToDto(new Money(-19.99m, "USD"));

        Assert.AreEqual(-19L, dto.Units);
        Assert.AreEqual(-990_000_000, dto.Nanos);
    }

    /// <summary>
    /// Verifies that <see cref="MoneyDtoAdapter.ToMoney(MoneyDto, UnknownCurrencyPolicy)" /> reconstructs the amount.
    /// </summary>
    [TestMethod]
    public void ToMoney_WhenValidDto_ShouldReconstructAmount()
    {
        Money money = MoneyDtoAdapter.ToMoney(new MoneyDto("USD", 19L, 990_000_000));

        Assert.AreEqual(new Money(19.99m, "USD"), money);
    }

    /// <summary>
    /// Verifies that conversion round-trips across a representative spread of amounts and currencies.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("USD", 19.99)]
    [DataRow("JPY", 1234.0)]
    [DataRow("BHD", 1.234)]
    [DataRow("USD", -0.50)]
    [DataRow("EUR", 0.0)]
    public void RoundTrip_WhenConvertingToDtoAndBack_ShouldPreserveValue(string iso, double amount)
    {
        Money original = new((decimal)amount, iso);

        Money roundTripped = MoneyDtoAdapter.ToMoney(MoneyDtoAdapter.ToDto(original));

        Assert.AreEqual(original, roundTripped);
    }

    /// <summary>
    /// Verifies that an out-of-range nanos value is rejected.
    /// </summary>
    [TestMethod]
    public void ToMoney_WhenNanosOutOfRange_ShouldThrowArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => _ = MoneyDtoAdapter.ToMoney(new MoneyDto("USD", 1L, 1_000_000_000)));
    }

    /// <summary>
    /// Verifies that mismatched unit and nano signs are rejected.
    /// </summary>
    [TestMethod]
    public void ToMoney_WhenSignsMismatch_ShouldThrowArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => _ = MoneyDtoAdapter.ToMoney(new MoneyDto("USD", 1L, -500_000_000)));
    }

    /// <summary>
    /// Verifies that an unregistered currency is rejected under the default policy but accepted unscaled when opted in.
    /// </summary>
    [TestMethod]
    public void ToMoney_WhenUnregisteredCurrency_ShouldFollowPolicy()
    {
        Assert.ThrowsExactly<ArgumentException>(() => _ = MoneyDtoAdapter.ToMoney(new MoneyDto("XYZ", 1L, 0)));

        Money unscaled = MoneyDtoAdapter.ToMoney(new MoneyDto("XYZ", 1L, 500_000_000), UnknownCurrencyPolicy.AllowUnscaled);
        Assert.AreEqual("XYZ", unscaled.IsoCode);
        Assert.AreEqual(1.5m, unscaled.Amount);
    }
}
