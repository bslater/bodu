// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedSerializerOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;

namespace Bodu.Text.Delimited;

/// <summary>
/// Provides configuration for <see cref="DelimitedSerializer" />, controlling column naming, case sensitivity, field
/// inclusion, and the output delimiter, quote, and header behaviour.
/// </summary>
/// <remarks>
/// An options instance becomes read-only the first time it is used; mutating a property after that point throws
/// <see cref="InvalidOperationException" />. Share and reuse a configured instance rather than allocating one per call.
/// </remarks>
public sealed class DelimitedSerializerOptions
{
    /// <summary>The naming policy applied to column names, or <see langword="null" /> to use names verbatim.</summary>
    private NamingPolicy? _propertyNamingPolicy;

    /// <summary>Whether column-name matching on deserialization ignores case.</summary>
    private bool _propertyNameCaseInsensitive;

    /// <summary>Whether public fields participate in addition to properties.</summary>
    private bool _includeFields;

    /// <summary>The output field delimiter, or <c>'\0'</c> for the default comma.</summary>
    private char _delimiter;

    /// <summary>The output quote character, or <c>'\0'</c> for the default double quote.</summary>
    private char _quote;

    /// <summary>Whether the header row is suppressed on the write path.</summary>
    private bool _noHeader;

    /// <summary>Whether the options have become read-only.</summary>
    private bool _isReadOnly;

    /// <summary>
    /// Initializes a new instance of the <see cref="DelimitedSerializerOptions" /> class with general-purpose defaults.
    /// </summary>
    public DelimitedSerializerOptions()
        : this(DelimitedSerializerDefaults.General)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DelimitedSerializerOptions" /> class from the specified defaults.
    /// </summary>
    /// <param name="defaults">The defaults to initialize from.</param>
    public DelimitedSerializerOptions(DelimitedSerializerDefaults defaults)
    {
        if (defaults == DelimitedSerializerDefaults.Web)
        {
            _propertyNamingPolicy = NamingPolicy.SnakeCaseLower;
            _propertyNameCaseInsensitive = true;
        }
    }

    /// <summary>
    /// Gets the shared read-only default options.
    /// </summary>
    /// <value>The default options.</value>
    public static DelimitedSerializerOptions Default { get; } = CreateDefault();

    /// <summary>
    /// Gets a value indicating whether the options have become read-only.
    /// </summary>
    /// <value><see langword="true" /> once the options have been used; otherwise <see langword="false" />.</value>
    public bool IsReadOnly => _isReadOnly;

    /// <summary>
    /// Gets or sets the naming policy applied to column names, or <see langword="null" /> to use names verbatim.
    /// </summary>
    /// <value>The column naming policy.</value>
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
    /// Gets or sets a value indicating whether column-name matching on deserialization ignores case.
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
    /// Gets or sets a value indicating whether public fields participate in addition to properties.
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
    /// Gets or sets the output field delimiter, or <c>'\0'</c> to use the default comma.
    /// </summary>
    /// <value>The delimiter character.</value>
    /// <exception cref="InvalidOperationException">Thrown when set after the options have become read-only.</exception>
    public char Delimiter
    {
        get => _delimiter;
        set
        {
            VerifyMutable();
            _delimiter = value;
        }
    }

    /// <summary>
    /// Gets or sets the output quote character, or <c>'\0'</c> to use the default double quote.
    /// </summary>
    /// <value>The quote character.</value>
    /// <exception cref="InvalidOperationException">Thrown when set after the options have become read-only.</exception>
    public char Quote
    {
        get => _quote;
        set
        {
            VerifyMutable();
            _quote = value;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the header row is suppressed on the write path and absent on the read
    /// path.
    /// </summary>
    /// <value><see langword="true" /> for headerless documents; otherwise <see langword="false" />.</value>
    /// <exception cref="InvalidOperationException">Thrown when set after the options have become read-only.</exception>
    public bool NoHeader
    {
        get => _noHeader;
        set
        {
            VerifyMutable();
            _noHeader = value;
        }
    }

    /// <summary>
    /// Marks the options read-only, preventing further mutation.
    /// </summary>
    internal void MakeReadOnly() => _isReadOnly = true;

    /// <summary>
    /// Converts a member name to its serialized column name, applying the configured naming policy.
    /// </summary>
    /// <param name="memberName">The .NET member name.</param>
    /// <returns>The serialized column name.</returns>
    internal string ConvertName(string memberName) =>
        _propertyNamingPolicy?.ConvertName(memberName) ?? memberName;

    /// <summary>
    /// Builds the reader options that mirror these serializer settings.
    /// </summary>
    /// <returns>The reader options.</returns>
    internal Reader.DelimitedReaderOptions ToReaderOptions() =>
        new() { Delimiter = _delimiter, Quote = _quote, NoHeader = _noHeader };

    /// <summary>
    /// Builds the writer options that mirror these serializer settings.
    /// </summary>
    /// <returns>The writer options.</returns>
    internal Writer.DelimitedWriterOptions ToWriterOptions() =>
        new() { Delimiter = _delimiter, Quote = _quote, NoHeader = _noHeader };

    /// <summary>
    /// Creates the shared read-only default options.
    /// </summary>
    /// <returns>The default options.</returns>
    private static DelimitedSerializerOptions CreateDefault()
    {
        var options = new DelimitedSerializerOptions();
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
            throw new InvalidOperationException(DelimitedResourceStrings.Op_Invalid_DelimitedOptionsReadOnly);
    }
}
