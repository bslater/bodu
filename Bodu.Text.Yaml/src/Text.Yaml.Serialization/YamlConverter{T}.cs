// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlConverter{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Document;
using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml.Serialization;

/// <summary>
/// Serves as the strongly-typed base for custom YAML converters that read and write values of type
/// <typeparamref name="T" />.
/// </summary>
/// <typeparam name="T">The type handled by the converter.</typeparam>
public abstract class YamlConverter<T> : YamlConverter
{
    /// <summary>
    /// Determines whether the converter can convert the specified type.
    /// </summary>
    /// <param name="typeToConvert">The type to test.</param>
    /// <returns><see langword="true" /> when the type is assignable to <typeparamref name="T" />.</returns>
    public override bool CanConvert(Type typeToConvert) => typeof(T) == typeToConvert;

    /// <summary>
    /// Reads and converts a YAML element to a value of type <typeparamref name="T" />.
    /// </summary>
    /// <param name="element">The element to read.</param>
    /// <param name="options">The serializer options in effect.</param>
    /// <returns>The converted value.</returns>
    public abstract T? Read(YamlElement element, YamlSerializerOptions options);

    /// <summary>
    /// Writes a value of type <typeparamref name="T" /> as YAML.
    /// </summary>
    /// <param name="writer">The writer to emit into.</param>
    /// <param name="value">The value to write.</param>
    /// <param name="options">The serializer options in effect.</param>
    public abstract void Write(ref Utf8YamlWriter writer, T value, YamlSerializerOptions options);

    /// <inheritdoc />
    internal override void WriteAsObject(ref Utf8YamlWriter writer, object value, YamlSerializerOptions options) =>
        Write(ref writer, (T)value, options);

    /// <inheritdoc />
    internal override object? ReadAsObject(YamlElement element, YamlSerializerOptions options) =>
        Read(element, options);
}
