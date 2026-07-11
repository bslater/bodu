// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Document;
using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml.Serialization;

/// <summary>
/// Serves as the non-generic base for custom YAML converters, in the manner of
/// <see cref="System.Text.Json.Serialization.JsonConverter" />.
/// </summary>
public abstract class YamlConverter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="YamlConverter" /> class.
    /// </summary>
    private protected YamlConverter()
    {
    }

    /// <summary>
    /// Determines whether the converter can convert the specified type.
    /// </summary>
    /// <param name="typeToConvert">The type to test.</param>
    /// <returns>
    /// <see langword="true" /> when the converter handles the type; otherwise <see langword="false" />.
    /// </returns>
    public abstract bool CanConvert(Type typeToConvert);

    /// <summary>
    /// Writes a boxed value using the converter's typed write implementation.
    /// </summary>
    /// <param name="writer">The writer to emit into.</param>
    /// <param name="value">The value to write.</param>
    /// <param name="options">The serializer options in effect.</param>
    internal abstract void WriteAsObject(Utf8YamlWriter writer, object value, YamlSerializerOptions options);

    /// <summary>
    /// Reads a value as a boxed object using the converter's typed read implementation.
    /// </summary>
    /// <param name="element">The element to read.</param>
    /// <param name="options">The serializer options in effect.</param>
    /// <returns>The converted value, boxed.</returns>
    internal abstract object? ReadAsObject(YamlElement element, YamlSerializerOptions options);
}
