// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NullableConverterFactory.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

#if BENCODE
namespace Bodu.Text.Bencode.Serialization.Converters;
#elif TOML
namespace Bodu.Text.Toml.Serialization.Converters;
#elif YAML
namespace Bodu.Text.Yaml.Serialization.Converters;
#endif

/// <summary>
/// Produces a <see cref="NullableConverter{T}" /> for any <see cref="Nullable{T}" /> type.
/// </summary>
internal sealed class NullableConverterFactory
    : FormatConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
    {
        ThrowHelper.ThrowIfNull(typeToConvert);
        return Nullable.GetUnderlyingType(typeToConvert) is not null;
    }

    /// <inheritdoc />
    public override FormatConverter CreateConverter(Type typeToConvert, FormatOptions options)
    {
        ThrowHelper.ThrowIfNull(typeToConvert);
        ThrowHelper.ThrowIfNull(options);

        Type underlying = Nullable.GetUnderlyingType(typeToConvert)!;
        FormatConverter inner = options.GetConverter(underlying);
        Type converterType = typeof(NullableConverter<>).MakeGenericType(underlying);
        return (FormatConverter)Activator.CreateInstance(converterType, inner)!;
    }
}
