// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyInfoTests.TryGetCurrencyCode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


namespace Bodu.Financial.Currencies;

/// <summary>
/// Verifies that <see cref="CurrencyInfo.TryGetCurrencyCode(string, out CurrencyCode)" /> honours the ISO 4217
/// alphabetic-shape contract and does not resolve the numeric underlying-value form.
/// </summary>
public partial class CurrencyInfoTests
{
    /// <summary>
    /// Verifies that a canonical three-letter ISO code resolves to its currency.
    /// </summary>
    [TestMethod]
    public void TryGetCurrencyCode_WhenAlphabeticCode_ShouldResolve()
    {
        Assert.IsTrue(CurrencyInfo.TryGetCurrencyCode("USD", out CurrencyCode code));
        Assert.AreEqual(CurrencyCode.USD, code);
    }

    /// <summary>
    /// Verifies that the numeric underlying-value form of a currency code (for example <c>"840"</c> for USD) is
    /// rejected, rather than bypassing the alphabetic-shape contract and silently resolving to a currency.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void TryGetCurrencyCode_WhenNumericString_ShouldReturnFalse()
    {
        Assert.IsFalse(CurrencyInfo.TryGetCurrencyCode("840", out CurrencyCode code));
        Assert.AreEqual(CurrencyCode.None, code);

        Assert.IsFalse(CurrencyInfo.TryGetCurrencyCode("036", out _));
    }
}
