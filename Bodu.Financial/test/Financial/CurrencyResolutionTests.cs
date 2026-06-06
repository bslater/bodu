// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyResolutionTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Financial;

/// <summary>
/// Verifies the ambient <see cref="CurrencyResolution" /> seam: the default resolves the shipped catalogue, a scoped
/// override redirects runtime <see cref="Money" /> currency resolution, and disposing the scope restores the prior
/// lookup.
/// </summary>
[TestClass]
public sealed class CurrencyResolutionTests
{
    /// <summary>
    /// Verifies that the default ambient lookup resolves a shipped currency, matching a direct registry lookup.
    /// </summary>
    [TestMethod]
    public void Current_WhenNoScope_ShouldResolveShippedCurrency()
    {
        Assert.IsTrue(CurrencyResolution.Current.TryByIsoCode("USD", out CurrencyInfo usd));
        Assert.AreEqual("USD", usd.IsoCode);
    }

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

    /// <summary>
    /// A minimal <see cref="ICurrencyLookup" /> that resolves only the ISO codes supplied at construction.
    /// </summary>
    private sealed class StubCurrencyLookup : ICurrencyLookup
    {
        /// <summary>
        /// The currencies this stub knows, keyed by ISO code.
        /// </summary>
        private readonly Dictionary<string, CurrencyInfo> _byIso;

        /// <summary>
        /// Initializes a new instance of the <see cref="StubCurrencyLookup" /> class.
        /// </summary>
        /// <param name="currencies">The currencies the stub should resolve.</param>
        public StubCurrencyLookup(params CurrencyInfo[] currencies) =>
            _byIso = currencies.ToDictionary(c => c.IsoCode, StringComparer.Ordinal);

        /// <inheritdoc />
        public bool TryByIsoCode(string isoCode, out CurrencyInfo currency)
        {
            if (isoCode is not null && _byIso.TryGetValue(isoCode, out CurrencyInfo? found))
            {
                currency = found;
                return true;
            }

            currency = null!;
            return false;
        }

        /// <inheritdoc />
        public bool TryByCurrencyCode(Currencies.CurrencyCode code, out CurrencyInfo currency)
        {
            currency = null!;
            return false;
        }

        /// <inheritdoc />
        public bool TryByNumericCode(int numericCode, out CurrencyInfo currency)
        {
            currency = null!;
            return false;
        }

        /// <inheritdoc />
        public bool TryBySymbol(string symbol, out IReadOnlyList<CurrencyInfo> matches)
        {
            matches = Array.Empty<CurrencyInfo>();
            return false;
        }

        /// <inheritdoc />
        public bool TryByRegion(string regionCode, out IReadOnlyList<CurrencyInfo> matches)
        {
            matches = Array.Empty<CurrencyInfo>();
            return false;
        }

        /// <inheritdoc />
        public IReadOnlyList<CurrencyInfo> ByCulture(CultureInfo culture) =>
            Array.Empty<CurrencyInfo>();
    }
}
