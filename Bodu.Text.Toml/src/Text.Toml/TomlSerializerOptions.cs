// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializerOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using Bodu.Text.Toml.Serialization;
using Bodu.Text.Toml.Serialization.Converters;
using Bodu.Text.Toml.Serialization.Metadata;

namespace Bodu.Text.Toml;

/// <summary>
/// Configures how values are serialized to and deserialized from TOML: the converters to use, the property naming
/// policy, the default ignore condition, and the maximum nesting depth.
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
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Configure once and reuse across calls; resolved converters are cached on the instance.
/// var options = new TomlSerializerOptions
/// {
///     PropertyNamingPolicy = NamingPolicy.SnakeCaseLower,
///     DefaultIgnoreCondition = IgnoreCondition.WhenWritingDefault,
/// };
/// options.Converters.Add(new TomlStringEnumConverter(NamingPolicy.SnakeCaseLower, allowIntegerValues: false));
///
/// string text = TomlSerializer.Serialize(config, options);
///]]>
/// </code>
/// </example>
public sealed partial class TomlSerializerOptions
{
    /// <summary>The default maximum container nesting depth applied when <see cref="MaxDepth" /> is left at zero. It is also the library's hard ceiling (<see cref="TomlLimits.AbsoluteMaxDepth" />): a configured depth is never effective beyond this value, because the recursive reader, writer, and serializer must stay within a safe call-stack budget.</summary>
    public const int DefaultMaxDepth = 64;

    /// <summary>The user-registered converters, consulted before the built-in converters. The list rejects a <see langword="null" /> entry and refuses every mutation once the options have become read-only.</summary>
    private readonly ConverterList _converters;

    /// <summary>The cache of concrete converters resolved per type.</summary>
    private readonly ConcurrentDictionary<Type, TomlConverter> _converterCache = new();

    /// <summary>The cache of resolved type metadata.</summary>
    private readonly ConcurrentDictionary<Type, TypeMetadata> _metadataCache = new();

    /// <summary>The frozen snapshot of user converters captured when the options became read-only, or <see langword="null" /> while mutable.</summary>
    private TomlConverter[]? _frozenConverters;

    /// <summary>The configured property naming policy.</summary>
    private NamingPolicy? _namingPolicy;

    /// <summary>Whether property-name matching ignores case when reading. Case-sensitive by default for general options, matching <see cref="System.Text.Json.JsonSerializer" /> and TOML's case-sensitive keys; the <see cref="TomlSerializerDefaults.Web" /> preset enables case-insensitive matching.</summary>
    private bool _caseInsensitive;

    /// <summary>Whether public fields are surfaced as serializable members.</summary>
    private bool _includeFields;

    /// <summary>The serializer-wide default condition under which a member is omitted on write.</summary>
    private IgnoreCondition _defaultIgnoreCondition = IgnoreCondition.Never;

    /// <summary>The serializer-wide handling for a dictionary key that maps to no member when reading.</summary>
    private UnmappedMemberHandling _unmappedMemberHandling = UnmappedMemberHandling.Skip;

    /// <summary>The serializer-wide preference for replacing or populating a member's value when reading.</summary>
    private ObjectCreationHandling _preferredObjectCreationHandling = ObjectCreationHandling.Replace;

    /// <summary>The maximum nesting depth.</summary>
    private int _maxDepth = DefaultMaxDepth;

    /// <summary>The TOML specification version whose grammar is accepted when deserializing.</summary>
    private TomlSpecVersion _specVersion = TomlSpecVersion.V1_0;

    /// <summary>The representation used when writing a byte array.</summary>
    private TomlByteArrayHandling _byteArrayHandling = TomlByteArrayHandling.IntegerArray;

    /// <summary>The representation used when writing a decimal value.</summary>
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
    /// appropriate for the specified usage scenario.
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

        _converters = new ConverterList(this);

        if (defaults == TomlSerializerDefaults.Web)
        {
            _namingPolicy = NamingPolicy.CamelCase;
            _caseInsensitive = true;
        }
    }

    /// <summary>
    /// Gets the list of user-registered converters, consulted in order before the built-in converters.
    /// </summary>
    /// <value>The mutable converter list while the options are mutable.</value>
    public IList<TomlConverter> Converters => _converters;

    /// <summary>
    /// Gets or sets the policy that translates member names to their serialized dictionary-key form.
    /// </summary>
    /// <value>The naming policy, or <see langword="null" /> to use member names unchanged.</value>
    /// <exception cref="InvalidOperationException">Thrown when the options are read-only.</exception>
    public NamingPolicy? PropertyNamingPolicy
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
    /// <see langword="false" /> for general options and <see langword="true" /> under
    /// <see cref="TomlSerializerDefaults.Web" />.
    /// </value>
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
    /// </summary>
    /// <value>
    /// <see langword="true" /> to serialize and deserialize public fields; otherwise <see langword="false" />. The
    /// default is <see langword="false" />.
    /// </value>
    /// <remarks>
    /// A public field annotated with <see cref="IncludeAttribute" /> participates regardless of this
    /// setting. Fields honor the property naming policy, name and order attributes, ignore conditions, and
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
    /// that does not carry its own <see cref="IgnoreAttribute" />.
    /// </summary>
    /// <value>The default ignore condition; <see cref="IgnoreCondition.Never" /> by default.</value>
    /// <remarks>
    /// Because TOML has no null token, a member whose value is <see langword="null" /> is omitted from the output
    /// regardless of this setting, so <see cref="IgnoreCondition.Never" /> behaves like
    /// <see cref="IgnoreCondition.WhenWritingNull" /> for null values specifically.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the value is undefined, or when it is <see cref="IgnoreCondition.Always" />, which is not a
    /// valid default.
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown when the options are read-only.</exception>
    public IgnoreCondition DefaultIgnoreCondition
    {
        get => _defaultIgnoreCondition;
        set
        {
            VerifyMutable();
            ThrowHelper.ThrowIfEnumValueIsUndefined(value);
            if (value == IgnoreCondition.Always)
                throw new ArgumentOutOfRangeException(nameof(value), TomlResourceStrings.Arg_OutOfRange_DefaultIgnoreConditionAlways);

            _defaultIgnoreCondition = value;
        }
    }

    /// <summary>
    /// Gets or sets the serializer-wide handling for a dictionary key that maps to no member of the target type when
    /// reading, applied to every type that does not carry its own
    /// <see cref="UnmappedMemberHandlingAttribute" />.
    /// </summary>
    /// <value>The unmapped-member handling; <see cref="UnmappedMemberHandling.Skip" /> by default.</value>
    /// <remarks>
    /// A type that declares an extension-data member captures unmapped keys into that member, which takes precedence
    /// over this setting, so a key absorbed by extension data never triggers
    /// <see cref="UnmappedMemberHandling.Disallow" />.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is undefined.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the options are read-only.</exception>
    public UnmappedMemberHandling UnmappedMemberHandling
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
    /// <see cref="ObjectCreationHandlingAttribute" />.
    /// </summary>
    /// <value>
    /// The preferred object-creation handling; <see cref="ObjectCreationHandling.Replace" /> by default.
    /// </value>
    /// <remarks>
    /// <see cref="ObjectCreationHandling.Populate" /> applies only to collection and dictionary members whose
    /// existing value is non-<see langword="null" />; in every other case the serializer replaces the value.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is undefined.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the options are read-only.</exception>
    public ObjectCreationHandling PreferredObjectCreationHandling
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
    /// Gets or sets the maximum container nesting depth permitted while serializing or deserializing.
    /// </summary>
    /// <value>The maximum depth; <see cref="DefaultMaxDepth" /> when set to zero.</value>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is negative.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the options are read-only.</exception>
    /// <remarks>
    /// <para>
    /// The limit bounds how deeply tables and arrays may nest. It is reached when serializing an object graph — or
    /// deserializing a document — whose containers nest more levels deep than the effective limit, for example a chain
    /// of objects each holding the next, a dictionary of dictionaries, or arrays within arrays. Crossing it is reported
    /// as a catchable failure: <see cref="TomlSerializationException" /> while serializing, or
    /// <see cref="TomlFormatException" /> while deserializing. A reference cycle (an object reachable from itself) is a
    /// distinct condition reported separately as a <see cref="TomlSerializationException" /> identifying the cycle, not
    /// as a depth-limit failure.
    /// </para>
    /// <para>
    /// Although any non-negative value is accepted here, the <em>effective</em> limit is clamped to the library's hard
    /// ceiling, <see cref="TomlLimits.AbsoluteMaxDepth" /> (64); setting a larger value — even
    /// <see cref="int.MaxValue" /> — does not raise it. The ceiling exists because the serializer and parser recurse
    /// one call-stack frame per nested container, so an unbounded depth on hostile or malformed input would exhaust the
    /// call stack and terminate the process with an uncatchable <see cref="StackOverflowException" />. Clamping
    /// converts that into the catchable exceptions above. Lowering this value below the default tightens the limit;
    /// raising it has no effect beyond the ceiling.
    /// </para>
    /// </remarks>
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
    /// <value><see langword="true" /> once the options have been used; otherwise <see langword="false" />.</value>
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
    [RequiresUnreferencedCode(TomlTrimming.RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(TomlTrimming.RequiresDynamicCodeMessage)]
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
    /// it when written; a <see cref="Document.TomlElement" />, <see cref="Document.TomlDocument" />, or
    /// <see cref="object" /> root likewise defers to the writer's own root state machine, because the written kind is
    /// known only at write time.
    /// </remarks>
    internal bool RootMapsToTable(Type type)
    {
        if (typeof(Nodes.TomlNode).IsAssignableFrom(type))
            return true;

        if (type == typeof(Document.TomlElement) || type == typeof(Document.TomlDocument) || type == typeof(object))
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

        var converter = (TomlConverter)Activator.CreateInstance(converterType)!;
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
        ConverterAttribute? attribute = type.GetCustomAttribute<ConverterAttribute>(inherit: false);
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
