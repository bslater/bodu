// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FinancialJsonServiceCollectionExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Financial.Serialization.Json;

/// <summary>
/// Provides the dependency-injection registration surface for the financial <see cref="JsonSerializerOptions" />.
/// </summary>
/// <remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// IServiceCollection services = new ServiceCollection();
///
/// services.AddFinancialJson(FinancialJsonPolicy.Compact);
///
/// // Resolve the configured options wherever financial values are serialized.
/// var options = provider.GetRequiredKeyedService<JsonSerializerOptions>(
///     FinancialJsonServiceCollectionExtensions.JsonOptionsKey);
///]]>
/// </code>
/// </example>
/// </remarks>
public static class FinancialJsonServiceCollectionExtensions
{
    /// <summary>The service key under which the configured financial <see cref="JsonSerializerOptions" /> is registered.</summary>
    public const string JsonOptionsKey = "Financial";

    /// <summary>
    /// Registers a <see cref="JsonSerializerOptions" /> configured with the financial converters under the
    /// <paramref name="policy" />, keyed by <see cref="JsonOptionsKey" />.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="policy">
    /// The serialization policy applied to every registered converter. Defaults to
    /// <see cref="FinancialJsonPolicy.Strict" />.
    /// </param>
    /// <returns>The same <paramref name="services" /> instance, so calls can be chained inline.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="policy" /> is not a defined <see cref="FinancialJsonPolicy" /> value.
    /// </exception>
    public static IServiceCollection AddFinancialJson(
        this IServiceCollection services,
        FinancialJsonPolicy policy = FinancialJsonPolicy.Strict)
    {
        ThrowHelper.ThrowIfNull(services);
        ThrowHelper.ThrowIfEnumValueIsUndefined(policy);

        services.AddKeyedSingleton(JsonOptionsKey, (_, _) => new JsonSerializerOptions().AddFinancialJsonConverters(policy));
        return services;
    }
}
