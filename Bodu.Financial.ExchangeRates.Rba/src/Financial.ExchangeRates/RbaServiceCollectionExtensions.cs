// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaServiceCollectionExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Provides a one-call entry point that registers the core Bodu.Financial services together with the RBA historical
/// exchange-rate provider.
/// </summary>
public static class RbaServiceCollectionExtensions
{
    /// <summary>
    /// Registers the core Bodu.Financial services and the RBA historical exchange-rate provider.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">
    /// An optional configuration root. When supplied, <see cref="FinancialOptions" /> is bound from the
    /// <c>Financial</c> section and <see cref="RbaExchangeRateOptions" /> from <paramref name="sectionName" />.
    /// </param>
    /// <param name="sectionName">The RBA configuration section name. Defaults to <c>Financial:Rba</c>.</param>
    /// <param name="configure">An optional callback applied after RBA configuration binding.</param>
    /// <returns>An <see cref="IFinancialServiceBuilder" /> for further composition.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="sectionName" /> is empty or white space.
    /// </exception>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// IServiceCollection services = new ServiceCollection();
    ///
    /// services.AddRbaHistoricalRates(configuration, configure: options =>
    /// {
    ///     options.EnableDiskCache = true;
    ///     options.AllowSynchronousNetworkAccess = false;
    /// });
    ///
    /// // Resolve the provider (or IExchangeRateProvider / IDatedExchangeRateProvider) from the container.
    /// using ServiceProvider provider = services.BuildServiceProvider();
    /// var rba = provider.GetRequiredService<RbaExchangeRateProvider>();
    ///]]>
    /// </code>
    /// </example>
    public static IFinancialServiceBuilder AddRbaHistoricalRates(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        string sectionName = "Financial:Rba",
        Action<RbaExchangeRateOptions>? configure = null)
    {
        ThrowHelper.ThrowIfNull(services);

        return services
            .AddFinancialService(configuration)
            .AddRbaHistoricalRates(configuration, sectionName, configure);
    }
}
