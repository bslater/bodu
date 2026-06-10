// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DefaultConverters.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization.Converters;

/// <summary>
/// Provides the ordered list of built-in converters the serializer consults when no user converter or converter
/// attribute applies.
/// </summary>
/// <remarks>
/// Order is significant: specific scalar converters precede the factories, the binary converter precedes the collection
/// factory so a <see cref="byte" /> array is treated as binary rather than as a sequence, the dictionary factory
/// precedes the collection factory, and the object factory is last as the catch-all.
/// </remarks>
internal static class DefaultConverters
{
    /// <summary>
    /// The built-in converters, in resolution order.
    /// </summary>
    private static readonly FormatConverter[] s_builtIn =
    [
        new StringConverter(),
        new BooleanConverter(),
        new DoubleConverter(),
        new SingleConverter(),
        new CharConverter(),
        new GuidConverter(),
        new UriConverter(),
        new DateTimeOffsetConverter(),
        new DateTimeConverter(),
        new DateOnlyConverter(),
        new TimeOnlyConverter(),
        new TimeSpanConverter(),
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
    internal static IReadOnlyList<FormatConverter> Converters => s_builtIn;
}
