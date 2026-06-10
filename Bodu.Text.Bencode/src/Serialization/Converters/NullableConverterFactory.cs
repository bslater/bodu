// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NullableConverterFactory.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode.Serialization.Converters;

/// <summary>
/// Produces a <see cref="NullableConverter{T}" /> for any <see cref="Nullable{T}" /> type.
/// </summary>
internal sealed class NullableConverterFactory
    : BencodeConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
    {
        ThrowHelper.ThrowIfNull(typeToConvert);
        return Nullable.GetUnderlyingType(typeToConvert) is not null;
    }

    /// <inheritdoc />
    public override BencodeConverter CreateConverter(Type typeToConvert, BencodeSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(typeToConvert);
        ThrowHelper.ThrowIfNull(options);

        Type underlying = Nullable.GetUnderlyingType(typeToConvert) !;
        BencodeConverter inner = options.GetConverter(underlying);
        Type converterType = typeof(NullableConverter<>).MakeGenericType(underlying);
        return (BencodeConverter)Activator.CreateInstance(converterType, inner) !;
    }
}
