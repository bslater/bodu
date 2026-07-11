// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateHostServiceCollectionExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Provides a one-call entry point that registers the core Bodu.Financial services together with the exchangerate.host
/// exchange-rate provider.
/// </summary>
public static class ExchangeRateHostServiceCollectionExtensions
{
    /// <summary>
    /// Registers the core Bodu.Financial services and the exchangerate.host exchange-rate provider.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">
    /// An optional configuration root. When supplied, <see cref="Bodu.Financial.FinancialOptions" /> is bound from the
    /// <c>Financial</c> section and <see cref="ExchangeRateHostRateProviderOptions" /> from
    /// <paramref name="sectionName" />.
    /// </param>
    /// <param name="sectionName">
    /// The exchangerate.host configuration section name. Defaults to <c>Financial:ExchangeRateHost</c>.
    /// </param>
    /// <param name="configure">An optional callback applied after exchangerate.host configuration binding.</param>
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
    /// services.AddExchangeRateHostExchangeRates(configuration, configure: options => options.ApiKey = "…");
    ///
    /// ServiceProvider provider = services.BuildServiceProvider();
    /// var rates = provider.GetRequiredService<IDatedRateProvider>();
    ///]]>
    /// </code>
    /// </example>
    public static IFinancialServiceBuilder AddExchangeRateHostExchangeRates(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        string sectionName = "Financial:ExchangeRateHost",
        Action<ExchangeRateHostRateProviderOptions>? configure = null)
    {
        ThrowHelper.ThrowIfNull(services);

        return services
            .AddFinancialService(configuration)
            .AddExchangeRateHostExchangeRates(configuration, sectionName, configure);
    }
}
