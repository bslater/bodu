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
/// Order is significant: the node converter leads the list so a <see cref="Nodes.BencodeNode" /> tree is always handled
/// by the document object model bridge and never captured by the dictionary or object factories, the string and
/// byte-string converters precede the factories, the byte-string converter precedes the collection factory so a
/// <see cref="byte" /> array is treated as a byte string rather than as a sequence, the dictionary factory precedes the
/// collection factory, and the object factory is last as the catch-all.
/// </para>
/// <para>
/// Bencode represents only integers and byte strings as scalars, so the built-in set omits converters for Boolean,
/// floating-point, character, GUID, URI, and date-time types. A program that needs to serialize one of those types
/// registers a custom <see cref="BencodeConverter{T}" /> that reduces it to an integer or a byte string; without one an
/// unsupported type surfaces as a missing-converter error.
/// </para>
/// </remarks>
internal static class DefaultConverters
{
    /// <summary>
    /// The built-in converters, in resolution order.
    /// </summary>
    private static readonly BencodeConverter[] s_builtIn =
    [
        new BencodeNodeConverter(),
        new StringConverter(),
        new ByteArrayConverter(),
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
    internal static IReadOnlyList<BencodeConverter> Converters => s_builtIn;
}
