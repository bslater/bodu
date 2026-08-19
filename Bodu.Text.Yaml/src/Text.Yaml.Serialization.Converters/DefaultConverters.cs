// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DefaultConverters.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml.Serialization.Converters;

/// <summary>
/// Provides the ordered list of built-in converters the serializer consults when no user converter or converter
/// attribute applies.
/// </summary>
/// <remarks>
/// <para>
/// Order is significant: the node bridge leads so a <see cref="Bodu.Text.Yaml.Nodes.YamlNode" />-typed target is never
/// claimed structurally by the dictionary, collection, or polymorphic converters (a <c>YamlObject</c> carries a
/// dictionary surface); the scalar converters precede the structural factories so a scalar type is never claimed by
/// the object factory; the generic dictionary factory precedes the untyped dictionary fallback so a string-keyed
/// dictionary maps through its typed converter; the dictionary converters precede the collection factory so a
/// dictionary becomes a mapping rather than a sequence of pairs; the <see cref="object" /> converter precedes the
/// catch-all so an <see cref="object" />-typed member dispatches on its runtime type instead of mapping to an empty
/// mapping; the polymorphic converter claims the interface and abstract declarations the catch-all cannot construct;
/// and the object factory is last as the catch-all.
/// </para>
/// <para>
/// YAML can represent strings, integers, floats, booleans, and null natively, so the built-in set covers
/// <see cref="string" />, <see cref="char" />, <see cref="Guid" />, the fixed-width integer types,
/// <see cref="double" />, <see cref="float" />, <see cref="bool" />, and enumerations directly. Types YAML has no
/// native form for map to a defined representation instead: <see cref="decimal" /> writes as its exact invariant text
/// so it round-trips without precision loss, <see cref="System.DateTime" /> and <see cref="System.DateTimeOffset" />
/// use the ISO 8601 round-trip format, and <see cref="System.TimeSpan" /> uses the invariant constant format.
/// </para>
/// </remarks>
internal static class DefaultConverters
{
    /// <summary>The built-in converters, in resolution order.</summary>
    private static readonly YamlConverter[] s_builtIn =
    [
        new NodeConverter(),
        new StringConverter(),
        new BooleanConverter(),
        new CharConverter(),
        new GuidConverter(),
        new TimeSpanConverter(),
        new DoubleConverter(),
        new SingleConverter(),
        new DecimalConverter(),
        new DateTimeOffsetConverter(),
        new DateTimeConverter(),
        new IntegerConverterFactory(),
        new EnumConverterFactory(),
        new NullableConverterFactory(),
        new DictionaryConverterFactory(),
        new UntypedDictionaryConverter(),
        new CollectionConverterFactory(),
        new ObjectTypeConverter(),
        new PolymorphicObjectConverter(),
        new ObjectConverterFactory(),
    ];

    /// <summary>
    /// Gets the built-in converters, in the order they are consulted during converter resolution.
    /// </summary>
    /// <value>The ordered built-in converters.</value>
    internal static IReadOnlyList<YamlConverter> Converters => s_builtIn;
}
