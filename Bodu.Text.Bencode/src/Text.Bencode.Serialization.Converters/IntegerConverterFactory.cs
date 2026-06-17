// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntegerConverterFactory.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode.Serialization.Converters;

/// <summary>
/// Produces an <see cref="IntegerConverter{T}" /> for each of the fixed-width integer types, and the dedicated
/// <see cref="UInt64Converter" /> and <see cref="UInt128Converter" /> for the unsigned types whose range exceeds the
/// signed 64-bit surface the shared converter reads and writes through.
/// </summary>
/// <remarks>
/// The 128-bit types are included even though this implementation reads and writes integers through the 64-bit
/// surfaces: checked conversions confine <see cref="Int128" /> to the signed and <see cref="UInt128" /> to the unsigned
/// 64-bit range, so an unstorable value surfaces as a serialization error on write rather than wrapping, and every
/// stored integer reads back exactly.
/// </remarks>
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
        typeof(Int128),
        typeof(UInt128),
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

        if (typeToConvert == typeof(UInt128))
            return new UInt128Converter();

        Type converterType = typeof(IntegerConverter<>).MakeGenericType(typeToConvert);
        return (BencodeConverter)Activator.CreateInstance(converterType)!;
    }
}
