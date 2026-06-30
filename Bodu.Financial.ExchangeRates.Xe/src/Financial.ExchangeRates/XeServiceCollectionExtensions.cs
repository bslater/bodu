// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XeServiceCollectionExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Provides a one-call entry point that registers the core Bodu.Financial services together with the XE.com
/// exchange-rate provider.
/// </summary>
public static class XeServiceCollectionExtensions
{
    /// <summary>
    /// Registers the core Bodu.Financial services and the XE.com exchange-rate provider.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">
    /// An optional configuration root. When supplied, <see cref="Bodu.Financial.FinancialOptions" /> is bound from the
    /// <c>Financial</c> section and <see cref="XeExchangeRateOptions" /> from <paramref name="sectionName" />.
    /// </param>
    /// <param name="sectionName">The XE configuration section name. Defaults to <c>Financial:Xe</c>.</param>
    /// <param name="configure">An optional callback applied after XE configuration binding.</param>
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
    /// services.AddXeExchangeRates(configuration, configure: options =>
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
    public static IFinancialServiceBuilder AddXeExchangeRates(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        string sectionName = "Financial:Xe",
        Action<XeExchangeRateOptions>? configure = null)
    {
        ThrowHelper.ThrowIfNull(services);

        return services
            .AddFinancialService(configuration)
            .AddXeExchangeRates(configuration, sectionName, configure);
    }
}
