// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationProperty.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Text.Configuration;

/// <summary>
/// Represents a single <c>key = value</c> entry within a <see cref="BoduConfigurationSection" />.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Key" /> is initialized once at construction and is not mutated thereafter; rename via the
/// owning section. <see cref="Value" /> is mutable so callers can adjust the property without having to
/// rebuild the surrounding section. The <see cref="ConfigurationKey" /> is computed from <see cref="Key" />
/// using the document's key options.
/// </para>
/// <para>
/// Leading and inline comments are preserved by the reader and consumed by the writer for round-trip fidelity.
/// </para>
/// </remarks>
[DebuggerDisplay("{Key,nq} = {Value,nq}")]
public sealed partial class BoduConfigurationProperty
{
    /// <summary>
    /// Initializes a new property with the specified raw key, value, and key options.
    /// </summary>
    /// <param name="rawKey">The raw key as authored in the source.</param>
    /// <param name="value">The property value. Must not be <see langword="null" />.</param>
    /// <param name="keyOptions">The key options used to compute <see cref="ConfigurationKey" />, or
    /// <see langword="null" /> for the defaults.</param>
    /// <param name="location">The source location at which this property was authored.</param>
    /// <exception cref="ArgumentException"><paramref name="rawKey" /> is <see langword="null" />, empty, or
    /// contains only whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="value" /> is <see langword="null" />.</exception>
    public BoduConfigurationProperty(
        string rawKey,
        string value,
        BoduConfigurationKeyOptions? keyOptions = null,
        BoduConfigurationSourceLocation location = default)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(rawKey);
        ThrowHelper.ThrowIfNull(value);

        this.Key = new BoduConfigurationKey(rawKey, keyOptions);
        this.Value = value;
        this.Location = location;
    }

    /// <summary>
    /// Initializes a new property from a pre-parsed key.
    /// </summary>
    /// <param name="key">The parsed key.</param>
    /// <param name="value">The property value. Must not be <see langword="null" />.</param>
    /// <param name="location">The source location at which this property was authored.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="key" /> is the default key.</exception>
    public BoduConfigurationProperty(
        BoduConfigurationKey key,
        string value,
        BoduConfigurationSourceLocation location = default)
    {
        ThrowHelper.ThrowIfNull(value);
        if (key.Segments.Length == 0)
            throw new ArgumentException("Configuration key cannot be empty.", nameof(key));

        this.Key = key;
        this.Value = value;
        this.Location = location;
    }

    /// <summary>
    /// Gets the key for this property, including its raw and colon-delimited forms.
    /// </summary>
    /// <returns>The configuration key supplied at construction.</returns>
    public BoduConfigurationKey Key { get; }

    /// <summary>
    /// Gets the raw key as authored in the source.
    /// </summary>
    /// <returns>The raw key string.</returns>
    public string RawKey => this.Key.RawKey;

    /// <summary>
    /// Gets the colon-delimited logical key produced from <see cref="RawKey" />.
    /// </summary>
    /// <returns>The configuration key in colon-delimited form.</returns>
    public string ConfigurationKey => this.Key.ConfigurationKey;

    /// <summary>
    /// Gets or sets the property's value. Empty values are represented by <see cref="string.Empty" />.
    /// </summary>
    /// <returns>The current value of the property.</returns>
    /// <exception cref="ArgumentNullException">A <see langword="null" /> value is assigned.</exception>
    public string Value
    {
        get => this._value;
        set
        {
            ThrowHelper.ThrowIfNull(value);
            this._value = value;
        }
    }

    /// <summary>
    /// Gets the source location at which the property was authored.
    /// </summary>
    /// <returns>The source location, or <see cref="BoduConfigurationSourceLocation.None" /> when unknown.</returns>
    public BoduConfigurationSourceLocation Location { get; }

    private string _value = string.Empty;
}
