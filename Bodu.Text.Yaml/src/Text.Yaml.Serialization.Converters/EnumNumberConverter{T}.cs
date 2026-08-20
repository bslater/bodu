// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EnumNumberConverter{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Yaml.Reader;
using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml.Serialization.Converters;

/// <summary>
/// Converts an enumeration value to and from a YAML integer scalar carrying its underlying numeric value. A null scalar
/// reads as the type default.
/// </summary>
/// <typeparam name="T">The enumeration type.</typeparam>
/// <remarks>
/// The numeric value is read and written through a signed 64-bit integer, which is the only integer width the writer
/// can store; an enumeration whose underlying type is <see cref="ulong" /> with values above
/// <see cref="long.MaxValue" /> therefore cannot be represented.
/// </remarks>
internal sealed class EnumNumberConverter<T>
    : YamlConverter<T>
    where T : struct, Enum
{
    /// <inheritdoc />
    public override T Read(ref Utf8YamlReader reader, Type typeToConvert, YamlSerializerOptions options)
    {
        if (reader.TokenType == YamlTokenType.Null)
            return default;

        if (reader.TokenType != YamlTokenType.Integer)
        {
            throw new YamlSerializationException(string.Format(
                CultureInfo.CurrentCulture, YamlResourceStrings.Op_Invalid_ExpectedInteger, reader.TokenType));
        }

        return (T)Enum.ToObject(typeof(T), reader.GetInt64());
    }

    /// <inheritdoc />
    public override void Write(Utf8YamlWriter writer, T value, YamlSerializerOptions options) =>
        writer.WriteInteger(Convert.ToInt64(value, CultureInfo.InvariantCulture));
}
