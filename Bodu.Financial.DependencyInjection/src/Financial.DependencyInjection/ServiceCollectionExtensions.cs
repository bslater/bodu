// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Bodu.Financial.DependencyInjection;

/// <summary>
/// Provides the <c>AddBoduFinancial</c> entry points that register the Bodu.Financial services into an
/// <see cref="IServiceCollection" /> and return a fluent <see cref="IFinancialServiceBuilder" />.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>The default <see cref="IConfiguration" /> section name bound into <see cref="FinancialOptions" />.</summary>
    public const string DefaultConfigurationSection = "Financial";

    /// <summary>
    /// Registers the core financial services — an <see cref="ICurrencyLookup" /> singleton, the financial
    /// <see cref="JsonSerializerOptions" /> and a default <see cref="MoneyParseOptions" /> applied from the bound and
    /// validated <see cref="FinancialOptions" /> — and returns a builder for further composition.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">
    /// Optional configuration root or section. When supplied, the section named <paramref name="sectionName" /> is
    /// bound into <see cref="FinancialOptions" />.
    /// </param>
    /// <param name="sectionName">
    /// The configuration section name. Defaults to <see cref="DefaultConfigurationSection" />.
    /// </param>
    /// <returns>An <see cref="IFinancialServiceBuilder" /> for further registration.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="sectionName" /> is empty or white space.</exception>
    /// <remarks>
    /// <para>
    /// The bound <see cref="FinancialOptions" /> are validated on start — the JSON policy must be a defined value — and
    /// applied to the services they govern: <see cref="FinancialOptions.JsonPolicy" /> configures the financial
    /// <see cref="JsonSerializerOptions" /> registered under
    /// <see cref="FinancialServiceBuilderExtensions.JsonOptionsKey" /> with <c>TryAdd</c>, so an explicit
    /// <see cref="FinancialServiceBuilderExtensions.AddFinancialJson(IFinancialServiceBuilder, FinancialJsonPolicy)" />
    /// takes precedence.
    /// </para>
    /// <para>
    /// No foreign-exchange provider is registered by default; supply one via
    /// <see cref="FinancialServiceBuilderExtensions.AddExchangeRateProvider{TProvider}(IFinancialServiceBuilder)" /> or
    /// its dated counterpart.
    /// </para>
    /// </remarks>
    public static IFinancialServiceBuilder AddBoduFinancial(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        string sectionName = DefaultConfigurationSection)
    {
        ThrowHelper.ThrowIfNull(services);
        ThrowHelper.ThrowIfNullOrWhiteSpace(sectionName);

        services.AddOptions();

        OptionsBuilder<FinancialOptions> optionsBuilder = services
            .AddOptions<FinancialOptions>()
            .Validate(
                static options => Enum.IsDefined(options.JsonPolicy),
                DependencyInjectionResourceStrings.Op_Invalid_FinancialOptions)
            .ValidateOnStart();

        if (configuration is not null)
            optionsBuilder.Bind(configuration.GetSection(sectionName));

        services.TryAddSingleton<ICurrencyLookup, CurrencyLookupService>();

        // Apply the bound JSON policy to the financial JSON options. TryAdd is used so an explicit
        // AddFinancialJson(...) still wins.
        services.TryAddKeyedSingleton(
            FinancialServiceBuilderExtensions.JsonOptionsKey,
            static (provider, _) => new JsonSerializerOptions()
                .AddFinancialJsonConverters(provider.GetRequiredService<IOptions<FinancialOptions>>().Value.JsonPolicy));

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
