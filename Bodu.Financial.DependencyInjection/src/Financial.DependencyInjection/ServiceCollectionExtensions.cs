// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bodu.Financial.DependencyInjection;

/// <summary>
/// Provides the <c>AddBoduFinancial</c> entry points that register the Bodu.Financial services into an
/// <see cref="IServiceCollection" /> and return a fluent <see cref="IFinancialServiceBuilder" />.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// The default <see cref="IConfiguration" /> section name bound into <see cref="FinancialOptions" />.
    /// </summary>
    public const string DefaultConfigurationSection = "Financial";

    /// <summary>
    /// Registers the core financial services — an <see cref="ICurrencyLookup" /> singleton and bound
    /// <see cref="FinancialOptions" /> — and returns a builder for further composition.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">
    /// Optional configuration root or section. When supplied, the section named <paramref name="sectionName" /> is
    /// bound into <see cref="FinancialOptions" />.
    /// </param>
    /// <param name="sectionName">The configuration section name. Defaults to <see cref="DefaultConfigurationSection" />.</param>
    /// <returns>An <see cref="IFinancialServiceBuilder" /> for further registration.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="sectionName" /> is empty or white space.</exception>
    /// <remarks>
    /// No foreign-exchange provider is registered by default; supply one via
    /// <see cref="FinancialServiceBuilderExtensions.AddExchangeRateProvider{TProvider}(IFinancialServiceBuilder)" /> or
    /// its dated counterpart.
    /// </remarks>
    public static IFinancialServiceBuilder AddBoduFinancial(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        string sectionName = DefaultConfigurationSection)
    {
        ThrowHelper.ThrowIfNull(services);
        ThrowHelper.ThrowIfNullOrWhiteSpace(sectionName);

        services.AddOptions();

        if (configuration is not null)
        {
            services
                .AddOptions<FinancialOptions>()
                .Bind(configuration.GetSection(sectionName));
        }

        services.TryAddSingleton<ICurrencyLookup, CurrencyLookupService>();

        return new FinancialServiceBuilder(services);
    }

    /// <summary>
    /// Registers the core financial services and invokes <paramref name="configure" /> for programmatic composition.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configure">A callback invoked with the <see cref="IFinancialServiceBuilder" />.</param>
    /// <returns>The same builder the callback received.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="services" /> or <paramref name="configure" /> is <see langword="null" />.
    /// </exception>
    public static IFinancialServiceBuilder AddBoduFinancial(
        this IServiceCollection services,
        Action<IFinancialServiceBuilder> configure)
    {
        ThrowHelper.ThrowIfNull(services);
        ThrowHelper.ThrowIfNull(configure);

        IFinancialServiceBuilder builder = services.AddBoduFinancial();
        configure(builder);
        return builder;
    }
}
