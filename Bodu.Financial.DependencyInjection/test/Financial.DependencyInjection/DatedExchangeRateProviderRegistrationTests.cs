// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DatedExchangeRateProviderRegistrationTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial;
using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Financial.DependencyInjection;

/// <summary>
/// Verifies the <see cref="IDatedExchangeRateProvider" /> registration overloads exposed by
/// <see cref="FinancialServiceBuilderExtensions" />.
/// </summary>
[TestClass]
public sealed class DatedExchangeRateProviderRegistrationTests
{
    /// <summary>
    /// A stub <see cref="IDatedExchangeRateProvider" /> used only to confirm registration and resolution.
    /// </summary>
    private sealed class StubDatedRateProvider
        : IDatedExchangeRateProvider
    {
        /// <inheritdoc />
        public ExchangeRateLookupResult GetRate(string fromIsoCode, string toIsoCode, ExchangeRateLookupOptions? options = null) =>
            throw new NotSupportedException();

        /// <inheritdoc />
        public ExchangeRateLookupResult GetRate(string fromIsoCode, string toIsoCode, DateOnly date, ExchangeRateLookupOptions? options = null) =>
            throw new NotSupportedException();

        /// <inheritdoc />
        public bool TryGetRate(string fromIsoCode, string toIsoCode, DateOnly date, ExchangeRateLookupOptions? options, out ExchangeRateLookupResult result) =>
            throw new NotSupportedException();

        /// <inheritdoc />
        public ExchangeRateRangeResult GetRates(string fromIsoCode, string toIsoCode, DateOnly startDate, DateOnly endDate) =>
            throw new NotSupportedException();

        /// <inheritdoc />
        public ValueTask<ExchangeRateLookupResult> GetRateAsync(string fromIsoCode, string toIsoCode, ExchangeRateLookupOptions? options = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        /// <inheritdoc />
        public ValueTask<ExchangeRateLookupResult> GetRateAsync(string fromIsoCode, string toIsoCode, DateOnly date, ExchangeRateLookupOptions? options = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        /// <inheritdoc />
        public ValueTask<ExchangeRateRangeResult> GetRatesAsync(string fromIsoCode, string toIsoCode, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    /// <summary>
    /// Verifies that the generic overload registers the provider type as a resolvable singleton.
    /// </summary>
    [TestMethod]
    public void AddDatedExchangeRateProvider_WhenType_ShouldRegisterResolvableProvider()
    {
        IFinancialServiceBuilder builder = new ServiceCollection().AddFinancialService();

        builder.AddDatedExchangeRateProvider<StubDatedRateProvider>();

        using ServiceProvider provider = builder.Services.BuildServiceProvider();
        Assert.IsInstanceOfType<StubDatedRateProvider>(provider.GetRequiredService<IDatedExchangeRateProvider>());
    }

    /// <summary>
    /// Verifies that the instance overload registers the supplied provider as a resolvable singleton.
    /// </summary>
    [TestMethod]
    public void AddDatedExchangeRateProvider_WhenInstance_ShouldRegisterSameInstance()
    {
        StubDatedRateProvider stub = new();
        IFinancialServiceBuilder builder = new ServiceCollection().AddFinancialService();

        builder.AddDatedExchangeRateProvider(stub);

        using ServiceProvider provider = builder.Services.BuildServiceProvider();
        Assert.AreSame(stub, provider.GetRequiredService<IDatedExchangeRateProvider>());
    }
}
