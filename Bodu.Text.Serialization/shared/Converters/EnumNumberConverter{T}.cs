// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EnumNumberConverter{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

#if BENCODE
namespace Bodu.Text.Bencode.Serialization.Converters;
#elif TOML
namespace Bodu.Text.Toml.Serialization.Converters;
#endif

/// <summary>
/// Converts an enumeration value to and from the format's integer token carrying its underlying numeric value.
/// </summary>
/// <typeparam name="T">The enumeration type.</typeparam>
/// <remarks>
/// The numeric value is read through <see cref="EnumIntegerConversion.ReadInteger{T}(ref FormatReader)" />, so an
/// integer outside the range of the enumeration's underlying type throws the format's serialization exception rather
/// than being narrowed into that type, and written through
/// <see cref="EnumIntegerConversion.WriteInteger{T}(ref FormatWriter, T)" />. A Bencode integer reaches
/// <see cref="ulong.MaxValue" />, so an enumeration based on <see cref="ulong" /> reads and writes every value. A TOML
/// integer is a signed 64-bit value, so such an enumeration reads integers up to <see cref="long.MaxValue" />, and
/// writing a larger value throws the format's serialization exception, with the <see cref="OverflowException" /> as its
/// inner exception.
/// </remarks>
internal sealed class EnumNumberConverter<T>
    : SharedConverter<T>
    where T : struct, Enum
{
    /// <inheritdoc />
    public override T Read(ref FormatReader reader, Type typeToConvert, FormatOptions options)
    {
        if (reader.TokenType != FormatToken.Integer)
            throw SerializationThrowHelper.ExpectedInteger(ref reader);

        return EnumIntegerConversion.ReadInteger<T>(ref reader);
    }

    /// <inheritdoc />
    public override void Write(FormatWriter writer, T value, FormatOptions options) =>
        EnumIntegerConversion.WriteInteger(ref writer, value);
}
