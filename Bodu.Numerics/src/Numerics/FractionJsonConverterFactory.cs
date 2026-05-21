// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FractionJsonConverterFactory.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bodu.Numerics;

/// <summary>
/// Creates <see cref="FractionJsonConverter{T}" /> instances for closed <see cref="Fraction{T}" /> types.
/// </summary>
/// <remarks>
/// This factory is referenced by the <see cref="JsonConverterAttribute" /> applied to <see cref="Fraction{T}" />, so
/// <see cref="Fraction{T}" /> values serialize through <see cref="System.Text.Json" /> without any explicit converter
/// registration.
/// </remarks>
public sealed class FractionJsonConverterFactory : JsonConverterFactory
{
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
    /// Thrown if <paramref name="typeToConvert" /> is <see langword="null" />.
    /// </exception>
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(typeToConvert);

        Type componentType = typeToConvert.GetGenericArguments()[0];
        Type converterType = typeof(FractionJsonConverter<>).MakeGenericType(componentType);
        object converter = Activator.CreateInstance(converterType)
            ?? throw new InvalidOperationException($"Unable to create a JSON converter for '{typeToConvert}'.");

        return (JsonConverter)converter;
    }
}
