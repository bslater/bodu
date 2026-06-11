// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializerOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using Bodu.Text.Toml.Serialization;
using Bodu.Text.Toml.Serialization.Converters;
using Bodu.Text.Toml.Serialization.Metadata;

namespace Bodu.Text.Toml;

/// <summary>
/// Configures how values are serialized to and deserialized from TOML: the converters to use, the property naming
/// policy, the default ignore condition, and the maximum nesting depth. Mirrors
/// <see cref="System.Text.Json.JsonSerializerOptions" />.
/// </summary>
/// <remarks>
/// <para>
/// An options instance becomes read-only the first time it is used to serialize or deserialize a value; subsequent
/// attempts to change a setting throw. Resolved converters and type metadata are cached on the instance, so reusing one
/// configured options object across many operations is the efficient pattern.
/// </para>
/// <para>
/// <see cref="DefaultIgnoreCondition" /> governs when a member is omitted on write. Because TOML has no null token, a
/// member whose value is <see langword="null" /> is omitted regardless of that setting.
/// </para>
/// </remarks>
public sealed class TomlSerializerOptions
{
    /// <summary>
    /// The default maximum nesting depth.
    /// </summary>
    public const int DefaultMaxDepth = 64;

    /// <summary>
    /// The user-registered converters, consulted before the built-in converters.
    /// </summary>
    private readonly List<TomlConverter> _converters = [];

    /// <summary>
    /// The cache of concrete converters resolved per type.
    /// </summary>
    private readonly ConcurrentDictionary<Type, TomlConverter> _converterCache = new();

    /// <summary>
    /// The cache of resolved type metadata.
    /// </summary>
    private readonly ConcurrentDictionary<Type, TypeMetadata> _metadataCache = new();

    /// <summary>
    /// The frozen snapshot of user converters captured when the options became read-only, or <see langword="null" />
    /// while mutable.
    /// </summary>
    private TomlConverter[]? _frozenConverters;

    /// <summary>
    /// The configured property naming policy.
    /// </summary>
    private TomlNamingPolicy? _namingPolicy;

    /// <summary>
    /// Whether property-name matching ignores case when reading.
    /// </summary>
    private bool _caseInsensitive = true;

    /// <summary>
    /// Whether public fields are surfaced as serializable members.
    /// </summary>
    private bool _includeFields;

    /// <summary>
    /// The serializer-wide default condition under which a member is omitted on write.
    /// </summary>
    private TomlIgnoreCondition _defaultIgnoreCondition = TomlIgnoreCondition.Never;

    /// <summary>
    /// The serializer-wide handling for a dictionary key that maps to no member when reading.
    /// </summary>
    private TomlUnmappedMemberHandling _unmappedMemberHandling = TomlUnmappedMemberHandling.Skip;

    /// <summary>
    /// The serializer-wide preference for replacing or populating a member's value when reading.
    /// </summary>
    private TomlObjectCreationHandling _preferredObjectCreationHandling = TomlObjectCreationHandling.Replace;

    /// <summary>
    /// The maximum nesting depth.
    /// </summary>
    private int _maxDepth = DefaultMaxDepth;

    /// <summary>
    /// The TOML specification version whose grammar is accepted when deserializing.
    /// </summary>
    private TomlSpecVersion _specVersion = TomlSpecVersion.V1_0;

    /// <summary>
    /// The representation used when writing a byte array.
    /// </summary>
    private TomlByteArrayHandling _byteArrayHandling = TomlByteArrayHandling.IntegerArray;

    /// <summary>
    /// The representation used when writing a decimal value.
    /// </summary>
    private TomlDecimalHandling _decimalHandling = TomlDecimalHandling.Float;

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlSerializerOptions" /> class with default (general-purpose)
    /// settings.
    /// </summary>
    public TomlSerializerOptions()
        : this(TomlSerializerDefaults.General)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlSerializerOptions" /> class with a base set of defaults
    /// appropriate for the specified usage scenario. Mirrors the
    /// <see cref="System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults)" /> constructor.
    /// </summary>
    /// <param name="defaults">The base defaults to apply.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="defaults" /> is not a defined <see cref="TomlSerializerDefaults" /> value.
    /// </exception>
    /// <remarks>
    /// <see cref="TomlSerializerDefaults.Web" /> selects camel-case property naming and case-insensitive property-name
    /// matching; <see cref="TomlSerializerDefaults.General" /> leaves the defaults unchanged.
    /// </remarks>
    public TomlSerializerOptions(TomlSerializerDefaults defaults)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(defaults);

        if (defaults == TomlSerializerDefaults.Web)
        {
            _namingPolicy = TomlNamingPolicy.CamelCase;
            _caseInsensitive = true;
        }
    }

    /// <summary>
    /// Gets the list of user-registered converters, consulted in order before the built-in converters.
    /// </summary>
    /// <returns>The mutable converter list while the options are mutable.</returns>
    public IList<TomlConverter> Converters => _converters;

    /// <summary>
    /// Gets or sets the policy that translates member names to their serialized dictionary-key form.
    /// </summary>
    /// <value>The naming policy, or <see langword="null" /> to use member names unchanged.</value>
    /// <returns>The configured naming policy.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the options are read-only.</exception>
    public TomlNamingPolicy? PropertyNamingPolicy
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
    /// Gets or sets a value indicating whether public fields are surfaced as serializable members alongside properties.
    /// Mirrors <see cref="System.Text.Json.JsonSerializerOptions.IncludeFields" />.
    /// </summary>
    /// <value>
    /// <see langword="true" /> to serialize and deserialize public fields; otherwise <see langword="false" />. The
    /// default is <see langword="false" />.
    /// </value>
    /// <returns>Whether public fields participate in serialization.</returns>
    /// <remarks>
    /// A public field annotated with <see cref="Serialization.TomlIncludeAttribute" /> participates regardless of this
    /// setting, mirroring how <see cref="System.Text.Json.Serialization.JsonIncludeAttribute" /> opts an individual
    /// field in. Fields honor the property naming policy, name and order attributes, ignore conditions, and
    /// required-member enforcement exactly like properties; a <see langword="readonly" /> field is written but never
    /// assigned on read.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when the options are read-only.</exception>
    public bool IncludeFields
    {
        get => _includeFields;
        set
        {
            VerifyMutable();
            _includeFields = value;
        }
    }

    /// <summary>
    /// Gets or sets the default condition that determines when a member is omitted on write, applied to every member
    /// that does not carry its own <see cref="Serialization.TomlIgnoreAttribute" />.
    /// </summary>
    /// <value>The default ignore condition; <see cref="TomlIgnoreCondition.Never" /> by default.</value>
    /// <returns>The configured default ignore condition.</returns>
    /// <remarks>
    /// Because TOML has no null token, a member whose value is <see langword="null" /> is omitted from the output
    /// regardless of this setting, so <see cref="TomlIgnoreCondition.Never" /> behaves like
    /// <see cref="TomlIgnoreCondition.WhenWritingNull" /> for null values specifically.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the value is undefined, or when it is <see cref="TomlIgnoreCondition.Always" />, which is not a
    /// valid default.
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown when the options are read-only.</exception>
    public TomlIgnoreCondition DefaultIgnoreCondition
    {
        get => _defaultIgnoreCondition;
        set
        {
            VerifyMutable();
            ThrowHelper.ThrowIfEnumValueIsUndefined(value);
            if (value == TomlIgnoreCondition.Always)
                throw new ArgumentOutOfRangeException(nameof(value), TomlResourceStrings.Arg_OutOfRange_DefaultIgnoreConditionAlways);

            _defaultIgnoreCondition = value;
        }
    }

    /// <summary>
    /// Gets or sets the serializer-wide handling for a dictionary key that maps to no member of the target type when
    /// reading, applied to every type that does not carry its own
    /// <see cref="Serialization.TomlUnmappedMemberHandlingAttribute" />. Mirrors
    /// <see cref="System.Text.Json.JsonSerializerOptions.UnmappedMemberHandling" />.
    /// </summary>
    /// <value>The unmapped-member handling; <see cref="TomlUnmappedMemberHandling.Skip" /> by default.</value>
    /// <returns>The configured unmapped-member handling.</returns>
    /// <remarks>
    /// A type that declares an extension-data member captures unmapped keys into that member, which takes precedence
    /// over this setting, so a key absorbed by extension data never triggers
    /// <see cref="TomlUnmappedMemberHandling.Disallow" />.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is undefined.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the options are read-only.</exception>
    public TomlUnmappedMemberHandling UnmappedMemberHandling
    {
        get => _unmappedMemberHandling;
        set
        {
            VerifyMutable();
            ThrowHelper.ThrowIfEnumValueIsUndefined(value);

            _unmappedMemberHandling = value;
        }
    }

    /// <summary>
    /// Gets or sets the serializer-wide preference for whether a member's value is replaced with a freshly created
    /// instance or populated when reading, applied to every type and member that does not carry its own
    /// <see cref="Serialization.TomlObjectCreationHandlingAttribute" />. Mirrors
    /// <see cref="System.Text.Json.JsonSerializerOptions.PreferredObjectCreationHandling" />.
    /// </summary>
    /// <value>
    /// The preferred object-creation handling; <see cref="TomlObjectCreationHandling.Replace" /> by default.
    /// </value>
    /// <returns>The configured preferred object-creation handling.</returns>
    /// <remarks>
    /// <see cref="TomlObjectCreationHandling.Populate" /> applies only to collection and dictionary members whose
    /// existing value is non-<see langword="null" />; in every other case the serializer replaces the value.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is undefined.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the options are read-only.</exception>
    public TomlObjectCreationHandling PreferredObjectCreationHandling
    {
        get => _preferredObjectCreationHandling;
        set
        {
            VerifyMutable();
            ThrowHelper.ThrowIfEnumValueIsUndefined(value);

            _preferredObjectCreationHandling = value;
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
    /// Gets or sets the TOML specification version whose grammar is accepted when deserializing.
    /// </summary>
    /// <value>The specification version; <see cref="TomlSpecVersion.V1_0" /> by default.</value>
    /// <returns>The configured specification version.</returns>
    /// <remarks>
    /// The version affects only parsing; the writer always emits text that is valid under both supported versions.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is undefined.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the options are read-only.</exception>
    public TomlSpecVersion SpecVersion
    {
        get => _specVersion;
        set
        {
            VerifyMutable();
            ThrowHelper.ThrowIfEnumValueIsUndefined(value);
            _specVersion = value;
        }
    }

    /// <summary>
    /// Gets or sets the representation used when writing a <see cref="byte" /> array.
    /// </summary>
    /// <value>The byte-array handling; <see cref="TomlByteArrayHandling.IntegerArray" /> by default.</value>
    /// <returns>The configured byte-array handling.</returns>
    /// <remarks>
    /// The setting controls only how a byte array is written; on read the serializer accepts both an array of integers
    /// and a Base64 basic string regardless of this value.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is undefined.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the options are read-only.</exception>
    public TomlByteArrayHandling ByteArrayHandling
    {
        get => _byteArrayHandling;
        set
        {
            VerifyMutable();
            ThrowHelper.ThrowIfEnumValueIsUndefined(value);
            _byteArrayHandling = value;
        }
    }

    /// <summary>
    /// Gets or sets the representation used when writing a <see cref="decimal" /> value.
    /// </summary>
    /// <value>The decimal handling; <see cref="TomlDecimalHandling.Float" /> by default.</value>
    /// <returns>The configured decimal handling.</returns>
    /// <remarks>
    /// The setting controls only how a decimal is written; on read the serializer accepts a TOML float, integer, or
    /// string regardless of this value. <see cref="TomlDecimalHandling.Float" /> maps to TOML's native float form but
    /// loses precision beyond what an IEEE 754 binary64 value can hold; <see cref="TomlDecimalHandling.String" />
    /// preserves the value exactly at the cost of a quoted representation.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is undefined.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the options are read-only.</exception>
    public TomlDecimalHandling DecimalHandling
    {
        get => _decimalHandling;
        set
        {
            VerifyMutable();
            ThrowHelper.ThrowIfEnumValueIsUndefined(value);
            _decimalHandling = value;
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
    public TomlConverter GetConverter(Type typeToConvert)
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
    /// Gets the cached metadata describing how the specified type maps to a TOML table.
    /// </summary>
    /// <param name="type">The type to describe.</param>
    /// <returns>The type metadata.</returns>
    internal TypeMetadata GetTypeMetadata(Type type) =>
        _metadataCache.GetOrAdd(type, t => MetadataResolver.Resolve(t, this));

    /// <summary>
    /// Determines whether the specified document root type maps to a TOML table, which a TOML document's root must be.
    /// </summary>
    /// <param name="type">The candidate document root type.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="type" /> is serialized as a table (a mapped object or a dictionary
    /// with a supported key type); otherwise <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// The classification resolves the type's converter and inspects its kind, so a user converter that produces a
    /// table is accepted at the root and a scalar, array, or enumeration converter is rejected. A
    /// <see cref="Nodes.TomlNode" /> root is also accepted, deferring the table-root check to the node, which enforces
    /// it when written.
    /// </remarks>
    internal bool RootMapsToTable(Type type)
    {
        if (typeof(Nodes.TomlNode).IsAssignableFrom(type))
            return true;

        TomlConverter converter = GetConverter(type);
        Type converterType = converter.GetType();
        if (!converterType.IsGenericType)
            return false;

        Type definition = converterType.GetGenericTypeDefinition();
        return definition == typeof(ObjectConverter<>) || definition == typeof(DictionaryConverter<,,>);
    }

    /// <summary>
    /// Instantiates the converter named by a converter attribute and adapts it to the target type.
    /// </summary>
    /// <param name="converterType">The converter type to instantiate.</param>
    /// <param name="targetType">The type the converter must handle.</param>
    /// <returns>The concrete converter.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="converterType" /> is not a <see cref="TomlConverter" />, lacks a public
    /// parameterless constructor, or cannot convert <paramref name="targetType" />.
    /// </exception>
    internal TomlConverter InstantiateConverter(Type converterType, Type targetType)
    {
        if (!typeof(TomlConverter).IsAssignableFrom(converterType))
            throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Arg_Invalid_ConverterAttributeType, converterType));

        if (converterType.GetConstructor(Type.EmptyTypes) is null)
            throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Arg_Invalid_ConverterNoParameterlessCtor, converterType));

        var converter = (TomlConverter)Activator.CreateInstance(converterType) !;
        return Materialize(converter, targetType);
    }

    /// <summary>
    /// Resolves the converter for a type without consulting the cache.
    /// </summary>
    /// <param name="type">The type to resolve a converter for.</param>
    /// <returns>The concrete converter.</returns>
    /// <exception cref="NotSupportedException">Thrown when no converter handles the type.</exception>
    private TomlConverter ResolveConverter(Type type)
    {
        TomlConverterAttribute? attribute = type.GetCustomAttribute<TomlConverterAttribute>(inherit: false);
        if (attribute is not null)
            return InstantiateConverter(attribute.ConverterType, type);

        foreach (TomlConverter converter in _frozenConverters!)
        {
            if (converter.CanConvert(type))
                return Materialize(converter, type);
        }

        foreach (TomlConverter converter in DefaultConverters.Converters)
        {
            if (converter.CanConvert(type))
                return Materialize(converter, type);
        }

        throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_NotSupported_NoConverter, type));
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
    private TomlConverter Materialize(TomlConverter converter, Type type)
    {
        if (converter is not TomlConverterFactory factory)
            return converter;

        TomlConverter created = factory.CreateConverter(type, this);
        return !created.CanConvert(type)
            ? throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_ConverterCannotConvert, created.GetType(), type))
            : created;
    }

    /// <summary>
    /// Throws when the options have become read-only.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the options are read-only.</exception>
    private void VerifyMutable()
    {
        if (IsReadOnly)
            throw new InvalidOperationException(TomlResourceStrings.Op_Invalid_OptionsReadOnly);
    }
}
