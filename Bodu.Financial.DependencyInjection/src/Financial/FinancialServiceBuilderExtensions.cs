// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FinancialServiceBuilderExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Financial.DependencyInjection;
using Bodu.Financial.ExchangeRates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bodu.Financial;

/// <summary>
/// Provides the fluent registration surface for <see cref="IFinancialServiceBuilder" />: currency lookup, named
/// monetary contexts, and exchange-rate providers.
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
///     .AddExchangeRateProvider<MyRateProvider>());
///]]>
/// </code>
/// </example>
/// </remarks>
public static class FinancialServiceBuilderExtensions
{
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
    /// Registers an <see cref="IRateProvider" /> implementation as a singleton.
    /// </summary>
    /// <typeparam name="TProvider">The provider implementation.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns>The builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
    public static IFinancialServiceBuilder AddExchangeRateProvider<TProvider>(this IFinancialServiceBuilder builder)
        where TProvider : class, IRateProvider
    {
        ThrowHelper.ThrowIfNull(builder);

        builder.Services.TryAddSingleton<IRateProvider, TProvider>();
        return builder;
    }

    /// <summary>
    /// Registers an <see cref="IRateProvider" /> instance as a singleton.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="provider">The provider instance.</param>
    /// <returns>The builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder" /> or <paramref name="provider" /> is <see langword="null" />.
    /// </exception>
    public static IFinancialServiceBuilder AddExchangeRateProvider(this IFinancialServiceBuilder builder, IRateProvider provider)
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNull(provider);

        builder.Services.TryAddSingleton(provider);
        return builder;
    }

    /// <summary>
    /// Registers an <see cref="IDatedRateProvider" /> implementation as a singleton.
    /// </summary>
    /// <typeparam name="TProvider">The provider implementation.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns>The builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
    public static IFinancialServiceBuilder AddDatedExchangeRateProvider<TProvider>(this IFinancialServiceBuilder builder)
        where TProvider : class, IDatedRateProvider
    {
        ThrowHelper.ThrowIfNull(builder);

        builder.Services.TryAddSingleton<IDatedRateProvider, TProvider>();
        return builder;
    }

    /// <summary>
    /// Registers an <see cref="IDatedRateProvider" /> instance as a singleton.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="provider">The provider instance.</param>
    /// <returns>The builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder" /> or <paramref name="provider" /> is <see langword="null" />.
    /// </exception>
    public static IFinancialServiceBuilder AddDatedExchangeRateProvider(this IFinancialServiceBuilder builder, IDatedRateProvider provider)
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNull(provider);

        builder.Services.TryAddSingleton(provider);
        return builder;
    }
}
