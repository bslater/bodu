// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OfxServiceCollectionExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Financial.ExchangeRates.Ofx.DependencyInjection;

/// <summary>
/// Provides a one-call entry point that registers the core Bodu.Financial services together with the OFX exchange-rate
/// provider.
/// </summary>
public static class OfxServiceCollectionExtensions
{
    /// <summary>
    /// Registers the core Bodu.Financial services and the OFX exchange-rate provider.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">
    /// An optional configuration root. When supplied,
    /// <see cref="Bodu.Financial.DependencyInjection.FinancialOptions" /> is bound from the <c>Financial</c> section
    /// and <see cref="OfxExchangeRateOptions" /> from <paramref name="sectionName" />.
    /// </param>
    /// <param name="sectionName">The OFX configuration section name. Defaults to <c>Financial:Ofx</c>.</param>
    /// <param name="configure">An optional callback applied after OFX configuration binding.</param>
    /// <returns>An <see cref="IFinancialServiceBuilder" /> for further composition.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="sectionName" /> is empty or white space.
    /// </exception>
    public static IFinancialServiceBuilder AddOfxExchangeRates(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        string sectionName = "Financial:Ofx",
        Action<OfxExchangeRateOptions>? configure = null)
    {
        ThrowHelper.ThrowIfNull(services);

        return services
            .AddFinancialService(configuration)
            .AddOfxExchangeRates(configuration, sectionName, configure);
    }
}
