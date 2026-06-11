// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntegerConverterFactory.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode.Serialization.Converters;

/// <summary>
/// Produces an <see cref="IntegerConverter{T}" /> for each of the fixed-width integer types, and the dedicated
/// <see cref="UInt64Converter" /> for <see cref="ulong" /> so the full unsigned 64-bit range round-trips.
/// </summary>
internal sealed class IntegerConverterFactory
    : BencodeConverterFactory
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
    ];

    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) =>
        s_integerTypes.Contains(typeToConvert);

    /// <inheritdoc />
    public override BencodeConverter CreateConverter(Type typeToConvert, BencodeSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(typeToConvert);

        if (typeToConvert == typeof(ulong))
            return new UInt64Converter();

        Type converterType = typeof(IntegerConverter<>).MakeGenericType(typeToConvert);
        return (BencodeConverter)Activator.CreateInstance(converterType) !;
    }
}
