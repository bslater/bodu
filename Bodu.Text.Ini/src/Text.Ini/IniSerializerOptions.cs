// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniSerializerOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;

namespace Bodu.Text.Ini;

/// <summary>
/// Provides configuration for <see cref="IniSerializer" />, controlling property naming, case sensitivity, field
/// inclusion, null handling, the global-section mapping, and the duplicate-section/key policies.
/// </summary>
/// <remarks>
/// An options instance becomes read-only the first time it is used for serialization or deserialization; mutating a
/// property after that point throws <see cref="InvalidOperationException" />. Share and reuse a configured instance
/// rather than allocating one per call.
/// </remarks>
public sealed class IniSerializerOptions
{
    /// <summary>The naming policy applied to property names, or <see langword="null" /> to use names verbatim.</summary>
    private NamingPolicy? _propertyNamingPolicy;

    /// <summary>Whether property-name matching on deserialization ignores case.</summary>
    private bool _propertyNameCaseInsensitive;

    /// <summary>Whether public fields participate in serialization in addition to properties.</summary>
    private bool _includeFields;

    /// <summary>The condition under which a property is omitted from the output.</summary>
    private IgnoreCondition _defaultIgnoreCondition = IgnoreCondition.WhenWritingNull;

    /// <summary>The reserved root name the global section binds to, or <see langword="null" /> to hoist global keys.</summary>
    private string? _globalSectionName;

    /// <summary>The policy applied when a section name appears more than once.</summary>
    private IniDuplicateSectionBehavior _duplicateSectionBehavior;

    /// <summary>The policy applied when a key appears more than once within the same section.</summary>
    private IniDuplicateKeyBehavior _duplicateKeyBehavior;

    /// <summary>Whether the options have become read-only.</summary>
    private bool _isReadOnly;

    /// <summary>
    /// Initializes a new instance of the <see cref="IniSerializerOptions" /> class with general-purpose defaults.
    /// </summary>
    public IniSerializerOptions()
        : this(IniSerializerDefaults.General)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IniSerializerOptions" /> class from the specified defaults.
    /// </summary>
    /// <param name="defaults">The defaults to initialize from.</param>
    public IniSerializerOptions(IniSerializerDefaults defaults)
    {
        if (defaults == IniSerializerDefaults.Strict)
        {
            _duplicateSectionBehavior = IniDuplicateSectionBehavior.Disallowed;
            _duplicateKeyBehavior = IniDuplicateKeyBehavior.Disallowed;
        }
    }

    /// <summary>
    /// Gets the shared read-only default options.
    /// </summary>
    /// <value>The default options.</value>
    public static IniSerializerOptions Default { get; } = CreateDefault();

    /// <summary>
    /// Gets a value indicating whether the options have become read-only.
    /// </summary>
    /// <value><see langword="true" /> once the options have been used; otherwise <see langword="false" />.</value>
    public bool IsReadOnly => _isReadOnly;

    /// <summary>
    /// Gets or sets the naming policy applied to property names, or <see langword="null" /> to use names verbatim.
    /// </summary>
    /// <value>The property naming policy.</value>
    /// <exception cref="InvalidOperationException">Thrown when set after the options have become read-only.</exception>
    public NamingPolicy? PropertyNamingPolicy
    {
        get => _propertyNamingPolicy;
        set
        {
            VerifyMutable();
            _propertyNamingPolicy = value;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether property-name matching on deserialization ignores case.
    /// </summary>
    /// <value><see langword="true" /> to match case-insensitively; otherwise <see langword="false" />.</value>
    /// <exception cref="InvalidOperationException">Thrown when set after the options have become read-only.</exception>
    public bool PropertyNameCaseInsensitive
    {
        get => _propertyNameCaseInsensitive;
        set
        {
            VerifyMutable();
            _propertyNameCaseInsensitive = value;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether public fields participate in serialization in addition to properties.
    /// </summary>
    /// <value><see langword="true" /> to include fields; otherwise <see langword="false" />.</value>
    /// <exception cref="InvalidOperationException">Thrown when set after the options have become read-only.</exception>
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
    /// Gets or sets the condition under which a property is omitted from the output.
    /// </summary>
    /// <value>The default ignore condition.</value>
    /// <exception cref="InvalidOperationException">Thrown when set after the options have become read-only.</exception>
    public IgnoreCondition DefaultIgnoreCondition
    {
        get => _defaultIgnoreCondition;
        set
        {
            VerifyMutable();
            _defaultIgnoreCondition = value;
        }
    }

    /// <summary>
    /// Gets or sets the reserved root name the global (pre-section) entries bind to, or <see langword="null" /> to
    /// hoist global keys onto the root object.
    /// </summary>
    /// <value>The reserved root name; the default is <see langword="null" /> (hoist).</value>
    /// <remarks>
    /// Hoisting maps global keys onto the root object's scalar members, which is ergonomic but cannot represent a
    /// document whose root type maps every member to a section. Naming a reserved key routes the global entries to and
    /// from the member (or dictionary entry) with that name instead, preserving round-trip fidelity.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when set after the options have become read-only.</exception>
    public string? GlobalSectionName
    {
        get => _globalSectionName;
        set
        {
            VerifyMutable();
            _globalSectionName = value;
        }
    }

    /// <summary>
    /// Gets or sets the policy applied when a section name appears more than once.
    /// </summary>
    /// <value>The duplicate-section policy; the default is <see cref="IniDuplicateSectionBehavior.Merge" />.</value>
    /// <exception cref="InvalidOperationException">Thrown when set after the options have become read-only.</exception>
    public IniDuplicateSectionBehavior DuplicateSectionBehavior
    {
        get => _duplicateSectionBehavior;
        set
        {
            VerifyMutable();
            _duplicateSectionBehavior = value;
        }
    }

    /// <summary>
    /// Gets or sets the policy applied when a key appears more than once within the same section.
    /// </summary>
    /// <value>The duplicate-key policy; the default is <see cref="IniDuplicateKeyBehavior.LastWins" />.</value>
    /// <exception cref="InvalidOperationException">Thrown when set after the options have become read-only.</exception>
    public IniDuplicateKeyBehavior DuplicateKeyBehavior
    {
        get => _duplicateKeyBehavior;
        set
        {
            VerifyMutable();
            _duplicateKeyBehavior = value;
        }
    }

    /// <summary>
    /// Marks the options read-only, preventing further mutation.
    /// </summary>
    internal void MakeReadOnly() => _isReadOnly = true;

    /// <summary>
    /// Converts a member name to its serialized key, applying the configured naming policy.
    /// </summary>
    /// <param name="memberName">The .NET member name.</param>
    /// <returns>The serialized key.</returns>
    internal string ConvertName(string memberName) =>
        _propertyNamingPolicy?.ConvertName(memberName) ?? memberName;

    /// <summary>
    /// Creates the document-model options carrying this instance's duplicate policies.
    /// </summary>
    /// <returns>The document options.</returns>
    internal IniDocumentOptions ToDocumentOptions() =>
        new()
        {
            DuplicateSectionBehavior = _duplicateSectionBehavior,
            DuplicateKeyBehavior = _duplicateKeyBehavior,
        };

    /// <summary>
    /// Creates the shared read-only default options.
    /// </summary>
    /// <returns>The default options.</returns>
    private static IniSerializerOptions CreateDefault()
    {
        var options = new IniSerializerOptions();
        options.MakeReadOnly();

        return options;
    }

    /// <summary>
    /// Throws when the options have become read-only.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the options are read-only.</exception>
    private void VerifyMutable()
    {
        if (_isReadOnly)
            throw new InvalidOperationException(IniResourceStrings.Op_Invalid_IniOptionsReadOnly);
    }
}
