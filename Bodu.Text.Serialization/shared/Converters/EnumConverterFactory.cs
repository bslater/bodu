// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EnumConverterFactory.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

#if BENCODE
namespace Bodu.Text.Bencode.Serialization.Converters;
#elif TOML
namespace Bodu.Text.Toml.Serialization.Converters;
#endif

/// <summary>
/// Produces an <see cref="EnumConverter{T}" /> for any enumeration type, configured with the built-in default behavior:
/// no naming policy and integers accepted on read.
/// </summary>
internal sealed class EnumConverterFactory
    : FormatConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
    {
        ThrowHelper.ThrowIfNull(typeToConvert);
        return typeToConvert.IsEnum;
    }

    /// <inheritdoc />
    public override FormatConverter CreateConverter(Type typeToConvert, FormatOptions options)
    {
        ThrowHelper.ThrowIfNull(typeToConvert);

        Type converterType = typeof(EnumConverter<>).MakeGenericType(typeToConvert);
        return (FormatConverter)Activator.CreateInstance(converterType, new object?[] { null, true })!;
    }
}
