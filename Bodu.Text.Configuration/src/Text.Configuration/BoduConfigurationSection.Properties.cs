// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationSection.Properties.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Bodu.Text.Configuration;

public sealed partial class BoduConfigurationSection
{
    /// <summary>
    /// Sets a property with the specified raw key to the supplied string value. If a property with the same
    /// configuration key already exists, its value is updated; otherwise, a new property is appended.
    /// </summary>
    /// <param name="rawKey">The raw key as it would appear in the source.</param>
    /// <param name="value">The new value. Use <see cref="string.Empty" /> for an empty value.</param>
    /// <param name="keyOptions">The key options used to compute the configuration key, or
    /// <see langword="null" /> for the defaults.</param>
    /// <exception cref="ArgumentException"><paramref name="rawKey" /> is <see langword="null" />, empty, or
    /// contains only whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="value" /> is <see langword="null" />.</exception>
    public void Set(string rawKey, string value, BoduConfigurationKeyOptions? keyOptions = null)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(rawKey);
        ThrowHelper.ThrowIfNull(value);

        BoduConfigurationKey key = new(rawKey, keyOptions);
        for (int i = 0; i < this._properties.Count; i++)
        {
            if (this._properties[i].Key.Equals(key))
            {
                this._properties[i].Value = value;
                return;
            }
        }

        this._properties.Add(new BoduConfigurationProperty(key, value));
    }

    /// <summary>
    /// Sets a property to a boolean value using the EditorConfig convention (<c>true</c>/<c>false</c>).
    /// </summary>
    /// <param name="rawKey">The raw key as it would appear in the source.</param>
    /// <param name="value">The boolean value.</param>
    /// <param name="keyOptions">The key options used to compute the configuration key, or
    /// <see langword="null" /> for the defaults.</param>
    public void Set(string rawKey, bool value, BoduConfigurationKeyOptions? keyOptions = null) =>
        this.Set(rawKey, value ? "true" : "false", keyOptions);

    /// <summary>
    /// Sets a property to a 32-bit integer value using invariant culture formatting.
    /// </summary>
    /// <param name="rawKey">The raw key as it would appear in the source.</param>
    /// <param name="value">The numeric value.</param>
    /// <param name="keyOptions">The key options used to compute the configuration key, or
    /// <see langword="null" /> for the defaults.</param>
    public void Set(string rawKey, int value, BoduConfigurationKeyOptions? keyOptions = null) =>
        this.Set(rawKey, value.ToString(CultureInfo.InvariantCulture), keyOptions);

    /// <summary>
    /// Sets a property to a 64-bit integer value using invariant culture formatting.
    /// </summary>
    /// <param name="rawKey">The raw key as it would appear in the source.</param>
    /// <param name="value">The numeric value.</param>
    /// <param name="keyOptions">The key options used to compute the configuration key, or
    /// <see langword="null" /> for the defaults.</param>
    public void Set(string rawKey, long value, BoduConfigurationKeyOptions? keyOptions = null) =>
        this.Set(rawKey, value.ToString(CultureInfo.InvariantCulture), keyOptions);

    /// <summary>
    /// Attempts to locate the first property whose configuration key matches <paramref name="rawKey" />.
    /// </summary>
    /// <param name="rawKey">The raw key to look up.</param>
    /// <param name="property">When this method returns, contains the matched property if found; otherwise,
    /// <see langword="null" />.</param>
    /// <param name="keyOptions">The key options used to compute the configuration key, or
    /// <see langword="null" /> for the defaults.</param>
    /// <returns><see langword="true" /> when a property was found; otherwise, <see langword="false" />.</returns>
    public bool TryGetProperty(
        string rawKey,
        [NotNullWhen(true)] out BoduConfigurationProperty? property,
        BoduConfigurationKeyOptions? keyOptions = null)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(rawKey);

        BoduConfigurationKey key = new(rawKey, keyOptions);
        for (int i = 0; i < this._properties.Count; i++)
        {
            if (this._properties[i].Key.Equals(key))
            {
                property = this._properties[i];
                return true;
            }
        }

        property = null;
        return false;
    }

    /// <summary>
    /// Removes the first property whose configuration key matches <paramref name="rawKey" />.
    /// </summary>
    /// <param name="rawKey">The raw key to remove.</param>
    /// <param name="keyOptions">The key options used to compute the configuration key, or
    /// <see langword="null" /> for the defaults.</param>
    /// <returns><see langword="true" /> when a property was removed; otherwise, <see langword="false" />.</returns>
    public bool RemoveProperty(string rawKey, BoduConfigurationKeyOptions? keyOptions = null)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(rawKey);

        BoduConfigurationKey key = new(rawKey, keyOptions);
        for (int i = 0; i < this._properties.Count; i++)
        {
            if (this._properties[i].Key.Equals(key))
            {
                this._properties.RemoveAt(i);
                return true;
            }
        }

        return false;
    }
}
