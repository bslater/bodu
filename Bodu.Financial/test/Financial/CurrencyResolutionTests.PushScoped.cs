// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyResolutionTests.PushScoped.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Financial;

public sealed partial class CurrencyResolutionTests
{

    /// <summary>
    /// Verifies that a scoped override redirects <see cref="Money" /> construction to the supplied catalogue, so a
    /// currency known only to the override constructs while one known only to the registry does not.
    /// </summary>
    [TestMethod]
    public void PushScoped_WhenActive_ShouldRedirectMoneyConstruction()
    {
        var custom = new CurrencyInfo("ZZZ", 2, 0m, false, null, null, "Test Dollar");

        using (CurrencyResolution.PushScoped(new StubCurrencyLookup(custom)))
        {
            var money = new Money(1.239m, "ZZZ");
            Assert.AreEqual("ZZZ", money.IsoCode);
            Assert.AreEqual(2, money.MinorUnits);
            Assert.AreEqual(1.24m, money.Amount);

            // USD is absent from the scoped catalogue, so it is rejected while the scope is active.
            _ = Assert.ThrowsExactly<ArgumentException>(() =>
            {
                _ = new Money(1m, "USD");
            });
        }
    }

    /// <summary>
    /// Verifies that disposing a scope restores the previous ambient lookup.
    /// </summary>
    [TestMethod]
    public void PushScoped_WhenDisposed_ShouldRestorePreviousLookup()
    {
        using (CurrencyResolution.PushScoped(new StubCurrencyLookup()))
        {
            Assert.IsFalse(CurrencyResolution.Current.TryByIsoCode("USD", out _));
        }

        Assert.IsTrue(CurrencyResolution.Current.TryByIsoCode("USD", out _));
    }

    /// <summary>
    /// Verifies that <see cref="CurrencyResolution.PushScoped(ICurrencyLookup)" /> rejects a <see langword="null" />
    /// lookup.
    /// </summary>
    [TestMethod]
    public void PushScoped_WhenLookupIsNull_ShouldThrowArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = CurrencyResolution.PushScoped(null!);
        });
    }
}
