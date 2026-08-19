// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EnumConverterFactory.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml.Serialization.Converters;

/// <summary>
/// Produces an <see cref="EnumConverter{T}" /> for any enumeration type, configured with the built-in default behavior:
/// no naming policy and integer scalars accepted on read.
/// </summary>
internal sealed class EnumConverterFactory
    : YamlConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
    {
        ThrowHelper.ThrowIfNull(typeToConvert);
        return typeToConvert.IsEnum;
    }

    /// <inheritdoc />
    public override YamlConverter CreateConverter(Type typeToConvert, YamlSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(typeToConvert);

        Type converterType = typeof(EnumConverter<>).MakeGenericType(typeToConvert);
        return (YamlConverter)Activator.CreateInstance(converterType, new object?[] { null, true })!;
    }
}
