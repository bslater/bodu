// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyResolutionTests.PushScoped.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public sealed partial class CurrencyResolutionTests
{

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
