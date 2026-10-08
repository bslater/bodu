// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlDocumentTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using System.Text.Json;
using Bodu.Text.Yaml.Document;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies parsing of YAML source into the read-only <see cref="YamlDocument" /> object model. Test methods live
/// in the member- and subject-specific partial files: scalar typing, quoting, block scalars, flow collections,
/// anchors, merge keys, tags, multi-document streams, structural parsing, mapping enumeration, and profile
/// enforcement.
/// </summary>
[TestClass]
public partial class YamlDocumentTests
{
    /// <summary>
    /// Renders an element as compact JSON, so that a test can state the tree it expects as one string.
    /// </summary>
    /// <param name="element">The element to render.</param>
    /// <returns>The JSON text: mappings as objects, sequences as arrays, and each scalar as its typed value.</returns>
    private static string ToJson(YamlElement element)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
            WriteJson(writer, element);

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>
    /// Writes an element, and everything beneath it, as JSON.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="element">The element to write.</param>
    private static void WriteJson(Utf8JsonWriter writer, YamlElement element)
    {
        switch (element.ValueKind)
        {
            case YamlValueKind.Mapping:
                writer.WriteStartObject();
                foreach (YamlProperty property in element.EnumerateMapping())
                {
                    writer.WritePropertyName(property.Name);
                    WriteJson(writer, property.Value);
                }

                writer.WriteEndObject();
                break;

            case YamlValueKind.Sequence:
                writer.WriteStartArray();
                foreach (YamlElement item in element.EnumerateSequence())
                    WriteJson(writer, item);

                writer.WriteEndArray();
                break;

            case YamlValueKind.String:
                writer.WriteStringValue(element.GetString());
                break;

            case YamlValueKind.Integer:
                writer.WriteNumberValue(element.GetInt64());
                break;

            case YamlValueKind.Float:
                writer.WriteNumberValue(element.GetDouble());
                break;

            case YamlValueKind.Boolean:
                writer.WriteBooleanValue(element.GetBoolean());
                break;

            default:
                writer.WriteNullValue();
                break;
        }
    }
}
