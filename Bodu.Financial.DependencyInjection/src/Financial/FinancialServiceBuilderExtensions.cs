// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FinancialServiceBuilderExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Currencies;
using Bodu.Financial.DependencyInjection;
using Bodu.Financial.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bodu.Financial;

/// <summary>
/// Provides the fluent registration surface for <see cref="IFinancialServiceBuilder" />: currency lookup, named
/// monetary contexts, exchange-rate providers, and JSON policy.
/// </summary>
/// <remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// IServiceCollection services = new ServiceCollection();
///
/// services.AddFinancialService(builder => builder
///     .AddCurrencyLookup<MyCurrencyLookup>()
///     .AddMonetaryContext("payroll", new MonetaryContext(CurrencyCode.EUR))
///     .AddExchangeRateProvider<MyRateProvider>()
///     .AddFinancialJson(FinancialJsonPolicy.Compact));
///]]>
/// </code>
/// </example>
/// </remarks>
public static class FinancialServiceBuilderExtensions
{
    /// <summary>The service key under which the configured financial <see cref="JsonSerializerOptions" /> is registered.</summary>
    public const string JsonOptionsKey = "Financial";

    /// <summary>
    /// Replaces the registered <see cref="ICurrencyLookup" /> with <typeparamref name="TLookup" />.
    /// </summary>
    /// <typeparam name="TLookup">The lookup implementation.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns>The builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
    public static IFinancialServiceBuilder AddCurrencyLookup<TLookup>(this IFinancialServiceBuilder builder)
        where TLookup : class, ICurrencyLookup
    {
        ThrowHelper.ThrowIfNull(builder);

        builder.Services.Replace(ServiceDescriptor.Singleton<ICurrencyLookup, TLookup>());
        return builder;
    }

    /// <summary>
    /// Registers a named <see cref="MonetaryContext" /> resolvable as a keyed singleton.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="name">The key under which the context is registered.</param>
    /// <param name="context">The monetary context.</param>
    /// <returns>The builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder" /> or <paramref name="context" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="name" /> is empty or white space.</exception>
    public static IFinancialServiceBuilder AddMonetaryContext(this IFinancialServiceBuilder builder, string name, MonetaryContext context)
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNull(context);
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(DependencyInjectionResourceStrings.Arg_Invalid_MonetaryContextNameBlank, nameof(name));

        builder.Services.AddKeyedSingleton(name, context);
        return builder;
    }

    /// <summary>
    /// Registers an <see cref="IExchangeRateProvider" /> implementation as a singleton.
    /// </summary>
    /// <typeparam name="TProvider">The provider implementation.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns>The builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
    public static IFinancialServiceBuilder AddExchangeRateProvider<TProvider>(this IFinancialServiceBuilder builder)
        where TProvider : class, IExchangeRateProvider
    {
        ThrowHelper.ThrowIfNull(builder);

        builder.Services.TryAddSingleton<IExchangeRateProvider, TProvider>();
        return builder;
    }

    /// <summary>
    /// Registers an <see cref="IExchangeRateProvider" /> instance as a singleton.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="provider">The provider instance.</param>
    /// <returns>The builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder" /> or <paramref name="provider" /> is <see langword="null" />.
    /// </exception>
    public static IFinancialServiceBuilder AddExchangeRateProvider(this IFinancialServiceBuilder builder, IExchangeRateProvider provider)
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNull(provider);

        builder.Services.TryAddSingleton(provider);
        return builder;
    }

    /// <summary>
    /// Registers an <see cref="IDatedExchangeRateProvider" /> implementation as a singleton.
    /// </summary>
    /// <typeparam name="TProvider">The provider implementation.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns>The builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
    public static IFinancialServiceBuilder AddDatedExchangeRateProvider<TProvider>(this IFinancialServiceBuilder builder)
        where TProvider : class, IDatedExchangeRateProvider
    {
        ThrowHelper.ThrowIfNull(builder);

        builder.Services.TryAddSingleton<IDatedExchangeRateProvider, TProvider>();
        return builder;
    }

    /// <summary>
    /// Registers an <see cref="IDatedExchangeRateProvider" /> instance as a singleton.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="provider">The provider instance.</param>
    /// <returns>The builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder" /> or <paramref name="provider" /> is <see langword="null" />.
    /// </exception>
    public static IFinancialServiceBuilder AddDatedExchangeRateProvider(this IFinancialServiceBuilder builder, IDatedExchangeRateProvider provider)
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNull(provider);

        builder.Services.TryAddSingleton(provider);
        return builder;
    }

    /// <summary>
    /// Registers a <see cref="JsonSerializerOptions" /> configured with the financial converters under the
    /// <paramref name="policy" />, keyed by <see cref="JsonOptionsKey" />.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="policy">The serialization policy.</param>
    /// <returns>The builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
    public static IFinancialServiceBuilder AddFinancialJson(this IFinancialServiceBuilder builder, FinancialJsonPolicy policy = FinancialJsonPolicy.Strict)
    {
        ThrowHelper.ThrowIfNull(builder);

        builder.Services.AddKeyedSingleton(JsonOptionsKey, (_, _) => new JsonSerializerOptions().AddFinancialJsonConverters(policy));
        return builder;
    }
}
