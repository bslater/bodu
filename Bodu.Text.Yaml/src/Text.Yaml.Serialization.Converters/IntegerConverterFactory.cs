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
/// <remarks>
/// The native-sized and 128-bit types are included even though the writer's integer surface is 64-bit signed: a value
/// outside that range writes as its invariant text — the scalar re-reads as a string and converts back exactly — so
/// every width round-trips.
/// </remarks>
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
        typeof(nint),
        typeof(nuint),
        typeof(Int128),
        typeof(UInt128),
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
