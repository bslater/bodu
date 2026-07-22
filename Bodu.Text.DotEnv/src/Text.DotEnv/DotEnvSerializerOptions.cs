// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvSerializerOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;

namespace Bodu.Text.DotEnv;

/// <summary>
/// Provides configuration for <see cref="DotEnvSerializer" />, controlling property naming, case sensitivity, field
/// inclusion, null handling, and the <c>export</c> prefix on the write path.
/// </summary>
/// <remarks>
/// An options instance becomes read-only the first time it is used for serialization or deserialization; mutating a
/// property after that point throws <see cref="InvalidOperationException" />. Share and reuse a configured instance
/// rather than allocating one per call.
/// </remarks>
public sealed class DotEnvSerializerOptions
{
    /// <summary>The naming policy applied to property names, or <see langword="null" /> to use names verbatim.</summary>
    private NamingPolicy? _propertyNamingPolicy;

    /// <summary>Whether property-name matching on deserialization ignores case.</summary>
    private bool _propertyNameCaseInsensitive;

    /// <summary>Whether public fields participate in serialization in addition to properties.</summary>
    private bool _includeFields;

    /// <summary>The condition under which a property is omitted from the output.</summary>
    private IgnoreCondition _defaultIgnoreCondition = IgnoreCondition.WhenWritingNull;

    /// <summary>Whether written keys are prefixed with the <c>export</c> keyword.</summary>
    private bool _writeExportPrefix;

    /// <summary>Whether the options have become read-only.</summary>
    private bool _isReadOnly;

    /// <summary>
    /// Initializes a new instance of the <see cref="DotEnvSerializerOptions" /> class with general-purpose defaults.
    /// </summary>
    public DotEnvSerializerOptions()
        : this(DotEnvSerializerDefaults.General)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DotEnvSerializerOptions" /> class from the specified defaults.
    /// </summary>
    /// <param name="defaults">The defaults to initialize from.</param>
    public DotEnvSerializerOptions(DotEnvSerializerDefaults defaults)
    {
        if (defaults == DotEnvSerializerDefaults.Web)
        {
            _propertyNamingPolicy = NamingPolicy.SnakeCaseUpper;
            _propertyNameCaseInsensitive = true;
        }
    }

    /// <summary>
    /// Gets the shared read-only default options.
    /// </summary>
    /// <value>The default options.</value>
    public static DotEnvSerializerOptions Default { get; } = CreateDefault();

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
    /// Gets or sets a value indicating whether written keys are prefixed with the <c>export</c> keyword.
    /// </summary>
    /// <value><see langword="true" /> to write an <c>export</c> prefix; otherwise <see langword="false" />.</value>
    /// <exception cref="InvalidOperationException">Thrown when set after the options have become read-only.</exception>
    public bool WriteExportPrefix
    {
        get => _writeExportPrefix;
        set
        {
            VerifyMutable();
            _writeExportPrefix = value;
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
    /// Creates the shared read-only default options.
    /// </summary>
    /// <returns>The default options.</returns>
    private static DotEnvSerializerOptions CreateDefault()
    {
        var options = new DotEnvSerializerOptions();
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
            throw new InvalidOperationException(DotEnvResourceStrings.Op_Invalid_DotEnvOptionsReadOnly);
    }
}
