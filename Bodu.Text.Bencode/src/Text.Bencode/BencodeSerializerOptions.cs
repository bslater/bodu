// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeSerializerOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using Bodu.Text.Bencode.Serialization;
using Bodu.Text.Bencode.Serialization.Converters;
using Bodu.Text.Bencode.Serialization.Metadata;

namespace Bodu.Text.Bencode;

/// <summary>
/// Configures how values are serialized to and deserialized from Bencode: the converters to use, the property naming
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
/// <see cref="DefaultIgnoreCondition" /> governs when a member is omitted on write. Because Bencode has no null token,
/// a member whose value is <see langword="null" /> is omitted regardless of that setting.
/// </para>
/// </remarks>
public sealed class BencodeSerializerOptions
{
    /// <summary>
    /// The default maximum nesting depth.
    /// </summary>
    public const int DefaultMaxDepth = 64;

    /// <summary>
    /// The user-registered converters, consulted before the built-in converters.
    /// </summary>
    private readonly List<BencodeConverter> _converters = [];

    /// <summary>
    /// The cache of concrete converters resolved per type.
    /// </summary>
    private readonly ConcurrentDictionary<Type, BencodeConverter> _converterCache = new();

    /// <summary>
    /// The cache of resolved type metadata.
    /// </summary>
    private readonly ConcurrentDictionary<Type, TypeMetadata> _metadataCache = new();

    /// <summary>
    /// The frozen snapshot of user converters captured when the options became read-only, or <see langword="null" />
    /// while mutable.
    /// </summary>
    private BencodeConverter[]? _frozenConverters;

    /// <summary>
    /// The configured property naming policy.
    /// </summary>
    private BencodeNamingPolicy? _namingPolicy;

    /// <summary>
    /// Whether property-name matching ignores case when reading.
    /// </summary>
    private bool _caseInsensitive = true;

    /// <summary>
    /// Whether public fields are surfaced as serializable members.
    /// </summary>
    private bool _includeFields;

    /// <summary>
    /// Whether deserialization accepts dictionaries whose keys are not in ascending bytewise order.
    /// </summary>
    private bool _allowUnsortedKeys;

    /// <summary>
    /// Whether deserialization accepts dictionaries carrying more than one entry for the same key.
    /// </summary>
    private bool _allowDuplicateKeys;

    /// <summary>
    /// The serializer-wide default condition under which a member is omitted on write.
    /// </summary>
    private BencodeIgnoreCondition _defaultIgnoreCondition = BencodeIgnoreCondition.Never;

    /// <summary>
    /// The serializer-wide handling for a dictionary key that maps to no member when reading.
    /// </summary>
    private BencodeUnmappedMemberHandling _unmappedMemberHandling = BencodeUnmappedMemberHandling.Skip;

    /// <summary>
    /// The serializer-wide preference for replacing or populating a member's value when reading.
    /// </summary>
    private BencodeObjectCreationHandling _preferredObjectCreationHandling = BencodeObjectCreationHandling.Replace;

    /// <summary>
    /// The maximum nesting depth.
    /// </summary>
    private int _maxDepth = DefaultMaxDepth;

    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeSerializerOptions" /> class with default (general-purpose)
    /// settings.
    /// </summary>
    public BencodeSerializerOptions()
        : this(BencodeSerializerDefaults.General)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeSerializerOptions" /> class with a base set of defaults
    /// appropriate for the specified usage scenario. Mirrors the
    /// <see cref="System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults)" /> constructor.
    /// </summary>
    /// <param name="defaults">The base defaults to apply.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="defaults" /> is not a defined <see cref="BencodeSerializerDefaults" /> value.
    /// </exception>
    /// <remarks>
    /// <see cref="BencodeSerializerDefaults.Web" /> selects camel-case property naming and case-insensitive
    /// property-name matching; <see cref="BencodeSerializerDefaults.General" /> leaves the defaults unchanged.
    /// </remarks>
    public BencodeSerializerOptions(BencodeSerializerDefaults defaults)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(defaults);

        if (defaults == BencodeSerializerDefaults.Web)
        {
            _namingPolicy = BencodeNamingPolicy.CamelCase;
            _caseInsensitive = true;
        }
    }

    /// <summary>
    /// Gets the list of user-registered converters, consulted in order before the built-in converters.
    /// </summary>
    /// <returns>The mutable converter list while the options are mutable.</returns>
    public IList<BencodeConverter> Converters => _converters;

    /// <summary>
    /// Gets or sets the policy that translates member names to their serialized dictionary-key form.
    /// </summary>
    /// <value>The naming policy, or <see langword="null" /> to use member names unchanged.</value>
    /// <returns>The configured naming policy.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the options are read-only.</exception>
    public BencodeNamingPolicy? PropertyNamingPolicy
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
    /// <remarks>
    /// The lenient default is a deliberate divergence from
    /// <see cref="System.Text.Json.JsonSerializerOptions.PropertyNameCaseInsensitive" />, whose general default is
    /// <see langword="false" />: Bencode wire keys are raw bytes with no prescribed casing convention, so reads
    /// tolerate casing variation by default. The option affects reading only; written keys always use the resolved wire
    /// name unchanged.
    /// </remarks>
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
    /// Gets or sets a value indicating whether deserialization accepts dictionaries whose keys are not in ascending
    /// bytewise order.
    /// </summary>
    /// <value>
    /// <see langword="true" /> to accept unsorted keys; otherwise <see langword="false" />. The default is
    /// <see langword="false" />, rejecting unsorted keys with <see cref="BencodeFormatException" />.
    /// </value>
    /// <returns>Whether unsorted dictionary keys are accepted when reading.</returns>
    /// <remarks>
    /// BEP 3 requires keys sorted by raw byte order, but documents produced by older encoders occasionally violate
    /// this. The option affects reading only; output is always written in canonical key order.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when the options are read-only.</exception>
    public bool AllowUnsortedKeys
    {
        get => _allowUnsortedKeys;
        set
        {
            VerifyMutable();
            _allowUnsortedKeys = value;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether deserialization accepts dictionaries carrying more than one entry for
    /// the same key.
    /// </summary>
    /// <value>
    /// <see langword="true" /> to accept duplicate keys; otherwise <see langword="false" />. The default is
    /// <see langword="false" />, rejecting duplicate keys with <see cref="BencodeFormatException" />.
    /// </value>
    /// <returns>Whether duplicate dictionary keys are accepted when reading.</returns>
    /// <remarks>
    /// When enabled, repeated keys bind last-wins: the final occurrence of a key determines the member or dictionary
    /// entry value. The option affects reading only; the writer always rejects duplicate keys because canonical Bencode
    /// forbids them.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when the options are read-only.</exception>
    public bool AllowDuplicateKeys
    {
        get => _allowDuplicateKeys;
        set
        {
            VerifyMutable();
            _allowDuplicateKeys = value;
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
    /// A public field annotated with <see cref="Serialization.BencodeIncludeAttribute" /> participates regardless of
    /// this setting, mirroring how <see cref="System.Text.Json.Serialization.JsonIncludeAttribute" /> opts an
    /// individual field in. Fields honor the property naming policy, name and order attributes, ignore conditions, and
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
    /// that does not carry its own <see cref="Serialization.BencodeIgnoreAttribute" />.
    /// </summary>
    /// <value>The default ignore condition; <see cref="BencodeIgnoreCondition.Never" /> by default.</value>
    /// <returns>The configured default ignore condition.</returns>
    /// <remarks>
    /// Because Bencode has no null token, a member whose value is <see langword="null" /> is omitted from the output
    /// regardless of this setting, so <see cref="BencodeIgnoreCondition.Never" /> behaves like
    /// <see cref="BencodeIgnoreCondition.WhenWritingNull" /> for null values specifically.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the value is undefined, or when it is <see cref="BencodeIgnoreCondition.Always" />, which is not a
    /// valid default.
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown when the options are read-only.</exception>
    public BencodeIgnoreCondition DefaultIgnoreCondition
    {
        get => _defaultIgnoreCondition;
        set
        {
            VerifyMutable();
            ThrowHelper.ThrowIfEnumValueIsUndefined(value);
            if (value == BencodeIgnoreCondition.Always)
                throw new ArgumentOutOfRangeException(nameof(value), BencodeResourceStrings.Arg_OutOfRange_DefaultIgnoreConditionAlways);

            _defaultIgnoreCondition = value;
        }
    }

    /// <summary>
    /// Gets or sets the serializer-wide handling for a dictionary key that maps to no member of the target type when
    /// reading, applied to every type that does not carry its own
    /// <see cref="Serialization.BencodeUnmappedMemberHandlingAttribute" />. Mirrors
    /// <see cref="System.Text.Json.JsonSerializerOptions.UnmappedMemberHandling" />.
    /// </summary>
    /// <value>The unmapped-member handling; <see cref="BencodeUnmappedMemberHandling.Skip" /> by default.</value>
    /// <returns>The configured unmapped-member handling.</returns>
    /// <remarks>
    /// A type that declares an extension-data member captures unmapped keys into that member, which takes precedence
    /// over this setting, so a key absorbed by extension data never triggers
    /// <see cref="BencodeUnmappedMemberHandling.Disallow" />.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is undefined.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the options are read-only.</exception>
    public BencodeUnmappedMemberHandling UnmappedMemberHandling
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
    /// <see cref="Serialization.BencodeObjectCreationHandlingAttribute" />. Mirrors
    /// <see cref="System.Text.Json.JsonSerializerOptions.PreferredObjectCreationHandling" />.
    /// </summary>
    /// <value>
    /// The preferred object-creation handling; <see cref="BencodeObjectCreationHandling.Replace" /> by default.
    /// </value>
    /// <returns>The configured preferred object-creation handling.</returns>
    /// <remarks>
    /// <see cref="BencodeObjectCreationHandling.Populate" /> applies only to collection and dictionary members whose
    /// existing value is non-<see langword="null" />; in every other case the serializer replaces the value.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is undefined.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the options are read-only.</exception>
    public BencodeObjectCreationHandling PreferredObjectCreationHandling
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
    /// <remarks>
    /// The default of <see cref="DefaultMaxDepth" /> (64) matches
    /// <see cref="System.Text.Json.JsonSerializerOptions.MaxDepth" /> and is deliberately tighter than the default of
    /// 256 used by <see cref="Reader.Utf8BencodeReader" />, <see cref="Writer.Utf8BencodeWriter" />, and
    /// <see cref="Document.BencodeDocument" />: the serializer is the typical entry point for untrusted input, while
    /// the lower-level surfaces favour permissiveness for callers that manage their own limits.
    /// </remarks>
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
    public BencodeConverter GetConverter(Type typeToConvert)
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
    /// Gets the cached metadata describing how the specified type maps to a Bencode dictionary.
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
    /// Thrown when <paramref name="converterType" /> is not a <see cref="BencodeConverter" />, lacks a public
    /// parameterless constructor, or cannot convert <paramref name="targetType" />.
    /// </exception>
    internal BencodeConverter InstantiateConverter(Type converterType, Type targetType)
    {
        if (!typeof(BencodeConverter).IsAssignableFrom(converterType))
            throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, BencodeResourceStrings.Arg_Invalid_ConverterAttributeType, converterType));

        if (converterType.GetConstructor(Type.EmptyTypes) is null)
            throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, BencodeResourceStrings.Arg_Invalid_ConverterNoParameterlessCtor, converterType));

        var converter = (BencodeConverter)Activator.CreateInstance(converterType) !;
        return Materialize(converter, targetType);
    }

    /// <summary>
    /// Resolves the converter for a type without consulting the cache.
    /// </summary>
    /// <param name="type">The type to resolve a converter for.</param>
    /// <returns>The concrete converter.</returns>
    /// <exception cref="NotSupportedException">Thrown when no converter handles the type.</exception>
    private BencodeConverter ResolveConverter(Type type)
    {
        BencodeConverterAttribute? attribute = type.GetCustomAttribute<BencodeConverterAttribute>(inherit: false);
        if (attribute is not null)
            return InstantiateConverter(attribute.ConverterType, type);

        foreach (BencodeConverter converter in _frozenConverters!)
        {
            if (converter.CanConvert(type))
                return Materialize(converter, type);
        }

        foreach (BencodeConverter converter in DefaultConverters.Converters)
        {
            if (converter.CanConvert(type))
                return Materialize(converter, type);
        }

        throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, BencodeResourceStrings.Op_NotSupported_NoConverter, type));
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
    private BencodeConverter Materialize(BencodeConverter converter, Type type)
    {
        if (converter is not BencodeConverterFactory factory)
            return converter;

        BencodeConverter created = factory.CreateConverter(type, this);
        return !created.CanConvert(type)
            ? throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, BencodeResourceStrings.Op_Invalid_ConverterCannotConvert, created.GetType(), type))
            : created;
    }

    /// <summary>
    /// Throws when the options have become read-only.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the options are read-only.</exception>
    private void VerifyMutable()
    {
        if (IsReadOnly)
            throw new InvalidOperationException(BencodeResourceStrings.Op_Invalid_OptionsReadOnly);
    }
}
