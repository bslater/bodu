// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml.Serialization;

/// <summary>
/// Serves as the non-generic base for every TOML converter. The hierarchy is closed to two extension points:
/// <see cref="TomlConverter{T}" />, which converts a single type, and <see cref="TomlConverterFactory" />, which
/// produces a converter for a family of types.
/// </summary>
/// <remarks>
/// <para>
/// For a given type the serializer selects the converter named by a member-level converter attribute first, then a
/// type-level converter attribute, then the first matching converter registered on the options, and finally the
/// built-in converters. A converter factory in any of these positions is asked to create the concrete converter for the
/// requested type.
/// </para>
/// <para>
/// The constructor is inaccessible outside this assembly, so external code extends the model through
/// <see cref="TomlConverter{T}" /> or <see cref="TomlConverterFactory" /> rather than deriving from this type directly.
/// This mirrors the relationship between <see cref="System.Text.Json.Serialization.JsonConverter" /> and its generic
/// and factory subclasses.
/// </para>
/// </remarks>
public abstract class TomlConverter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TomlConverter" /> class.
    /// </summary>
    private protected TomlConverter()
    {
    }

    /// <summary>
    /// Determines whether the converter can convert the specified type.
    /// </summary>
    /// <param name="typeToConvert">The type to test.</param>
    /// <returns>
    /// <see langword="true" /> when the converter handles <paramref name="typeToConvert" />; otherwise
    /// <see langword="false" />.
    /// </returns>
    public abstract bool CanConvert(Type typeToConvert);

    /// <summary>
    /// Reads a value of the converter's type from the reader as a boxed object.
    /// </summary>
    /// <param name="reader">The reader positioned on the value to read.</param>
    /// <param name="typeToConvert">The requested type.</param>
    /// <param name="options">The serializer options in effect.</param>
    /// <returns>The deserialized value, boxed.</returns>
    internal abstract object? ReadAsObject(ref Utf8TomlReader reader, Type typeToConvert, TomlSerializerOptions options);

    /// <summary>
    /// Writes a boxed value of the converter's type to the writer.
    /// </summary>
    /// <param name="writer">The destination writer.</param>
    /// <param name="value">The value to write, boxed.</param>
    /// <param name="options">The serializer options in effect.</param>
    internal abstract void WriteAsObject(Utf8TomlWriter writer, object? value, TomlSerializerOptions options);
}
