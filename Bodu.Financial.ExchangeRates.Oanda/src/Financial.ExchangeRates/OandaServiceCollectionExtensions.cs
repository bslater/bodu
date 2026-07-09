// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OandaServiceCollectionExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Provides a one-call entry point that registers the core Bodu.Financial services together with the OANDA
/// exchange-rate provider.
/// </summary>
public static class OandaServiceCollectionExtensions
{
    /// <summary>
    /// Registers the core Bodu.Financial services and the OANDA exchange-rate provider.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">
    /// An optional configuration root. When supplied, <see cref="Bodu.Financial.FinancialOptions" /> is bound from the
    /// <c>Financial</c> section and <see cref="OandaRateProviderOptions" /> from <paramref name="sectionName" />.
    /// </param>
    /// <param name="sectionName">The OANDA configuration section name. Defaults to <c>Financial:Oanda</c>.</param>
    /// <param name="configure">An optional callback applied after OANDA configuration binding.</param>
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
    /// // Register the core financial services and the OANDA provider in one call, tuning the
    /// // OANDA options inline rather than binding them from configuration.
    /// services.AddOandaExchangeRates(configure: options =>
    /// {
    ///     options.Price = "mid";
    ///     options.HttpTimeout = TimeSpan.FromSeconds(15);
    /// });
    ///]]>
    /// </code>
    /// </example>
    public static IFinancialServiceBuilder AddOandaExchangeRates(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        string sectionName = "Financial:Oanda",
        Action<OandaRateProviderOptions>? configure = null)
    {
        ThrowHelper.ThrowIfNull(services);

        return services
            .AddFinancialService(configuration)
            .AddOandaExchangeRates(configuration, sectionName, configure);
    }
}
