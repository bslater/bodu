// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyOfTCurrencyJsonConverterFactory.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bodu.Financial.Serialization.Json;

/// <summary>
/// Creates <see cref="MoneyOfTCurrencyJsonConverter{TCurrency}" /> instances for closed <see cref="Money{TCurrency}" />
/// types, applying a configurable <see cref="FinancialJsonPolicy" /> to every closed converter the factory produces.
/// </summary>
/// <remarks>
/// The factory is registered on <see cref="System.Text.Json.JsonSerializerOptions" /> via
/// <see cref="FinancialJsonSerializerOptionsExtensions.AddFinancialJsonConverters" /> (or added directly to
/// <see cref="System.Text.Json.JsonSerializerOptions.Converters" />); the parameterless constructor defaults to
/// <see cref="FinancialJsonPolicy.Strict" />.
/// </remarks>
public sealed class MoneyOfTCurrencyJsonConverterFactory
    : JsonConverterFactory
{
    /// <summary>The policy passed to every <see cref="MoneyOfTCurrencyJsonConverter{TCurrency}" /> produced by this factory.</summary>
    private readonly FinancialJsonPolicy _policy;

    /// <summary>
    /// Initializes a new instance of the <see cref="MoneyOfTCurrencyJsonConverterFactory" /> class configured for the
    /// <see cref="FinancialJsonPolicy.Strict" /> shape.
    /// </summary>
    public MoneyOfTCurrencyJsonConverterFactory()
        : this(FinancialJsonPolicy.Strict)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MoneyOfTCurrencyJsonConverterFactory" /> class configured for the
    /// supplied <paramref name="policy" />.
    /// </summary>
    /// <param name="policy">The policy applied to every closed converter the factory produces.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="policy" /> is not a defined <see cref="FinancialJsonPolicy" /> value.
    /// </exception>
    public MoneyOfTCurrencyJsonConverterFactory(FinancialJsonPolicy policy)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(policy);
        _policy = policy;
    }

    /// <summary>
    /// Determines whether this factory can create a converter for the specified type.
    /// </summary>
    /// <param name="typeToConvert">The candidate type.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="typeToConvert" /> is a closed <see cref="Money{TCurrency}" /> type;
    /// otherwise <see langword="false" />.
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
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="typeToConvert" /> is <see langword="null" />.
    /// </exception>
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(typeToConvert);

        Type currencyType = typeToConvert.GetGenericArguments()[0];
        Type converterType = typeof(MoneyOfTCurrencyJsonConverter<>).MakeGenericType(currencyType);
        object converter = Activator.CreateInstance(converterType, _policy)
            ?? throw new InvalidOperationException(
                string.Format(CultureInfo.CurrentCulture, FinancialJsonResourceStrings.Op_Invalid_UnableToCreateConverter, typeToConvert));

        return (JsonConverter)converter;
    }
}
