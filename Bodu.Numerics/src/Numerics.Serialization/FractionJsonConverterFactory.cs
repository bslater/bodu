// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FractionJsonConverterFactory.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bodu.Numerics.Serialization;

/// <summary>
/// Creates <see cref="FractionJsonConverter{T}" /> instances for closed <see cref="Fraction{T}" /> types, applying a
/// configurable <see cref="NumericsJsonPolicy" /> to every closed converter the factory produces.
/// </summary>
/// <remarks>
/// This factory is referenced by the <see cref="JsonConverterAttribute" /> applied to <see cref="Fraction{T}" />, so
/// rational values serialize through <see cref="System.Text.Json" /> without any explicit converter registration. The
/// attribute path defaults to <see cref="NumericsJsonPolicy.Strict" />; consumers who need a different policy register
/// an additional factory via <see cref="NumericsJsonSerializerOptionsExtensions.AddNumericsJsonConverters" />, which
/// takes precedence over the type-level attribute.
/// </remarks>
public sealed class FractionJsonConverterFactory
    : JsonConverterFactory
{
    /// <summary>The policy passed to every <see cref="FractionJsonConverter{T}" /> produced by this factory.</summary>
    private readonly NumericsJsonPolicy _policy;

    /// <summary>
    /// Initializes a new instance of the <see cref="FractionJsonConverterFactory" /> class configured for the
    /// <see cref="NumericsJsonPolicy.Strict" /> shape. Invoked by <see cref="JsonConverterAttribute" />.
    /// </summary>
    public FractionJsonConverterFactory()
        : this(NumericsJsonPolicy.Strict)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FractionJsonConverterFactory" /> class configured for the supplied
    /// <paramref name="policy" />.
    /// </summary>
    /// <param name="policy">The policy applied to every closed converter the factory produces.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="policy" /> is not a defined <see cref="NumericsJsonPolicy" /> value.
    /// </exception>
    public FractionJsonConverterFactory(NumericsJsonPolicy policy)
    {
        NumericsThrowHelper.ThrowIfNumericsJsonPolicyUndefined(policy);
        _policy = policy;
    }

    /// <summary>
    /// Determines whether this factory can create a converter for the specified type.
    /// </summary>
    /// <param name="typeToConvert">The candidate type.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="typeToConvert" /> is a closed <see cref="Fraction{T}" /> type;
    /// otherwise, <see langword="false" />.
    /// </returns>
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert is { IsGenericType: true }
        && typeToConvert.GetGenericTypeDefinition() == typeof(Fraction<>);

    /// <summary>
    /// Creates a converter for the specified closed <see cref="Fraction{T}" /> type.
    /// </summary>
    /// <param name="typeToConvert">The closed <see cref="Fraction{T}" /> type to convert.</param>
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
        Type converterType = typeof(FractionJsonConverter<>).MakeGenericType(componentType);
        object converter = Activator.CreateInstance(converterType, _policy)
            ?? throw new InvalidOperationException(
                string.Format(CultureInfo.CurrentCulture, NumericsResourceStrings.Op_Invalid_UnableToCreateConverter, typeToConvert));

        return (JsonConverter)converter;
    }
}
