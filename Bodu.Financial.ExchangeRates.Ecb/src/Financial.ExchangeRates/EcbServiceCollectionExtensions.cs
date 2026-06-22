// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbServiceCollectionExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu;
using Bodu.Financial;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Provides a one-call entry point that registers the core Bodu.Financial services together with the ECB euro
/// reference-rate provider.
/// </summary>
public static class EcbServiceCollectionExtensions
{
    /// <summary>
    /// Registers the core Bodu.Financial services and the ECB euro reference-rate provider.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">
    /// An optional configuration root. When supplied, <see cref="FinancialOptions" /> is bound from the
    /// <c>Financial</c> section and <see cref="EcbExchangeRateOptions" /> from <paramref name="sectionName" />.
    /// </param>
    /// <param name="sectionName">The ECB configuration section name. Defaults to <c>Financial:Ecb</c>.</param>
    /// <param name="configure">An optional callback applied after ECB configuration binding.</param>
    /// <returns>An <see cref="IFinancialServiceBuilder" /> for further composition.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="sectionName" /> is empty or white space.
    /// </exception>
    public static IFinancialServiceBuilder AddEcbReferenceRates(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        string sectionName = "Financial:Ecb",
        Action<EcbExchangeRateOptions>? configure = null)
    {
        ThrowHelper.ThrowIfNull(services);

        return services
            .AddFinancialService(configuration)
            .AddEcbReferenceRates(configuration, sectionName, configure);
    }
}
