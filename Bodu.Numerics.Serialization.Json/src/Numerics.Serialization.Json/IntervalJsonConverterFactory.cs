// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalJsonConverterFactory.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bodu.Numerics.Serialization.Json;

/// <summary>
/// Creates <see cref="IntervalJsonConverter{T}" /> instances for closed <see cref="Interval{T}" /> types, applying a
/// configurable <see cref="NumericsJsonPolicy" /> to every closed converter the factory produces.
/// </summary>
/// <remarks>
/// The core <see cref="Interval{T}" /> type carries no <see cref="JsonConverterAttribute" /> so that
/// <c>Bodu.Numerics</c> stays serialization-agnostic; register this factory through
/// <see cref="NumericsJsonSerializerOptionsExtensions.AddNumericsJsonConverters" /> (which registers a coherent set for
/// every numeric type) or add it directly to <see cref="JsonSerializerOptions.Converters" />.
/// </remarks>
public sealed class IntervalJsonConverterFactory
    : JsonConverterFactory
{
    /// <summary>The policy passed to every <see cref="IntervalJsonConverter{T}" /> produced by this factory.</summary>
    private readonly NumericsJsonPolicy _policy;

    /// <summary>
    /// Initializes a new instance of the <see cref="IntervalJsonConverterFactory" /> class configured for the
    /// <see cref="NumericsJsonPolicy.Strict" /> shape. Invoked by <see cref="JsonConverterAttribute" />.
    /// </summary>
    public IntervalJsonConverterFactory()
        : this(NumericsJsonPolicy.Strict)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IntervalJsonConverterFactory" /> class configured for the supplied
    /// <paramref name="policy" />.
    /// </summary>
    /// <param name="policy">The policy applied to every closed converter the factory produces.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="policy" /> is not a defined <see cref="NumericsJsonPolicy" /> value.
    /// </exception>
    public IntervalJsonConverterFactory(NumericsJsonPolicy policy)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(policy);
        _policy = policy;
    }

    /// <summary>
    /// Determines whether this factory can create a converter for the specified type.
    /// </summary>
    /// <param name="typeToConvert">The candidate type.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="typeToConvert" /> is a closed <see cref="Interval{T}" /> type;
    /// otherwise, <see langword="false" />.
    /// </returns>
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert is { IsGenericType: true }
        && typeToConvert.GetGenericTypeDefinition() == typeof(Interval<>);

    /// <summary>
    /// Creates a converter for the specified closed <see cref="Interval{T}" /> type.
    /// </summary>
    /// <param name="typeToConvert">The closed <see cref="Interval{T}" /> type to convert.</param>
    /// <param name="options">The serializer options in effect.</param>
    /// <returns>A converter for <paramref name="typeToConvert" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="typeToConvert" /> is <see langword="null" />.
    /// </exception>
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Aot", "IL3050", Justification = "Reached only through reflection-based JSON serialization, whose public entry points carry the RequiresDynamicCode annotation.")]
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(typeToConvert);

        Type componentType = typeToConvert.GetGenericArguments()[0];
        Type converterType = typeof(IntervalJsonConverter<>).MakeGenericType(componentType);
        object converter = Activator.CreateInstance(converterType, _policy)
            ?? throw new InvalidOperationException(
                string.Format(CultureInfo.CurrentCulture, NumericsJsonResourceStrings.Op_Invalid_UnableToCreateConverter, typeToConvert));

        return (JsonConverter)converter;
    }
}
