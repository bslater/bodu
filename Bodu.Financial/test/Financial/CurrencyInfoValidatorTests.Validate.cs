// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyInfoValidatorTests.Validate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public partial class CurrencyInfoValidatorTests
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
}
