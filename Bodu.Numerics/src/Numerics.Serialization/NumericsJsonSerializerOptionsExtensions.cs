// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NumericsJsonSerializerOptionsExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;

namespace Bodu.Numerics.Serialization;

/// <summary>
/// Extension methods that register the <see cref="Bodu.Numerics" /> JSON converters on a
/// <see cref="JsonSerializerOptions" />, picking a coherent shape for every shipped numeric type from a single
/// <see cref="NumericsJsonPolicy" /> value.
/// </summary>
public static class NumericsJsonSerializerOptionsExtensions
{
    /// <summary>
    /// Adds the <see cref="Fraction{T}" /> and <see cref="Interval{T}" /> JSON converters to
    /// <paramref name="options" />, configured for the supplied <paramref name="policy" />.
    /// </summary>
    /// <param name="options">The serializer options to extend.</param>
    /// <param name="policy">
    /// The serialization policy applied to every registered converter. Defaults to
    /// <see cref="NumericsJsonPolicy.Strict" />.
    /// </param>
    /// <returns>The same <paramref name="options" /> instance, so calls can be chained inline.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="policy" /> is not a defined <see cref="NumericsJsonPolicy" /> value.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="options" /> has already been used for serialization or deserialization, in which case
    /// <see cref="JsonSerializerOptions.Converters" /> is read-only. Configure options before first use.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Converters registered on <see cref="JsonSerializerOptions.Converters" /> take precedence over the
    /// <c>[JsonConverter]</c> attribute that ships on the numeric types, so calling this method overrides the default
    /// attribute-driven policy for the lifetime of <paramref name="options" />.
    /// </para>
    /// <para>
    /// Use <see cref="NumericsJsonPolicy.Strict" /> for canonical persistence shapes,
    /// <see cref="NumericsJsonPolicy.Lenient" /> for import workflows tolerant of <c>"min"</c>/<c>"max"</c> aliases
    /// and compact-string fallbacks, and <see cref="NumericsJsonPolicy.Compact" /> for the single-string
    /// representations (<c>"3/4"</c>, <c>"[1, 5)"</c>, <c>"∅"</c>).
    /// </para>
    /// </remarks>
    public static JsonSerializerOptions AddNumericsJsonConverters(
        this JsonSerializerOptions options,
        NumericsJsonPolicy policy = NumericsJsonPolicy.Strict)
    {
        ThrowHelper.ThrowIfNull(options);
        NumericsThrowHelper.ThrowIfNumericsJsonPolicyUndefined(policy);

        options.Converters.Add(new FractionJsonConverterFactory(policy));
        options.Converters.Add(new IntervalJsonConverterFactory(policy));

        return options;
    }
}
