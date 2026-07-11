// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Reader;
using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml.Serialization;

/// <summary>
/// Serves as the non-generic base for custom YAML converters, in the manner of
/// <see cref="System.Text.Json.Serialization.JsonConverter" />.
/// </summary>
/// <remarks>
/// The constructor is inaccessible outside this assembly, so external code extends the model through
/// <see cref="YamlConverter{T}" /> rather than deriving from this type directly.
/// </remarks>
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
    /// Reads a value of the converter's type from the reader as a boxed object.
    /// </summary>
    /// <param name="reader">The reader positioned on the first token of the value.</param>
    /// <param name="typeToConvert">The requested type.</param>
    /// <param name="options">The serializer options in effect.</param>
    /// <returns>The converted value, boxed.</returns>
    internal abstract object? ReadAsObject(ref Utf8YamlReader reader, Type typeToConvert, YamlSerializerOptions options);

    /// <summary>
    /// Writes a boxed value using the converter's typed write implementation.
    /// </summary>
    /// <param name="writer">The writer to emit into.</param>
    /// <param name="value">The value to write.</param>
    /// <param name="options">The serializer options in effect.</param>
    internal abstract void WriteAsObject(Utf8YamlWriter writer, object? value, YamlSerializerOptions options);
}
