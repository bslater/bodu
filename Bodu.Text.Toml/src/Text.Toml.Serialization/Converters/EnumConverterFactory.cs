// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EnumConverterFactory.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Serialization.Converters;

/// <summary>
/// Produces an <see cref="EnumConverter{T}" /> for any enumeration type, configured with the built-in default behavior:
/// no naming policy and integers accepted on read. Mirrors the implicit enum-to-name handling of
/// <see cref="System.Text.Json.JsonSerializerOptions" />.
/// </summary>
internal sealed class EnumConverterFactory
    : TomlConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
    {
        ThrowHelper.ThrowIfNull(typeToConvert);
        return typeToConvert.IsEnum;
    }

    /// <inheritdoc />
    public override TomlConverter CreateConverter(Type typeToConvert, TomlSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(typeToConvert);

        Type converterType = typeof(EnumConverter<>).MakeGenericType(typeToConvert);
        return (TomlConverter)Activator.CreateInstance(converterType, new object?[] { null, true }) !;
    }
}
