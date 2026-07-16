// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EnumConverter{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Yaml.Reader;
using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml.Serialization.Converters;

/// <summary>
/// Converts an enumeration to and from a YAML scalar: names are matched case-insensitively and integer scalars map
/// through the enumeration's underlying value; writing emits the member name or the numeric value per
/// <see cref="YamlSerializerOptions.WriteEnumsAsStrings" />. A null scalar reads as the type default.
/// </summary>
/// <typeparam name="T">The enumeration type.</typeparam>
internal sealed class EnumConverter<T>
    : YamlConverter<T>
    where T : struct, Enum
{
    /// <inheritdoc />
    public override T Read(ref Utf8YamlReader reader, Type typeToConvert, YamlSerializerOptions options)
    {
        if (reader.TokenType == YamlTokenType.Null)
            return default;

        if (reader.TokenType == YamlTokenType.Integer)
            return (T)Enum.ToObject(typeof(T), reader.GetInt64());

        return (T)Enum.Parse(typeof(T), ScalarCoercion.ReaderScalarText(ref reader), ignoreCase: true);
    }

    /// <inheritdoc />
    public override void Write(Utf8YamlWriter writer, T value, YamlSerializerOptions options)
    {
        if (options.WriteEnumsAsStrings)
            writer.WriteString(value.ToString());
        else
            writer.WriteInteger(Convert.ToInt64(value, CultureInfo.InvariantCulture));
    }
}
