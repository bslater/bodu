// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationView.Getters.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Bodu.Text.Configuration;

public sealed partial class BoduConfigurationView
{
    /// <summary>
    /// Gets the raw string value for <paramref name="key" />, throwing if the key is missing.
    /// </summary>
    /// <param name="key">The configuration key in colon-delimited form.</param>
    /// <returns>The value as authored.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    /// <exception cref="KeyNotFoundException">The key is absent from the resolved view.</exception>
    public string GetString(string key)
    {
        ThrowHelper.ThrowIfNull(key);
        string? value = LookupValue(this._values, key);
        if (value is not null)
            return value;

        throw new KeyNotFoundException($"Configuration key '{key}' is not present in the resolved view.");
    }

    /// <summary>
    /// Gets the string value for <paramref name="key" />, returning <paramref name="fallback" /> when absent.
    /// </summary>
    /// <param name="key">The configuration key in colon-delimited form.</param>
    /// <param name="fallback">The value to return when the key is absent.</param>
    /// <returns>The resolved value or <paramref name="fallback" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    public string? GetString(string key, string? fallback)
    {
        ThrowHelper.ThrowIfNull(key);
        string? value = LookupValue(this._values, key);
        return value ?? fallback;
    }

    /// <summary>
    /// Attempts to get the string value for <paramref name="key" /> without throwing.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="value">When this method returns, contains the value if found; otherwise,
    /// <see langword="null" />.</param>
    /// <returns><see langword="true" /> when the key was present; otherwise, <see langword="false" />.</returns>
    public bool TryGetString(string key, out string? value)
    {
        ThrowHelper.ThrowIfNull(key);
        value = LookupValue(this._values, key);
        return value is not null;
    }

    /// <summary>
    /// Gets the 32-bit integer value for <paramref name="key" />, throwing on missing or malformed values.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <returns>The parsed integer value.</returns>
    /// <exception cref="KeyNotFoundException">The key is absent.</exception>
    /// <exception cref="FormatException">The value cannot be parsed as an integer.</exception>
    public int GetInt32(string key)
    {
        string raw = this.GetString(key);
        if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
            return value;

        throw new FormatException(string.Format(CultureInfo.InvariantCulture, ConfigurationResourceStrings.FormatException_ValueNotConvertible, key, raw, nameof(Int32)));
    }

    /// <summary>
    /// Gets the 32-bit integer value for <paramref name="key" />, returning <paramref name="fallback" /> on
    /// missing keys. Present-but-malformed values still throw <see cref="FormatException" />.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="fallback">The value to return when the key is absent.</param>
    /// <returns>The parsed value or <paramref name="fallback" />.</returns>
    public int GetInt32(string key, int fallback)
    {
        ThrowHelper.ThrowIfNull(key);
        string? raw = LookupValue(this._values, key);
        if (raw is null)
            return fallback;

        if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
            return value;

        throw new FormatException(string.Format(CultureInfo.InvariantCulture, ConfigurationResourceStrings.FormatException_ValueNotConvertible, key, raw, nameof(Int32)));
    }

    /// <summary>
    /// Attempts to parse the value for <paramref name="key" /> as a 32-bit integer.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="value">When this method returns, contains the parsed value; otherwise, zero.</param>
    /// <returns><see langword="true" /> when the value was present and parseable; otherwise, <see langword="false" />.</returns>
    public bool TryGetInt32(string key, out int value)
    {
        ThrowHelper.ThrowIfNull(key);
        string? raw = LookupValue(this._values, key);
        if (raw is not null && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            return true;

        value = 0;
        return false;
    }

    /// <summary>
    /// Gets the 64-bit integer value for <paramref name="key" />.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <returns>The parsed value.</returns>
    /// <exception cref="KeyNotFoundException">The key is absent.</exception>
    /// <exception cref="FormatException">The value cannot be parsed.</exception>
    public long GetInt64(string key)
    {
        string raw = this.GetString(key);
        if (long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out long value))
            return value;

        throw new FormatException(string.Format(CultureInfo.InvariantCulture, ConfigurationResourceStrings.FormatException_ValueNotConvertible, key, raw, nameof(Int64)));
    }

    /// <summary>
    /// Gets the boolean value for <paramref name="key" /> using EditorConfig conventions
    /// (<c>true</c>/<c>false</c>, case-insensitive).
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <returns>The parsed boolean value.</returns>
    /// <exception cref="KeyNotFoundException">The key is absent.</exception>
    /// <exception cref="FormatException">The value cannot be parsed.</exception>
    public bool GetBoolean(string key)
    {
        string raw = this.GetString(key);
        if (bool.TryParse(raw, out bool value))
            return value;

        throw new FormatException(string.Format(CultureInfo.InvariantCulture, ConfigurationResourceStrings.FormatException_ValueNotConvertible, key, raw, nameof(Boolean)));
    }

    /// <summary>
    /// Gets the boolean value for <paramref name="key" />, returning <paramref name="fallback" /> on missing
    /// keys.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="fallback">The value to return when the key is absent.</param>
    /// <returns>The parsed value or <paramref name="fallback" />.</returns>
    public bool GetBoolean(string key, bool fallback)
    {
        ThrowHelper.ThrowIfNull(key);
        string? raw = LookupValue(this._values, key);
        if (raw is null)
            return fallback;

        if (bool.TryParse(raw, out bool value))
            return value;

        throw new FormatException(string.Format(CultureInfo.InvariantCulture, ConfigurationResourceStrings.FormatException_ValueNotConvertible, key, raw, nameof(Boolean)));
    }

    /// <summary>
    /// Attempts to parse the value for <paramref name="key" /> as a boolean.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="value">When this method returns, contains the parsed value; otherwise, <see langword="false" />.</param>
    /// <returns><see langword="true" /> when the value was present and parseable.</returns>
    public bool TryGetBoolean(string key, out bool value)
    {
        ThrowHelper.ThrowIfNull(key);
        string? raw = LookupValue(this._values, key);
        if (raw is not null && bool.TryParse(raw, out value))
            return true;

        value = false;
        return false;
    }

    /// <summary>
    /// Gets the value for <paramref name="key" /> as an enum of type <typeparamref name="TEnum" />.
    /// </summary>
    /// <typeparam name="TEnum">The enum type.</typeparam>
    /// <param name="key">The configuration key.</param>
    /// <returns>The parsed enum value.</returns>
    /// <exception cref="KeyNotFoundException">The key is absent.</exception>
    /// <exception cref="FormatException">The value cannot be parsed as the enum.</exception>
    public TEnum GetEnum<TEnum>(string key)
        where TEnum : struct, Enum
    {
        string raw = this.GetString(key);
        if (Enum.TryParse(raw, ignoreCase: true, out TEnum value) && Enum.IsDefined(typeof(TEnum), value))
            return value;

        throw new FormatException(string.Format(CultureInfo.InvariantCulture, ConfigurationResourceStrings.FormatException_ValueNotConvertible, key, raw, typeof(TEnum).Name));
    }
}
