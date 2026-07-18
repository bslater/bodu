// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ObjectTypeConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Reader;
using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml.Serialization.Converters;

/// <summary>
/// Converts a value statically typed as <see cref="object" />: on write the value's runtime type selects the converter,
/// and on read the value surfaces as a loosely-typed graph of dictionaries, lists, and primitives (booleans, 64-bit
/// integers, doubles, and strings, with null scalars as <see langword="null" />).
/// </summary>
internal sealed class ObjectTypeConverter
    : YamlConverter<object>
{
    /// <inheritdoc />
    public override object Read(ref Utf8YamlReader reader, Type typeToConvert, YamlSerializerOptions options) =>
        BindDynamic(ref reader)!;

    /// <inheritdoc />
    public override void Write(Utf8YamlWriter writer, object value, YamlSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(options);

        // A bare System.Object has no members and would otherwise resolve back to this converter and recurse.
        if (value.GetType() == typeof(object))
        {
            writer.WriteStartMapping();
            writer.WriteEndMapping();
            return;
        }

        options.GetConverter(value.GetType()).WriteAsObject(writer, value, options);
    }

    /// <summary>
    /// Binds the value at the reader's current position to a loosely-typed graph of dictionaries, lists, and
    /// primitives.
    /// </summary>
    /// <param name="reader">The reader positioned on the value's first token.</param>
    /// <returns>The bound value.</returns>
    private static object? BindDynamic(ref Utf8YamlReader reader)
    {
        switch (reader.TokenType)
        {
            case YamlTokenType.Null:
                return null;
            case YamlTokenType.Boolean:
                return reader.GetBoolean();
            case YamlTokenType.Integer:
                return reader.GetInt64();
            case YamlTokenType.Float:
                return reader.GetDouble();
            case YamlTokenType.String:
                return reader.GetString();
            case YamlTokenType.StartSequence:
                var list = new List<object?>();
                while (reader.Read() && reader.TokenType != YamlTokenType.EndSequence)
                    list.Add(BindDynamic(ref reader));

                return list;
            default:
                var map = new Dictionary<string, object?>(StringComparer.Ordinal);
                while (reader.Read() && reader.TokenType != YamlTokenType.EndMapping)
                {
                    string key = reader.GetString();
                    reader.Read();
                    map[key] = BindDynamic(ref reader);
                }

                return map;
        }
    }
}
