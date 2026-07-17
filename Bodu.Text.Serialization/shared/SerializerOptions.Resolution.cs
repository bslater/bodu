// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerializerOptions.Resolution.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using Bodu.Text.Serialization;
#if TOML || YAML
using System.Diagnostics.CodeAnalysis;
#endif

#if BENCODE
namespace Bodu.Text.Bencode;

public sealed partial class BencodeSerializerOptions
#elif TOML
namespace Bodu.Text.Toml;

public sealed partial class TomlSerializerOptions
#elif YAML
namespace Bodu.Text.Yaml;

public sealed partial class YamlSerializerOptions
#endif
{
    /// <summary>The cache of concrete converters resolved per type.</summary>
    private readonly ConcurrentDictionary<Type, FormatConverter> _converterCache = new();

    /// <summary>The cache of resolved type metadata.</summary>
    private readonly ConcurrentDictionary<Type, Serialization.Metadata.TypeMetadata> _metadataCache = new();

    /// <summary>The frozen snapshot of user converters captured when the options became read-only, or <see langword="null" /> while mutable.</summary>
    private FormatConverter[]? _frozenConverters;

    /// <summary>
    /// Gets a value indicating whether the options have become read-only.
    /// </summary>
    /// <value><see langword="true" /> once the options have been used or frozen; otherwise <see langword="false" />.</value>
    public bool IsReadOnly => _frozenConverters is not null;

    /// <summary>
    /// Resolves the converter that handles the specified type, applying the type-level converter attribute, the
    /// registered converters, and finally the built-in converters, in that order.
    /// </summary>
    /// <param name="typeToConvert">The type to resolve a converter for.</param>
    /// <returns>The concrete converter for the type.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="typeToConvert" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="NotSupportedException">Thrown when no converter handles the type.</exception>
#if TOML
    [RequiresUnreferencedCode(TomlTrimming.RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(TomlTrimming.RequiresDynamicCodeMessage)]
#elif YAML
    [RequiresUnreferencedCode("Reflection-based YAML serialization may require types that trimming cannot statically determine.")]
#endif
    public FormatConverter GetConverter(Type typeToConvert)
    {
        ThrowHelper.ThrowIfNull(typeToConvert);

        MakeReadOnly();

        // The state overload avoids allocating a delegate per call on cache hits.
        return _converterCache.GetOrAdd(typeToConvert, static (type, self) => self.ResolveConverter(type), this);
    }

    /// <summary>
    /// Freezes the options so their settings can no longer change, capturing the converter list. Called automatically
    /// before the first serialization or deserialization.
    /// </summary>
    public void MakeReadOnly() =>
        _frozenConverters ??= [.. Converters];

    /// <summary>
    /// Gets the cached metadata describing how the specified type maps to the format's keyed structure.
    /// </summary>
    /// <param name="type">The type to describe.</param>
    /// <returns>The type metadata.</returns>
    internal Serialization.Metadata.TypeMetadata GetTypeMetadata(Type type) =>
        _metadataCache.GetOrAdd(type, static (t, self) => Serialization.Metadata.MetadataResolver.Resolve(t, self), this);

    /// <summary>
    /// Instantiates the converter named by a converter attribute and adapts it to the target type.
    /// </summary>
    /// <param name="converterType">The converter type to instantiate.</param>
    /// <param name="targetType">The type the converter must handle.</param>
    /// <returns>The concrete converter.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="converterType" /> is not a converter type, lacks a public parameterless
    /// constructor, or cannot convert <paramref name="targetType" />.
    /// </exception>
    internal FormatConverter InstantiateConverter(Type converterType, Type targetType)
    {
        if (!typeof(FormatConverter).IsAssignableFrom(converterType))
            throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, FormatResourceStrings.Arg_Invalid_ConverterAttributeType, converterType));

        if (converterType.GetConstructor(Type.EmptyTypes) is null)
            throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, FormatResourceStrings.Arg_Invalid_ConverterNoParameterlessCtor, converterType));

        var converter = (FormatConverter)Activator.CreateInstance(converterType)!;
        return Materialize(converter, targetType);
    }

    /// <summary>
    /// Resolves the converter for a type without consulting the cache.
    /// </summary>
    /// <param name="type">The type to resolve a converter for.</param>
    /// <returns>The concrete converter.</returns>
    /// <exception cref="NotSupportedException">Thrown when no converter handles the type.</exception>
    private FormatConverter ResolveConverter(Type type)
    {
        ConverterAttribute? attribute = type.GetCustomAttribute<ConverterAttribute>(inherit: false);
        if (attribute is not null)
            return InstantiateConverter(attribute.ConverterType, type);

        foreach (FormatConverter converter in _frozenConverters!)
        {
            if (converter.CanConvert(type))
                return Materialize(converter, type);
        }

        foreach (FormatConverter converter in Serialization.Converters.DefaultConverters.Converters)
        {
            if (converter.CanConvert(type))
                return Materialize(converter, type);
        }

        throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, FormatResourceStrings.Op_NotSupported_NoConverter, type));
    }

    /// <summary>
    /// Resolves a converter to a concrete converter, invoking a factory when necessary.
    /// </summary>
    /// <param name="converter">The converter or factory to materialize.</param>
    /// <param name="type">The type to convert.</param>
    /// <returns>A concrete (non-factory) converter for the type.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a factory produces a converter that cannot convert the type.
    /// </exception>
    private FormatConverter Materialize(FormatConverter converter, Type type)
    {
        if (converter is not FormatConverterFactory factory)
            return converter;

        FormatConverter created = factory.CreateConverter(type, this);
        return !created.CanConvert(type)
            ? throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, FormatResourceStrings.Op_Invalid_ConverterCannotConvert, created.GetType(), type))
            : created;
    }

    /// <summary>
    /// Throws when the options have become read-only.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the options are read-only.</exception>
    internal void VerifyMutable()
    {
        if (IsReadOnly)
            throw new InvalidOperationException(FormatResourceStrings.Op_Invalid_OptionsReadOnly);
    }
}
