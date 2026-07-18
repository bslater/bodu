// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntegerConverterFactory.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml.Serialization.Converters;

/// <summary>
/// Produces an <see cref="IntegerConverter{T}" /> for each of the fixed-width CLR integer types the serializer maps to
/// YAML integer scalars.
/// </summary>
internal sealed class IntegerConverterFactory
    : YamlConverterFactory
{
    /// <summary>The integer types this factory handles.</summary>
    private static readonly HashSet<Type> s_integerTypes =
    [
        typeof(sbyte),
        typeof(byte),
        typeof(short),
        typeof(ushort),
        typeof(int),
        typeof(uint),
        typeof(long),
        typeof(ulong),
    ];

    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) =>
        s_integerTypes.Contains(typeToConvert);

    /// <inheritdoc />
    public override YamlConverter CreateConverter(Type typeToConvert, YamlSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(typeToConvert);

        Type converterType = typeof(IntegerConverter<>).MakeGenericType(typeToConvert);
        return (YamlConverter)Activator.CreateInstance(converterType)!;
    }
}
