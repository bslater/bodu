// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NullableConverterFactory.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Serialization.Converters;

/// <summary>
/// Produces a <see cref="NullableConverter{T}" /> for any <see cref="Nullable{T}" /> type.
/// </summary>
internal sealed class NullableConverterFactory
    : TomlConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
    {
        ThrowHelper.ThrowIfNull(typeToConvert);
        return Nullable.GetUnderlyingType(typeToConvert) is not null;
    }

    /// <inheritdoc />
    public override TomlConverter CreateConverter(Type typeToConvert, TomlSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(typeToConvert);
        ThrowHelper.ThrowIfNull(options);

        Type underlying = Nullable.GetUnderlyingType(typeToConvert)!;
        TomlConverter inner = options.GetConverter(underlying);
        Type converterType = typeof(NullableConverter<>).MakeGenericType(underlying);
        return (TomlConverter)Activator.CreateInstance(converterType, inner)!;
    }
}
