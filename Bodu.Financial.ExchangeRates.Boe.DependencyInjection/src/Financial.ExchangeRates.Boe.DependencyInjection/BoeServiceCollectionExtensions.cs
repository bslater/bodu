// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeServiceCollectionExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Financial.ExchangeRates.Boe.DependencyInjection;

/// <summary>
/// Provides a one-call entry point that registers the core Bodu.Financial services together with the Bank of England
/// exchange-rate provider.
/// </summary>
public static class BoeServiceCollectionExtensions
{
    /// <summary>
    /// Registers the core Bodu.Financial services and the Bank of England exchange-rate provider.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">
    /// An optional configuration root. When supplied, <see cref="FinancialOptions" /> is bound from the
    /// <c>Financial</c> section and <see cref="BoeExchangeRateOptions" /> from <paramref name="sectionName" />.
    /// </param>
    /// <param name="sectionName">
    /// The Bank of England configuration section name. Defaults to <c>Financial:Boe</c>.
    /// </param>
    /// <param name="configure">An optional callback applied after Bank of England configuration binding.</param>
    /// <returns>An <see cref="IFinancialServiceBuilder" /> for further composition.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="sectionName" /> is empty or white space.
    /// </exception>
    public static IFinancialServiceBuilder AddBoeReferenceRates(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        string sectionName = "Financial:Boe",
        Action<BoeExchangeRateOptions>? configure = null)
    {
        ThrowHelper.ThrowIfNull(services);

        return services
            .AddFinancialService(configuration)
            .AddBoeReferenceRates(configuration, sectionName, configure);
    }
}
