// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyInfoValidatorTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

/// <summary>
/// Verifies <see cref="CurrencyInfoValidator" /> enforces the currency-metadata rules through both the throwing
/// <c>Validate</c> path and the non-throwing <c>TryValidate</c> path.
/// </summary>
[TestClass]
public class CurrencyInfoValidatorTests
{
    /// <summary>
    /// Verifies that a cash-rounding increment finer than the minor-unit grid is rejected.
    /// </summary>
    [TestMethod]
    public void Validate_WhenCashRoundingTooFine_ShouldThrowArgumentException() =>
        Assert.ThrowsExactly<ArgumentException>(() => CurrencyInfoValidator.Validate(new CurrencyInfo("USD", 2, 0.001m, false, null, null)));

    /// <summary>
    /// Verifies that a numeric code outside the ISO 4217 range is rejected.
    /// </summary>
    [TestMethod]
    public void Validate_WhenNumericCodeOutOfRange_ShouldThrowArgumentOutOfRangeException() =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => CurrencyInfoValidator.Validate(new CurrencyInfo("USD", 2, 0m, false, null, null, "", 1000)));

    /// <summary>
    /// Verifies that an invalid successor ISO code is rejected.
    /// </summary>
    [TestMethod]
    public void Validate_WhenSuccessorIsoInvalid_ShouldThrowArgumentException() =>
        Assert.ThrowsExactly<ArgumentException>(() => CurrencyInfoValidator.Validate(new CurrencyInfo("USD", 2, 0m, true, null, "XX")));

    /// <summary>
    /// Verifies that an active currency carrying demonetization metadata is rejected as inconsistent.
    /// </summary>
    [TestMethod]
    public void Validate_WhenNonHistoricHasDemonetizationMetadata_ShouldThrowArgumentException() =>
        Assert.ThrowsExactly<ArgumentException>(() => CurrencyInfoValidator.Validate(new CurrencyInfo("USD", 2, 0m, false, new DateOnly(2020, 1, 1), null)));

    /// <summary>
    /// Verifies that <c>TryValidate</c> rejects every invalid metadata shape without throwing.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenMetadataInvalid_ShouldReturnFalse()
    {
        Assert.IsFalse(CurrencyInfoValidator.TryValidate(null));
        Assert.IsFalse(CurrencyInfoValidator.TryValidate(new CurrencyInfo("us", 2, 0m, false, null, null)));
        Assert.IsFalse(CurrencyInfoValidator.TryValidate(new CurrencyInfo("USD", 29, 0m, false, null, null)));
        Assert.IsFalse(CurrencyInfoValidator.TryValidate(new CurrencyInfo("USD", 2, -0.05m, false, null, null)));
        Assert.IsFalse(CurrencyInfoValidator.TryValidate(new CurrencyInfo("USD", 2, 0.001m, false, null, null)));
        Assert.IsFalse(CurrencyInfoValidator.TryValidate(new CurrencyInfo("USD", 2, 0m, false, null, null, "", 1000)));
        Assert.IsFalse(CurrencyInfoValidator.TryValidate(new CurrencyInfo("USD", 2, 0m, true, null, "XX")));
        Assert.IsFalse(CurrencyInfoValidator.TryValidate(new CurrencyInfo("USD", 2, 0m, false, new DateOnly(2020, 1, 1), null)));
    }

    /// <summary>
    /// Verifies that well-formed metadata — including a valid historic record — passes <c>TryValidate</c>.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenMetadataValid_ShouldReturnTrue()
    {
        Assert.IsTrue(CurrencyInfoValidator.TryValidate(new CurrencyInfo("USD", 2, 0.05m, false, null, null, "US Dollar", 840)));
        Assert.IsTrue(CurrencyInfoValidator.TryValidate(new CurrencyInfo("ZWL", 2, 0m, true, new DateOnly(2024, 4, 8), "ZWG", "Zimbabwe Dollar", 932)));
    }
}
