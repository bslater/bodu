// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntegerConverterFactory.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

#if BENCODE
namespace Bodu.Text.Bencode.Serialization.Converters;
#elif TOML
namespace Bodu.Text.Toml.Serialization.Converters;
#endif

/// <summary>
/// Produces an <see cref="IntegerConverter{T}" /> for each of the fixed-width integer types. A format whose native
/// integer surface exceeds the signed 64-bit range the shared converter reads and writes through routes those widths
/// to its dedicated converters instead.
/// </summary>
/// <remarks>
/// The 128-bit types are included even though the shared converter is 64-bit signed: its checked conversions confine
/// them to the storable range, so a value outside that range surfaces as a serialization error on write rather than
/// wrapping, and every stored integer reads back exactly.
/// </remarks>
internal sealed class IntegerConverterFactory
    : FormatConverterFactory
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
    public override FormatConverter CreateConverter(Type typeToConvert, FormatOptions options)
    {
        ThrowHelper.ThrowIfNull(typeToConvert);

#if BENCODE
        // Bencode integers are arbitrary-precision per BEP 3, so the unsigned widths above the signed 64-bit surface
        // route to converters over the reader's and writer's unsigned surfaces.
        if (typeToConvert == typeof(ulong))
            return new UInt64Converter();

        if (typeToConvert == typeof(nuint))
            return new UIntPtrConverter();

        if (typeToConvert == typeof(UInt128))
            return new UInt128Converter();
#endif

        Type converterType = typeof(IntegerConverter<>).MakeGenericType(typeToConvert);
        return (FormatConverter)Activator.CreateInstance(converterType)!;
    }
}
