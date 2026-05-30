// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyJsonConverterFactory.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bodu.Numerics;

/// <summary>
/// Creates <see cref="MoneyJsonConverter{TCurrency}" /> instances for closed <see cref="Money{TCurrency}" /> types.
/// </summary>
/// <remarks>
/// This factory is referenced by the <see cref="JsonConverterAttribute" /> applied to <see cref="Money{TCurrency}" />,
/// so monetary values serialize through <see cref="System.Text.Json" /> without any explicit converter registration.
/// </remarks>
public sealed class MoneyJsonConverterFactory : JsonConverterFactory
{
    /// <summary>
    /// Determines whether this factory can create a converter for the specified type.
    /// </summary>
    /// <param name="typeToConvert">The candidate type.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="typeToConvert" /> is a closed <see cref="Money{TCurrency}" />
    /// type; otherwise <see langword="false" />.
    /// </returns>
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert is { IsGenericType: true }
        && typeToConvert.GetGenericTypeDefinition() == typeof(Money<>);

    /// <summary>
    /// Creates a converter for the specified closed <see cref="Money{TCurrency}" /> type.
    /// </summary>
    /// <param name="typeToConvert">The closed <see cref="Money{TCurrency}" /> type to convert.</param>
    /// <param name="options">The serializer options in effect.</param>
    /// <returns>A converter for <paramref name="typeToConvert" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="typeToConvert" /> is <see langword="null" />.</exception>
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(typeToConvert);

        Type currencyType = typeToConvert.GetGenericArguments()[0];
        Type converterType = typeof(MoneyJsonConverter<>).MakeGenericType(currencyType);
        var converter = Activator.CreateInstance(converterType)
            ?? throw new InvalidOperationException($"Unable to create a JSON converter for '{typeToConvert}'.");

        return (JsonConverter)converter;
    }
}
