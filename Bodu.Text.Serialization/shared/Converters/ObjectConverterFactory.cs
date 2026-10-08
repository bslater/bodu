// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ObjectConverterFactory.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

#if BENCODE
namespace Bodu.Text.Bencode.Serialization.Converters;
#elif TOML
namespace Bodu.Text.Toml.Serialization.Converters;
#elif YAML
namespace Bodu.Text.Yaml.Serialization.Converters;
#endif

/// <summary>
/// Produces an <see cref="ObjectConverter{T}" /> for a plain class or struct that no more specific converter handles.
/// This is the catch-all converter, consulted last.
/// </summary>
/// <remarks>
/// The factory deliberately declines primitive and special scalar types - <see cref="decimal" />, enumerations,
/// interfaces, abstract types, delegates, reflection types such as <see cref="Type" />, pointers, and (for a format
/// without native mappings for them) the well-known framework scalar types - so that an unsupported type surfaces as a
/// missing-converter error rather than being mapped to a keyed container of its incidental public properties, which
/// would lose data or recurse on self-referential members. <see cref="object" /> has a dedicated built-in converter
/// earlier in the resolution order, so its rejection here is unreachable through the default list and guards only
/// against a reordering.
/// </remarks>
internal sealed class ObjectConverterFactory
    : FormatConverterFactory
{
#if BENCODE
    /// <summary>The well-known framework scalar types that have no native Bencode mapping. They expose public properties the object converter would otherwise treat as dictionary entries, so the factory declines them and lets the serializer report a missing-converter error rather than emitting a lossy dictionary.</summary>
    private static readonly HashSet<Type> s_unsupportedScalars =
    [
        typeof(DateTime),
        typeof(DateTimeOffset),
        typeof(TimeSpan),
        typeof(DateOnly),
        typeof(TimeOnly),
        typeof(Guid),
        typeof(Uri),
        typeof(Version),
        typeof(Half),
    ];
#endif

    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
    {
        ThrowHelper.ThrowIfNull(typeToConvert);

        if (typeToConvert.IsPrimitive || typeToConvert.IsEnum || typeToConvert.IsAbstract || typeToConvert.IsArray || IsUnsupportedObjectType(typeToConvert))
            return false;

        if (typeToConvert == typeof(decimal) || typeToConvert == typeof(object) || typeToConvert == typeof(string))
            return false;

#if BENCODE
        if (s_unsupportedScalars.Contains(typeToConvert))
            return false;
#endif

        if (Nullable.GetUnderlyingType(typeToConvert) is not null)
            return false;

        return typeToConvert.IsClass || typeToConvert.IsValueType;
    }

    /// <summary>
    /// Determines whether a type is one that no built-in converter maps: a delegate, a reflection type (a
    /// <see cref="MemberInfo" />, such as <see cref="Type" />), or a pointer.
    /// </summary>
    /// <param name="type">The type to test.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="type" /> is a delegate, reflection, or pointer type; otherwise
    /// <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// Such a type is not a plain class or struct. Mapped as an object, a delegate would be written as its target and
    /// method and a reflection type as its reflection surface, neither of which reads back, so it is declined and
    /// resolving it fails with a <see cref="NotSupportedException" /> naming the type, unless a converter of the
    /// caller's own claims it.
    /// </remarks>
    internal static bool IsUnsupportedObjectType(Type type) =>
        type.IsPointer
            || type.IsFunctionPointer
            || typeof(Delegate).IsAssignableFrom(type)
            || typeof(MemberInfo).IsAssignableFrom(type);

    /// <inheritdoc />
    public override FormatConverter CreateConverter(Type typeToConvert, FormatOptions options)
    {
        ThrowHelper.ThrowIfNull(typeToConvert);

        Type converterType = typeof(ObjectConverter<>).MakeGenericType(typeToConvert);
        return (FormatConverter)Activator.CreateInstance(converterType)!;
    }
}
