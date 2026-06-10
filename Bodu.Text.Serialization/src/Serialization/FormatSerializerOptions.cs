// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FormatSerializerOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using Bodu.Text.Serialization.Converters;
using Bodu.Text.Serialization.Metadata;

namespace Bodu.Text.Serialization;

/// <summary>
/// Configures how values are serialized and deserialized: the converters to use, the property naming policy, null
/// handling, and the maximum nesting depth. Format-specific options derive from this type to add their own settings.
/// Mirrors <see cref="System.Text.Json.JsonSerializerOptions" />.
/// </summary>
/// <remarks>
/// An options instance becomes read-only the first time it is used to serialize or deserialize a value; subsequent
/// attempts to change a setting throw. Resolved converters and type metadata are cached on the instance, so reusing one
/// configured options object across many operations is the efficient pattern.
/// </remarks>
public class FormatSerializerOptions
{
    /// <summary>
    /// The default maximum nesting depth.
    /// </summary>
    public const int DefaultMaxDepth = 64;

    /// <summary>
    /// The user-registered converters, consulted before the built-in converters.
    /// </summary>
    private readonly List<FormatConverter> _converters = [];

    /// <summary>
    /// The cache of concrete converters resolved per type.
    /// </summary>
    private readonly ConcurrentDictionary<Type, FormatConverter> _converterCache = new();

    /// <summary>
    /// The cache of resolved type metadata.
    /// </summary>
    private readonly ConcurrentDictionary<Type, TypeMetadata> _metadataCache = new();

    /// <summary>
    /// The frozen snapshot of user converters captured when the options became read-only, or <see langword="null" />
    /// while mutable.
    /// </summary>
    private FormatConverter[]? _frozenConverters;

    /// <summary>
    /// The configured property naming policy.
    /// </summary>
    private FormatNamingPolicy? _namingPolicy;

    /// <summary>
    /// Whether property-name matching ignores case when reading.
    /// </summary>
    private bool _caseInsensitive = true;

    /// <summary>
    /// The null-handling policy.
    /// </summary>
    private FormatNullHandling _nullHandling = FormatNullHandling.IgnoreOnWrite;

    /// <summary>
    /// The maximum nesting depth.
    /// </summary>
    private int _maxDepth = DefaultMaxDepth;

    /// <summary>
    /// Initializes a new instance of the <see cref="FormatSerializerOptions" /> class with default settings.
    /// </summary>
    public FormatSerializerOptions()
    {
    }

    /// <summary>
    /// Gets the list of user-registered converters, consulted in order before the built-in converters.
    /// </summary>
    /// <returns>The mutable converter list while the options are mutable.</returns>
    public IList<FormatConverter> Converters => _converters;

    /// <summary>
    /// Gets or sets the policy that translates member names to their serialized form.
    /// </summary>
    /// <value>The naming policy, or <see langword="null" /> to use member names unchanged.</value>
    /// <returns>The configured naming policy.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the options are read-only.</exception>
    public FormatNamingPolicy? PropertyNamingPolicy
    {
        get => _namingPolicy;
        set
        {
            VerifyMutable();
            _namingPolicy = value;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether property-name matching ignores case when reading.
    /// </summary>
    /// <value>
    /// <see langword="true" /> to match case-insensitively; otherwise <see langword="false" />. The default is
    /// <see langword="true" />.
    /// </value>
    /// <returns>Whether property-name matching ignores case.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the options are read-only.</exception>
    public bool PropertyNameCaseInsensitive
    {
        get => _caseInsensitive;
        set
        {
            VerifyMutable();
            _caseInsensitive = value;
        }
    }

    /// <summary>
    /// Gets or sets how a member whose value is <see langword="null" /> is treated when writing.
    /// </summary>
    /// <value>The null-handling policy; <see cref="FormatNullHandling.IgnoreOnWrite" /> by default.</value>
    /// <returns>The configured null-handling policy.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the options are read-only.</exception>
    public FormatNullHandling NullHandling
    {
        get => _nullHandling;
        set
        {
            VerifyMutable();
            ThrowHelper.ThrowIfEnumValueIsUndefined(value);
            _nullHandling = value;
        }
    }

    /// <summary>
    /// Gets or sets the maximum nesting depth permitted while serializing or deserializing.
    /// </summary>
    /// <value>The maximum depth; <see cref="DefaultMaxDepth" /> when set to zero.</value>
    /// <returns>The configured maximum depth.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is negative.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the options are read-only.</exception>
    public int MaxDepth
    {
        get => _maxDepth;
        set
        {
            VerifyMutable();
            ThrowHelper.ThrowIfNegative(value);
            _maxDepth = value == 0 ? DefaultMaxDepth : value;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the options have become read-only.
    /// </summary>
    /// <returns><see langword="true" /> once the options have been used; otherwise <see langword="false" />.</returns>
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
    public FormatConverter GetConverter(Type typeToConvert)
    {
        ThrowHelper.ThrowIfNull(typeToConvert);

        MakeReadOnly();
        return _converterCache.GetOrAdd(typeToConvert, ResolveConverter);
    }

    /// <summary>
    /// Freezes the options so their settings can no longer change, capturing the converter list. Called automatically
    /// before the first serialization or deserialization.
    /// </summary>
    public void MakeReadOnly() =>
        _frozenConverters ??= [.. _converters];

    /// <summary>
    /// Gets the cached metadata describing how the specified type maps to an object structure.
    /// </summary>
    /// <param name="type">The type to describe.</param>
    /// <returns>The type metadata.</returns>
    internal TypeMetadata GetTypeMetadata(Type type) =>
        _metadataCache.GetOrAdd(type, t => MetadataResolver.Resolve(t, this));

    /// <summary>
    /// Instantiates the converter named by a converter attribute and adapts it to the target type.
    /// </summary>
    /// <param name="converterType">The converter type to instantiate.</param>
    /// <param name="targetType">The type the converter must handle.</param>
    /// <returns>The concrete converter.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="converterType" /> is not a <see cref="FormatConverter" />, lacks a public
    /// parameterless constructor, or cannot convert <paramref name="targetType" />.
    /// </exception>
    internal FormatConverter InstantiateConverter(Type converterType, Type targetType)
    {
        if (!typeof(FormatConverter).IsAssignableFrom(converterType))
            throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, SerializationResourceStrings.Arg_Invalid_ConverterAttributeType, converterType));

        if (converterType.GetConstructor(Type.EmptyTypes) is null)
            throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, SerializationResourceStrings.Arg_Invalid_ConverterNoParameterlessCtor, converterType));

        var converter = (FormatConverter)Activator.CreateInstance(converterType) !;
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
        FormatConverterAttribute? attribute = type.GetCustomAttribute<FormatConverterAttribute>(inherit: false);
        if (attribute is not null)
            return InstantiateConverter(attribute.ConverterType, type);

        foreach (FormatConverter converter in _frozenConverters!)
        {
            if (converter.CanConvert(type))
                return Materialize(converter, type);
        }

        foreach (FormatConverter converter in DefaultConverters.Converters)
        {
            if (converter.CanConvert(type))
                return Materialize(converter, type);
        }

        throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, SerializationResourceStrings.Op_NotSupported_NoConverter, type));
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
            ? throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, SerializationResourceStrings.Op_Invalid_ConverterCannotConvert, created.GetType(), type))
            : created;
    }

    /// <summary>
    /// Throws when the options have become read-only.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the options are read-only.</exception>
    private void VerifyMutable()
    {
        if (IsReadOnly)
            throw new InvalidOperationException(SerializationResourceStrings.Op_Invalid_OptionsReadOnly);
    }
}
