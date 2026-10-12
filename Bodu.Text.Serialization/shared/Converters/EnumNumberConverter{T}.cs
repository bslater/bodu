// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EnumNumberConverter{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

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
/// than being narrowed into that type. A Bencode integer reaches <see cref="ulong.MaxValue" /> for an enumeration based
/// on <see cref="ulong" />, whereas a TOML integer is a signed 64-bit value. The value is written through a signed
/// 64-bit integer, so a value of an enumeration based on <see cref="ulong" /> above <see cref="long.MaxValue" /> cannot
/// be written.
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
        writer.WriteInteger(Convert.ToInt64(value, CultureInfo.InvariantCulture));
}
