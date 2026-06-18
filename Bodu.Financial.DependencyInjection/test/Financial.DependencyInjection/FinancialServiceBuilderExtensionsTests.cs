// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FinancialServiceBuilderExtensionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial;
using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Financial.DependencyInjection;

/// <summary>
/// Verifies the fluent registration surface exposed by <see cref="FinancialServiceBuilderExtensions" />.
/// </summary>
[TestClass]
public sealed partial class FinancialServiceBuilderExtensionsTests
{
    /// <summary>
    /// A stub <see cref="IExchangeRateProvider" /> for registration tests.
    /// </summary>
    private sealed class StubRateProvider
        : IExchangeRateProvider
    {
        /// <inheritdoc />
        public decimal GetRate(string fromIsoCode, string toIsoCode) => 1m;
    }

    /// <summary>
    /// An alternative <see cref="ICurrencyLookup" /> used to verify replacement. All members are unsupported; the type
    /// exists only to confirm the registered implementation type.
    /// </summary>
    private sealed class CustomLookup
        : ICurrencyLookup
    {
        /// <inheritdoc />
        public bool TryByIsoCode(string isoCode, out CurrencyInfo currency) => throw new NotSupportedException();

        /// <inheritdoc />
        public bool TryByCurrencyCode(Bodu.Financial.Currencies.CurrencyCode code, out CurrencyInfo currency) => throw new NotSupportedException();

        /// <inheritdoc />
        public bool TryByNumericCode(int numericCode, out CurrencyInfo currency) => throw new NotSupportedException();

        /// <inheritdoc />
        public bool TryBySymbol(string symbol, out IReadOnlyList<CurrencyInfo> matches) => throw new NotSupportedException();

        /// <inheritdoc />
        public bool TryByRegion(string regionCode, out IReadOnlyList<CurrencyInfo> matches) => throw new NotSupportedException();

        /// <inheritdoc />
        public IReadOnlyList<CurrencyInfo> ByCulture(System.Globalization.CultureInfo culture) => throw new NotSupportedException();
    }
}
