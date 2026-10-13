// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ServiceProviderExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Financial;

/// <summary>
/// Provides the connector that promotes the container-registered <see cref="ICurrencyLookup" /> to the ambient
/// <see cref="CurrencyResolution" /> default, so the runtime-tagged <see cref="Money" /> reads currency metadata
/// through the application's configured catalog.
/// </summary>
public static class ServiceProviderExtensions
{
    /// <summary>
    /// Installs the container's <see cref="ICurrencyLookup" /> as the process-wide ambient currency resolver. Call once
    /// at start-up, after the service provider is built, when a custom lookup should back the currency metadata runtime
    /// <see cref="Money" /> reports and the ISO codes its parsers accept.
    /// </summary>
    /// <param name="provider">The built service provider.</param>
    /// <returns>The same <paramref name="provider" />, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="provider" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">
    /// No <see cref="ICurrencyLookup" /> is registered, for example because <c>AddFinancialService</c> was not called.
    /// </exception>
    /// <remarks>
    /// This is a composition-root operation: it mutates process-wide state via
    /// <see cref="CurrencyResolution.SetDefault(ICurrencyLookup)" />. Omitting the call leaves the registry-backed
    /// default in place, so existing applications behave identically without it. Construction of a runtime
    /// <see cref="Money" /> always takes its precision from the shipped <see cref="CurrencyRegistry" />, whichever
    /// lookup is installed.
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// IServiceProvider provider = services
    ///     .AddFinancialService(b => b.AddCurrencyLookup<MyCurrencyLookup>())
    ///     .Services
    ///     .BuildServiceProvider();
    ///
    /// // Make the container's ICurrencyLookup back runtime Money metadata and parsing.
    /// provider.UseCurrencyResolution();
    ///]]>
    /// </code>
    /// </example>
    /// </remarks>
    public static IServiceProvider UseCurrencyResolution(this IServiceProvider provider)
    {
        ThrowHelper.ThrowIfNull(provider);

        CurrencyResolution.SetDefault(provider.GetRequiredService<ICurrencyLookup>());
        return provider;
    }
}
