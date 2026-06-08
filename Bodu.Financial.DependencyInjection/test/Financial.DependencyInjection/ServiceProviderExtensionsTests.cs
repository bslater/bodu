// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ServiceProviderExtensionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Financial;
using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Financial.DependencyInjection;

/// <summary>
/// Verifies that <see cref="ServiceProviderExtensions.UseBoduFinancialCurrencyResolution(IServiceProvider)" /> promotes
/// the container-registered <see cref="ICurrencyLookup" /> to the ambient <see cref="CurrencyResolution" /> default.
/// </summary>
[TestClass]
public sealed class ServiceProviderExtensionsTests
{
    /// <summary>
    /// Restores the registry-backed ambient default after each test so a custom lookup never leaks across cases.
    /// </summary>
    [TestCleanup]
    public void RestoreAmbientDefault() =>
        CurrencyResolution.SetDefault(new CurrencyLookupService());

    /// <summary>
    /// Verifies that the connector installs the registered lookup as the ambient resolver.
    /// </summary>
    [TestMethod]
    public void UseBoduFinancialCurrencyResolution_WhenCalled_ShouldInstallRegisteredLookupAsAmbient()
    {
        ServiceProvider provider = new ServiceCollection()
            .AddBoduFinancial(builder => builder.AddCurrencyLookup<MarkerLookup>())
            .Services
            .BuildServiceProvider();

        IServiceProvider returned = provider.UseBoduFinancialCurrencyResolution();

        Assert.AreSame(provider, returned);
        Assert.IsInstanceOfType<MarkerLookup>(CurrencyResolution.Current);
    }

    /// <summary>
    /// Verifies that the connector rejects a <see langword="null" /> provider.
    /// </summary>
    [TestMethod]
    public void UseBoduFinancialCurrencyResolution_WhenProviderIsNull_ShouldThrowArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = ((IServiceProvider)null!).UseBoduFinancialCurrencyResolution();
        });
    }

    /// <summary>
    /// A distinguishable <see cref="ICurrencyLookup" /> used to confirm the ambient default was replaced.
    /// </summary>
    private sealed class MarkerLookup
        : ICurrencyLookup
    {
        /// <inheritdoc />
        public bool TryByIsoCode(string isoCode, out CurrencyInfo currency)
        {
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
