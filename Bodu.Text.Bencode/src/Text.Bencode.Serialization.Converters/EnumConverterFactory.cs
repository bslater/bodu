// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EnumConverterFactory.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode.Serialization.Converters;

/// <summary>
/// Produces an <see cref="EnumConverter{T}" /> for any enumeration type, configured with the built-in default behavior:
/// no naming policy and integers accepted on read.
/// </summary>
internal sealed class EnumConverterFactory
    : BencodeConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
    {
        ThrowHelper.ThrowIfNull(typeToConvert);
        return typeToConvert.IsEnum;
    }

    /// <inheritdoc />
    public override BencodeConverter CreateConverter(Type typeToConvert, BencodeSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(typeToConvert);

        Type converterType = typeof(EnumConverter<>).MakeGenericType(typeToConvert);
        return (BencodeConverter)Activator.CreateInstance(converterType, new object?[] { null, true }) !;
    }
}
