// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DefaultConverters.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode.Serialization.Converters;

/// <summary>
/// Provides the ordered list of built-in converters the serializer consults when no user converter or converter
/// attribute applies.
/// </summary>
/// <remarks>
/// <para>
/// Order is significant: the document object model bridges lead the list so a <see cref="Nodes.BencodeNode" /> tree, a
/// <see cref="Document.BencodeElement" />, or a <see cref="Document.BencodeDocument" /> is always handled by its bridge
/// and never captured by the dictionary or object factories, the string, byte-string, and memory-of-byte converters
/// precede the factories so binary data is treated as a byte string rather than as a sequence, the dictionary factory
/// precedes the collection factory, the <see cref="object" /> converter precedes the catch-all so an
/// <see cref="object" />-typed member dispatches on its runtime type instead of mapping to an empty dictionary, and the
/// object factory is last as the catch-all.
/// </para>
/// <para>
/// Bencode represents only integers and byte strings as scalars, so the built-in set covers <see cref="string" />,
/// <see cref="byte" /> arrays and memory-of-byte, and the fixed-width integer family — including <see cref="Int128" />
/// and <see cref="UInt128" />, confined by checked conversion to the 64-bit surfaces the implementation reads and
/// writes through — plus enumerations and the structural shapes. It omits converters for Boolean, floating-point,
/// character, GUID, URI, and date-time types. A program that needs to serialize one of those types registers a custom
/// <see cref="BencodeConverter{T}" /> that reduces it to an integer or a byte string; without one an unsupported type
/// surfaces as a missing-converter error.
/// </para>
/// </remarks>
internal static class DefaultConverters
{
    /// <summary>The built-in converters, in resolution order.</summary>
    private static readonly BencodeConverter[] s_builtIn =
    [
        new BencodeNodeConverter(),
        new BencodeElementConverter(),
        new BencodeDocumentConverter(),
        new StringConverter(),
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
    internal static IReadOnlyList<BencodeConverter> Converters => s_builtIn;
}
