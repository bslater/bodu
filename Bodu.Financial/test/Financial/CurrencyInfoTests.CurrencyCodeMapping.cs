// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyInfoTests.CurrencyCodeMapping.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial;

public partial class CurrencyInfoTests
{
    /// <summary>
    /// Verifies that <see cref="CurrencyInfo.FromCurrencyCode" /> returns the registered metadata when the supplied
    /// enum value is a defined active currency.
    /// </summary>
    [TestMethod]
    public void FromCurrencyCode_WhenCodeIsDefined_ShouldReturnRegisteredInfo()
    {
        var info = CurrencyInfo.FromCurrencyCode(CurrencyCode.AUD);

        Assert.AreEqual("AUD", info.IsoCode);
        Assert.AreEqual(36, info.NumericCode);
        Assert.AreEqual(2, info.MinorUnits);
    }

    /// <summary>
    /// Verifies that <see cref="CurrencyInfo.FromCurrencyCode" /> throws <see cref="ArgumentOutOfRangeException" />
    /// when the supplied enum value is not a defined member (e.g. cast from an arbitrary integer).
    /// </summary>
    [TestMethod]
    public void FromCurrencyCode_WhenCodeIsUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = CurrencyInfo.FromCurrencyCode((CurrencyCode)9999);
        });

        Assert.AreEqual("code", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="CurrencyInfo.TryGetCurrencyCode" /> returns <see langword="true" /> and produces the
    /// matching enum member for an active ISO 4217 alphabetic code.
    /// </summary>
    [TestMethod]
    public void TryGetCurrencyCode_WhenIsoIsActive_ShouldReturnTrueAndCode()
    {
        var ok = CurrencyInfo.TryGetCurrencyCode("USD", out CurrencyCode code);

        Assert.IsTrue(ok);
        Assert.AreEqual(CurrencyCode.USD, code);
    }

    /// <summary>
    /// Verifies that <see cref="CurrencyInfo.TryGetCurrencyCode" /> returns <see langword="false" /> for historic
    /// currencies that are intentionally excluded from <see cref="CurrencyCode" /> to preserve enum stability.
    /// </summary>
    [TestMethod]
    public void TryGetCurrencyCode_WhenIsoIsHistoric_ShouldReturnFalse()
    {
        var ok = CurrencyInfo.TryGetCurrencyCode("DEM", out CurrencyCode code);

        Assert.IsFalse(ok);
        Assert.AreEqual(default, code);
    }

    /// <summary>
    /// Verifies that <see cref="CurrencyInfo.TryGetCurrencyCode" /> is case-sensitive and rejects lowercase variants
    /// of an otherwise active ISO code.
    /// </summary>
    [TestMethod]
    public void TryGetCurrencyCode_WhenIsoIsLowerCase_ShouldReturnFalse()
    {
        var ok = CurrencyInfo.TryGetCurrencyCode("usd", out CurrencyCode code);

        Assert.IsFalse(ok);
        Assert.AreEqual(default, code);
    }

    /// <summary>
    /// Verifies that <see cref="CurrencyInfo.ToCurrencyCode" /> returns the matching enum member when the instance
    /// represents an active ISO 4217 currency.
    /// </summary>
    [TestMethod]
    public void ToCurrencyCode_WhenIsoIsActive_ShouldReturnMatchingCode()
    {
        CurrencyInfo info = CurrencyRegistry.Get("EUR");

        var code = info.ToCurrencyCode();

        Assert.AreEqual(CurrencyCode.EUR, code);
    }

    /// <summary>
    /// Verifies that <see cref="CurrencyInfo.ToCurrencyCode" /> throws <see cref="InvalidOperationException" /> for
    /// historic currencies because they are not represented in <see cref="CurrencyCode" />.
    /// </summary>
    [TestMethod]
    public void ToCurrencyCode_WhenIsoIsHistoric_ShouldThrowInvalidOperationException()
    {
        CurrencyInfo info = CurrencyRegistry.Get("DEM");

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = info.ToCurrencyCode();
        });
    }
}
