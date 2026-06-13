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
/// Order is significant: the document object model bridges lead the list so a <see cref="Nodes.TomlNode" /> tree, a
/// <see cref="Document.TomlElement" />, or a <see cref="Document.TomlDocument" /> is always handled by its bridge and
/// never captured by the dictionary or object factories; the scalar converters precede the structural factories so a
/// scalar type is never claimed by the object factory; the byte-array and memory-of-byte converters precede the
/// collection factory so binary data is mapped by its dedicated converters rather than as a sequence of integers; the
/// dictionary factory precedes the collection factory so a string-keyed dictionary becomes a table rather than a
/// collection; the <see cref="object" /> converter precedes the catch-all so an <see cref="object" />-typed member
/// dispatches on its runtime type instead of mapping to an empty table; and the object factory is last as the
/// catch-all.
/// </para>
/// <para>
/// TOML can represent strings, integers, floats, Booleans, and the four date-time kinds natively, so the built-in set
/// covers <see cref="string" />, <see cref="char" />, <see cref="Guid" />, <see cref="Uri" />, <see cref="Version" />,
/// the fixed-width integer types, <see cref="double" />, <see cref="float" />, <see cref="Half" />, <see cref="bool" />
/// , <see cref="System.DateTimeOffset" />, <see cref="System.DateTime" />, <see cref="System.DateOnly" />, and
/// <see cref="System.TimeOnly" />, plus enumerations and the structural shapes. Types TOML has no native form for map
/// to a defined representation instead: <see cref="decimal" /> writes as a float or lossless string per
/// <see cref="TomlSerializerOptions.DecimalHandling" /> and reads from a float, integer, or string;
/// <see cref="System.TimeSpan" /> maps to the invariant constant-format string; <see cref="Half" /> widens exactly to a
/// float on write and narrows with IEEE 754 saturation on read; <see cref="Int128" /> and <see cref="UInt128" /> are
/// confined by checked conversion to the signed 64-bit range TOML stores; and byte arrays and memory-of-byte map to an
/// integer array or Base64 string per <see cref="TomlSerializerOptions.ByteArrayHandling" />.
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
        new ObjectTypeConverter(),
        new ObjectConverterFactory(),
    ];

    /// <summary>
    /// Gets the built-in converters, in the order they are consulted during converter resolution.
    /// </summary>
    /// <returns>The ordered built-in converters.</returns>
    internal static IReadOnlyList<TomlConverter> Converters => s_builtIn;
}
