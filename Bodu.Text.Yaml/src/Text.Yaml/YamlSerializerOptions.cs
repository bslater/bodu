// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Bodu.Text.Serialization;
using Bodu.Text.Yaml.Serialization;

namespace Bodu.Text.Yaml;

/// <summary>
/// Provides options that configure <see cref="YamlSerializer" />, in the manner of
/// <see cref="System.Text.Json.JsonSerializerOptions" />.
/// </summary>
/// <remarks>
/// An options instance becomes read-only the first time it is used to serialize or deserialize, or when
/// <see cref="MakeReadOnly" /> is called. After that point its settable properties throw
/// <see cref="InvalidOperationException" />, which guarantees deterministic behavior when the same instance is shared
/// across threads.
/// </remarks>
public sealed class YamlSerializerOptions
{
    /// <summary>The policy used to convert member names to YAML keys, or <see langword="null" /> for none.</summary>
    private NamingPolicy? _propertyNamingPolicy;

    /// <summary>Indicates whether public fields are serialized in addition to properties.</summary>
    private bool _includeFields;

    /// <summary>Indicates whether members with <see langword="null" /> values are omitted when serializing.</summary>
    private bool _ignoreNullValues;

    /// <summary>Indicates whether enumeration values are written as their names rather than numbers.</summary>
    private bool _writeEnumsAsStrings = true;

    /// <summary>Indicates whether member names are matched case-insensitively when deserializing.</summary>
    private bool _propertyNameCaseInsensitive;

    /// <summary>The YAML specification version whose implicit type resolution is applied while parsing.</summary>
    private YamlSpecVersion _specVersion;

    /// <summary>The policy applied to duplicate mapping keys while parsing.</summary>
    private YamlDuplicateKeyBehavior _duplicateKeyBehavior;

    /// <summary>The policy applied to the merge key (<c>&lt;&lt;</c>) while parsing.</summary>
    private YamlMergeKeyBehavior _mergeKeyBehavior;

    /// <summary>The policy applied when coercing numeric scalars to integral targets while deserializing.</summary>
    private YamlNumberHandling _numberHandling;

    /// <summary>The policy applied when a mapping key has no matching member on the target type.</summary>
    private UnmappedMemberHandling _unmappedMemberHandling;

    /// <summary>The serializer-wide default for whether a member's value is replaced or populated when reading.</summary>
    private ObjectCreationHandling _preferredObjectCreationHandling;

    /// <summary>The configured maximum nesting depth; zero or less selects the default.</summary>
    private int _maxDepth;

    /// <summary>Indicates whether the options instance has been used or frozen and can no longer change.</summary>
    private bool _isReadOnly;

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlSerializerOptions" /> class.
    /// </summary>
    public YamlSerializerOptions()
    {
        Converters = new YamlConverterCollection(this);
    }

    /// <summary>
    /// Gets the list of custom converters consulted before the built-in handling.
    /// </summary>
    /// <value>The converter collection.</value>
    public Collection<YamlConverter> Converters { get; }

    /// <summary>
    /// Gets a value indicating whether the options instance is read-only.
    /// </summary>
    /// <value>
    /// <see langword="true" /> once the instance has been used or frozen; otherwise <see langword="false" />.
    /// </value>
    public bool IsReadOnly => _isReadOnly;

    /// <summary>
    /// Gets or sets the policy used to convert member names to YAML keys.
    /// </summary>
    /// <value>The naming policy, or <see langword="null" /> to use member names verbatim.</value>
    /// <exception cref="InvalidOperationException">The options instance is read-only.</exception>
    public NamingPolicy? PropertyNamingPolicy
    {
        get => _propertyNamingPolicy;
        set { VerifyMutable(); _propertyNamingPolicy = value; }
    }

    /// <summary>
    /// Gets or sets a value indicating whether public fields are serialized in addition to properties.
    /// </summary>
    /// <value><see langword="true" /> to include public fields; otherwise <see langword="false" />.</value>
    /// <exception cref="InvalidOperationException">The options instance is read-only.</exception>
    public bool IncludeFields
    {
        get => _includeFields;
        set { VerifyMutable(); _includeFields = value; }
    }

    /// <summary>
    /// Gets or sets a value indicating whether members with a null value are omitted when writing.
    /// </summary>
    /// <value><see langword="true" /> to omit null members; otherwise <see langword="false" />.</value>
    /// <exception cref="InvalidOperationException">The options instance is read-only.</exception>
    public bool IgnoreNullValues
    {
        get => _ignoreNullValues;
        set { VerifyMutable(); _ignoreNullValues = value; }
    }

    /// <summary>
    /// Gets or sets a value indicating whether enumerations are written as their string names.
    /// </summary>
    /// <value>
    /// <see langword="true" /> to write enum names; <see langword="false" /> to write their numeric values. The default
    /// is <see langword="true" />.
    /// </value>
    /// <exception cref="InvalidOperationException">The options instance is read-only.</exception>
    public bool WriteEnumsAsStrings
    {
        get => _writeEnumsAsStrings;
        set { VerifyMutable(); _writeEnumsAsStrings = value; }
    }

    /// <summary>
    /// Gets or sets a value indicating whether YAML keys are matched to members case-insensitively.
    /// </summary>
    /// <value><see langword="true" /> to match keys case-insensitively; otherwise <see langword="false" />.</value>
    /// <exception cref="InvalidOperationException">The options instance is read-only.</exception>
    public bool PropertyNameCaseInsensitive
    {
        get => _propertyNameCaseInsensitive;
        set { VerifyMutable(); _propertyNameCaseInsensitive = value; }
    }

    /// <summary>
    /// Gets or sets the YAML specification version applied when parsing during deserialization.
    /// </summary>
    /// <value>The specification version; the default is <see cref="YamlSpecVersion.V1_2" />.</value>
    /// <exception cref="InvalidOperationException">The options instance is read-only.</exception>
    public YamlSpecVersion SpecVersion
    {
        get => _specVersion;
        set { VerifyMutable(); _specVersion = value; }
    }

    /// <summary>
    /// Gets or sets the policy applied when a mapping declares the same key more than once during deserialization.
    /// </summary>
    /// <value>The duplicate-key policy; the default is <see cref="YamlDuplicateKeyBehavior.Throw" />.</value>
    /// <exception cref="InvalidOperationException">The options instance is read-only.</exception>
    public YamlDuplicateKeyBehavior DuplicateKeyBehavior
    {
        get => _duplicateKeyBehavior;
        set { VerifyMutable(); _duplicateKeyBehavior = value; }
    }

    /// <summary>
    /// Gets or sets the policy applied to the YAML merge key (<c>&lt;&lt;</c>) during deserialization.
    /// </summary>
    /// <value>The merge-key policy; the default is <see cref="YamlMergeKeyBehavior.Expand" />.</value>
    /// <exception cref="InvalidOperationException">The options instance is read-only.</exception>
    public YamlMergeKeyBehavior MergeKeyBehavior
    {
        get => _mergeKeyBehavior;
        set { VerifyMutable(); _mergeKeyBehavior = value; }
    }

    /// <summary>
    /// Gets or sets the policy applied when coercing numeric scalars to integral targets.
    /// </summary>
    /// <value>The number-handling policy; the default is <see cref="YamlNumberHandling.Strict" />.</value>
    /// <exception cref="InvalidOperationException">The options instance is read-only.</exception>
    public YamlNumberHandling NumberHandling
    {
        get => _numberHandling;
        set { VerifyMutable(); _numberHandling = value; }
    }

    /// <summary>
    /// Gets or sets the policy applied to a YAML key that does not map to any member of the target type.
    /// </summary>
    /// <value>The unmapped-member policy; the default is <see cref="UnmappedMemberHandling.Skip" />.</value>
    /// <exception cref="InvalidOperationException">The options instance is read-only.</exception>
    public UnmappedMemberHandling UnmappedMemberHandling
    {
        get => _unmappedMemberHandling;
        set { VerifyMutable(); _unmappedMemberHandling = value; }
    }

    /// <summary>
    /// Gets or sets the serializer-wide default for whether a member's value is replaced with a freshly created
    /// instance or populated into the value the member already holds during deserialization.
    /// </summary>
    /// <value>
    /// The preferred object-creation handling; the default is <see cref="ObjectCreationHandling.Replace" />.
    /// </value>
    /// <exception cref="InvalidOperationException">The options instance is read-only.</exception>
    public ObjectCreationHandling PreferredObjectCreationHandling
    {
        get => _preferredObjectCreationHandling;
        set { VerifyMutable(); _preferredObjectCreationHandling = value; }
    }

    /// <summary>
    /// Gets or sets the maximum depth allowed when serializing an object graph and when parsing during deserialization.
    /// </summary>
    /// <value>The maximum depth. A value of zero or less selects the default of 64.</value>
    /// <exception cref="InvalidOperationException">The options instance is read-only.</exception>
    public int MaxDepth
    {
        get => _maxDepth;
        set { VerifyMutable(); _maxDepth = value; }
    }

    /// <summary>
    /// Gets the effective maximum depth, applying the default when unset and clamping to the absolute ceiling.
    /// </summary>
    /// <value>The maximum depth used by the serializer.</value>
    /// <remarks>
    /// A caller-supplied maximum is clamped to <see cref="YamlLimits.AbsoluteMaxDepth" /> — matching the reader and
    /// writer option types — so a large configured depth cannot defeat the recursion ceiling that guards against a
    /// <see cref="StackOverflowException" /> on deeply nested graphs.
    /// </remarks>
    internal int EffectiveMaxDepth =>
        _maxDepth <= 0 ? YamlLimits.DefaultMaxDepth : Math.Min(_maxDepth, YamlLimits.AbsoluteMaxDepth);

    /// <summary>
    /// Marks the options instance as read-only so that its settable properties can no longer be changed.
    /// </summary>
    public void MakeReadOnly() => _isReadOnly = true;

    /// <summary>The resolved converter per type, cached once the options are frozen; a <see langword="null" /> entry records that no custom converter applies.</summary>
    private readonly ConcurrentDictionary<Type, YamlConverter?> _converterCache = new();

    /// <summary>The member binding per type, cached once the options are frozen.</summary>
    private readonly ConcurrentDictionary<Type, YamlTypeBinding> _bindingCache = new();

    /// <summary>
    /// Finds a custom converter for the specified type.
    /// </summary>
    /// <param name="type">The type to convert.</param>
    /// <returns>The matching converter, or <see langword="null" /> when none applies.</returns>
    /// <remarks>
    /// Resolution is memoized once the options are frozen, so the linear converter scan runs once per type rather
    /// than once per value read or written. Before the freeze the scan runs uncached, because the converter list can
    /// still change.
    /// </remarks>
    internal YamlConverter? GetConverter(Type type) =>
        _isReadOnly
            ? _converterCache.GetOrAdd(type, static (t, self) => self.ResolveConverter(t), this)
            : ResolveConverter(type);

    /// <summary>
    /// Gets the cached member binding describing how the specified type maps to a YAML mapping.
    /// </summary>
    /// <param name="type">The type to bind.</param>
    /// <returns>The binding.</returns>
    /// <remarks>
    /// The binding is memoized once the options are frozen, so wire names are computed and validated once per type.
    /// </remarks>
    [RequiresUnreferencedCode("Reflection-based YAML serialization may require types that trimming cannot statically determine.")]
    internal YamlTypeBinding GetBinding(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] Type type) =>
        _isReadOnly
            ? _bindingCache.GetOrAdd(type, static (t, self) => YamlTypeBinding.Create(t, self), this)
            : YamlTypeBinding.Create(type, this);

    /// <summary>
    /// Resolves the converter for a type without consulting the cache.
    /// </summary>
    /// <param name="type">The type to convert.</param>
    /// <returns>The matching converter, or <see langword="null" /> when none applies.</returns>
    private YamlConverter? ResolveConverter(Type type)
    {
        foreach (YamlConverter converter in Converters)
        {
            if (converter.CanConvert(type))
                return converter;
        }

        return null;
    }

    /// <summary>
    /// Throws when the options instance has been frozen.
    /// </summary>
    /// <exception cref="InvalidOperationException">The options instance is read-only.</exception>
    internal void VerifyMutable()
    {
        if (_isReadOnly)
            throw new InvalidOperationException(YamlResourceStrings.Op_Invalid_OptionsReadOnly);
    }
}
