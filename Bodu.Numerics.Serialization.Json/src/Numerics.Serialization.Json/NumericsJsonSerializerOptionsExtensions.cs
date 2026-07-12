// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NumericsJsonSerializerOptionsExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;

namespace Bodu.Numerics.Serialization.Json;

/// <summary>
/// Extension methods that register the <c>Bodu.Numerics</c> JSON converters on a <see cref="JsonSerializerOptions" />,
/// picking a coherent shape for every shipped numeric type from a single <see cref="NumericsJsonPolicy" /> value.
/// </summary>
public static class NumericsJsonSerializerOptionsExtensions
{
    /// <summary>
    /// Registers the JSON converters for every serializable <c>Bodu.Numerics</c> value type on
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
    /// The core <c>Bodu.Numerics</c> types carry no <c>[JsonConverter]</c> attribute — the library is
    /// serialization-agnostic — so this call is required for <see cref="Fraction{T}" />, <see cref="Interval{T}" />,
    /// <see cref="DiscreteInterval{T}" />, <see cref="IntervalSet{T}" />, and <see cref="BigDecimal" /> to round-trip
    /// through their canonical shapes. The <see cref="IntervalPair{T}" /> and <see cref="DiscreteIntervalPair{T}" />
    /// result types are transient and are not serializable; convert them with <c>ToIntervalSet()</c> and serialize the
    /// resulting <see cref="IntervalSet{T}" /> instead.
    /// </para>
    /// <para>
    /// Use <see cref="NumericsJsonPolicy.Strict" /> for canonical persistence shapes,
    /// <see cref="NumericsJsonPolicy.Lenient" /> for import workflows tolerant of <c>"min"</c>/<c>"max"</c> aliases and
    /// compact-string fallbacks, and <see cref="NumericsJsonPolicy.Compact" /> for the single-string representations (
    /// <c>"3/4"</c>, <c>"[1, 5)"</c>, <c>"∅"</c>).
    /// </para>
    /// </remarks>
    public static JsonSerializerOptions AddNumericsJsonConverters(
        this JsonSerializerOptions options,
        NumericsJsonPolicy policy = NumericsJsonPolicy.Strict)
    {
        ThrowHelper.ThrowIfNull(options);
        ThrowHelper.ThrowIfEnumValueIsUndefined(policy);

        options.Converters.Add(new FractionJsonConverterFactory(policy));
        options.Converters.Add(new IntervalJsonConverterFactory(policy));
        options.Converters.Add(new DiscreteIntervalJsonConverterFactory(policy));
        options.Converters.Add(new IntervalSetJsonConverterFactory(policy));
        options.Converters.Add(new BigDecimalJsonConverter(policy));

        return options;
    }
}
