// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlConverter{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Reader;
using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml.Serialization;

/// <summary>
/// Serves as the strongly-typed base for custom YAML converters that read and write values of type
/// <typeparamref name="T" />. This is the base authors derive to customize how a specific type is serialized.
/// </summary>
/// <typeparam name="T">The type handled by the converter.</typeparam>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// public sealed class VersionConverter : YamlConverter<Version>
/// {
///     public override Version Read(ref Utf8YamlReader reader, Type typeToConvert, YamlSerializerOptions options) =>
///         Version.Parse(reader.GetString());
///
///     public override void Write(Utf8YamlWriter writer, Version value, YamlSerializerOptions options) =>
///         writer.WriteString(value.ToString());
/// }
///]]>
/// </code>
/// </example>
public abstract class YamlConverter<T> : YamlConverter
{
    /// <summary>
    /// Determines whether the converter can convert the specified type.
    /// </summary>
    /// <param name="typeToConvert">The type to test.</param>
    /// <returns><see langword="true" /> when the type is <typeparamref name="T" />.</returns>
    public override bool CanConvert(Type typeToConvert) => typeof(T) == typeToConvert;

    /// <summary>
    /// Reads and converts a value of type <typeparamref name="T" /> from the reader.
    /// </summary>
    /// <param name="reader">The reader positioned on the first token of the value.</param>
    /// <param name="typeToConvert">The requested type.</param>
    /// <param name="options">The serializer options in effect.</param>
    /// <returns>The converted value.</returns>
    /// <remarks>
    /// On entry the reader is positioned on the value's first token. On return it must be positioned on the value's
    /// last token, so the caller can advance past the value with a single <see cref="Utf8YamlReader.Read" />.
    /// </remarks>
    public abstract T Read(ref Utf8YamlReader reader, Type typeToConvert, YamlSerializerOptions options);

    /// <summary>
    /// Writes a value of type <typeparamref name="T" /> as YAML.
    /// </summary>
    /// <param name="writer">The writer to emit into.</param>
    /// <param name="value">The value to write.</param>
    /// <param name="options">The serializer options in effect.</param>
    public abstract void Write(Utf8YamlWriter writer, T value, YamlSerializerOptions options);

    /// <inheritdoc />
    internal sealed override object? ReadAsObject(ref Utf8YamlReader reader, Type typeToConvert, YamlSerializerOptions options) =>
        Read(ref reader, typeToConvert, options);

    /// <inheritdoc />
    internal sealed override void WriteAsObject(Utf8YamlWriter writer, object? value, YamlSerializerOptions options)
    {
        // Once a failure has been recorded, stop dispatching so the recursion unwinds through normal returns rather
        // than entering another converter on a document that will be discarded.
        if (writer.WriteStack is { HasFailure: true })
            return;

        // A null value always writes the YAML null scalar without consulting the converter, and the depth ceiling is
        // enforced at this single dispatch seam for every value — scalar or container — so each converter observes
        // the same entry contract the serializer's former inline walker provided.
        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        if (writer.CurrentDepth > options.EffectiveMaxDepth)
        {
            throw new YamlSerializationException(string.Format(
                System.Globalization.CultureInfo.CurrentCulture, YamlResourceStrings.Format_Invalid_YamlNestingTooDeep, options.EffectiveMaxDepth));
        }

        Write(writer, (T)value!, options);
    }
}
