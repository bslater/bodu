// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntegerConverterFactory.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Serialization.Converters;

/// <summary>
/// Produces an <see cref="IntegerConverter{T}" /> for each of the fixed-width integer types.
/// </summary>
/// <remarks>
/// The 128-bit types are included even though TOML integers are 64-bit signed: the converter's checked conversions
/// confine them to the storable range, so a value outside that range surfaces as a serialization error on write rather
/// than wrapping, and every stored integer reads back exactly.
/// </remarks>
internal sealed class IntegerConverterFactory
    : TomlConverterFactory
{
    /// <summary>
    /// The integer types this factory handles.
    /// </summary>
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
    public override TomlConverter CreateConverter(Type typeToConvert, TomlSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(typeToConvert);

        Type converterType = typeof(IntegerConverter<>).MakeGenericType(typeToConvert);
        return (TomlConverter)Activator.CreateInstance(converterType)!;
    }
}
