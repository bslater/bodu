// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YahooServiceCollectionExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu;
using Bodu.Financial;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Provides a one-call entry point that registers the core Bodu.Financial services together with the Yahoo Finance
/// exchange-rate provider.
/// </summary>
public static class YahooServiceCollectionExtensions
{
    /// <summary>
    /// Registers the core Bodu.Financial services and the Yahoo Finance exchange-rate provider.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">
    /// An optional configuration root. When supplied, <see cref="Bodu.Financial.FinancialOptions" /> is bound from the
    /// <c>Financial</c> section and <see cref="YahooExchangeRateOptions" /> from <paramref name="sectionName" />.
    /// </param>
    /// <param name="sectionName">The Yahoo configuration section name. Defaults to <c>Financial:Yahoo</c>.</param>
    /// <param name="configure">An optional callback applied after Yahoo configuration binding.</param>
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
    /// services.AddYahooExchangeRates(configuration, configure: options =>
    /// {
    ///     options.UserAgent = "my-app/1.0";
    ///     options.HttpTimeout = TimeSpan.FromSeconds(15);
    /// });
    ///
    /// ServiceProvider provider = services.BuildServiceProvider();
    /// var rates = provider.GetRequiredService<IDatedExchangeRateProvider>();
    ///]]>
    /// </code>
    /// </example>
    public static IFinancialServiceBuilder AddYahooExchangeRates(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        string sectionName = "Financial:Yahoo",
        Action<YahooExchangeRateOptions>? configure = null)
    {
        ThrowHelper.ThrowIfNull(services);

        return services
            .AddFinancialService(configuration)
            .AddYahooExchangeRates(configuration, sectionName, configure);
    }
}
