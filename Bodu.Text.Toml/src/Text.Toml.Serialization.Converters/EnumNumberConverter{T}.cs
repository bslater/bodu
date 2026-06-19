// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EnumNumberConverter{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml.Serialization.Converters;

/// <summary>
/// Converts an enumeration value to and from a TOML integer carrying its underlying numeric value.
/// </summary>
/// <typeparam name="T">The enumeration type.</typeparam>
/// <remarks>
/// The numeric value is read and written through a signed 64-bit integer, which is the only integer width TOML can
/// store; an enumeration whose underlying type is <see cref="ulong" /> with values above <see cref="long.MaxValue" />
/// therefore cannot be represented.
/// </remarks>
internal sealed class EnumNumberConverter<T>
    : TomlConverter<T>
    where T : struct, Enum
{
    /// <inheritdoc />
    public override T Read(ref TomlDocumentReader reader, Type typeToConvert, TomlSerializerOptions options)
    {
        if (reader.TokenType != TomlTokenType.Integer)
        {
            throw new TomlSerializationException(
                string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_ExpectedInteger, reader.TokenType));
        }

        return (T)Enum.ToObject(typeof(T), reader.GetInt64());
    }

    /// <inheritdoc />
    public override void Write(Utf8TomlWriter writer, T value, TomlSerializerOptions options) =>
        writer.WriteInteger(Convert.ToInt64(value, CultureInfo.InvariantCulture));
}
