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
/// The numeric value is read and written through a signed 64-bit integer, which is the only integer width the format
/// can store; an enumeration whose underlying type is <see cref="ulong" /> with values above
/// <see cref="long.MaxValue" /> therefore cannot be represented.
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

        return (T)Enum.ToObject(typeof(T), reader.GetInt64());
    }

    /// <inheritdoc />
    public override void Write(FormatWriter writer, T value, FormatOptions options) =>
        writer.WriteInteger(Convert.ToInt64(value, CultureInfo.InvariantCulture));
}
