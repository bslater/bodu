// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DefaultConverters.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Serialization.Converters;

/// <summary>
/// Provides the ordered list of built-in converters the serializer consults when no user converter or converter
/// attribute applies.
/// </summary>
/// <remarks>
/// <para>
/// Order is significant: the node converter leads the list so a <see cref="Nodes.TomlNode" /> tree is always handled by
/// the document object model bridge and never captured by the dictionary or object factories; the scalar converters
/// precede the structural factories so a scalar type is never claimed by the object factory; the byte-array converter
/// precedes the collection factory so a <see cref="byte" /> array is mapped by its dedicated converter rather than as a
/// sequence of integers; the dictionary factory precedes the collection factory so a string-keyed dictionary becomes a
/// table rather than a collection; and the object factory is last as the catch-all.
/// </para>
/// <para>
/// TOML can represent strings, integers, floats, Booleans, and the four date-time kinds natively, so the built-in set
/// covers <see cref="string" />, <see cref="char" />, <see cref="Guid" />, <see cref="Uri" />, the fixed-width integer
/// types, <see cref="double" />, <see cref="float" />, <see cref="bool" />, <see cref="System.DateTimeOffset" />,
/// <see cref="System.DateTime" />, <see cref="System.DateOnly" />, and <see cref="System.TimeOnly" />, plus
/// enumerations and the structural shapes. It omits <see cref="decimal" /> and <see cref="System.TimeSpan" />, which
/// TOML has no native form for; a program that needs one registers a custom <see cref="TomlConverter{T}" />.
/// </para>
/// </remarks>
internal static class DefaultConverters
{
    /// <summary>
    /// The built-in converters, in resolution order.
    /// </summary>
    private static readonly TomlConverter[] s_builtIn =
    [
        new TomlNodeConverter(),
        new TomlElementConverter(),
        new TomlDocumentConverter(),
        new StringConverter(),
        new BooleanConverter(),
        new CharConverter(),
        new GuidConverter(),
        new UriConverter(),
        new VersionConverter(),
        new TimeSpanConverter(),
        new DoubleConverter(),
        new SingleConverter(),
        new HalfConverter(),
        new DecimalConverter(),
        new DateTimeOffsetConverter(),
        new DateTimeConverter(),
        new DateOnlyConverter(),
        new TimeOnlyConverter(),
        new ByteArrayConverter(),
        new MemoryByteConverter(),
        new ReadOnlyMemoryByteConverter(),
        new IntegerConverterFactory(),
        new EnumConverterFactory(),
        new NullableConverterFactory(),
        new DictionaryConverterFactory(),
        new CollectionConverterFactory(),
        new ObjectConverterFactory(),
    ];

    /// <summary>
    /// Gets the built-in converters, in the order they are consulted during converter resolution.
    /// </summary>
    /// <returns>The ordered built-in converters.</returns>
    internal static IReadOnlyList<TomlConverter> Converters => s_builtIn;
}
