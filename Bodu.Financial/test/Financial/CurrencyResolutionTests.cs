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
public sealed partial class CurrencyResolutionTests
{

    /// <summary>
    /// A minimal <see cref="ICurrencyLookup" /> that resolves only the ISO codes supplied at construction.
    /// </summary>
    private sealed class StubCurrencyLookup
        : ICurrencyLookup
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
