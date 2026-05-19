// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationView.Getters.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Bodu.Text.Configuration;

public sealed partial class BoduConfigurationView
{
    /// <summary>
    /// Gets the value for <paramref name="key" /> parsed as <typeparamref name="T" /> using
    /// <see cref="CultureInfo.InvariantCulture" />. Mirrors <c>Bodu.Text.Formats.IniSection.GetValue&lt;T&gt;(key)</c>.
    /// </summary>
    /// <typeparam name="T">The target type. Must implement <see cref="ISpanParsable{TSelf}" />.</typeparam>
    /// <param name="key">The configuration key, in either dotted or colon-delimited form.</param>
    /// <returns>The parsed value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    /// <exception cref="KeyNotFoundException">The key is absent from the resolved view.</exception>
    /// <exception cref="FormatException">The value cannot be parsed as <typeparamref name="T" />.</exception>
    public T GetValue<T>(string key)
        where T : ISpanParsable<T>
    {
        ThrowHelper.ThrowIfNull(key);
        var raw = LookupValue(Values, key);
        if (raw is null)
            ConfigurationHelpers.ThrowConfigKeyNotPresent(key);

        return T.Parse(raw.AsSpan(), CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Attempts to get the value for <paramref name="key" /> parsed as <typeparamref name="T" />, returning
    /// <see langword="false" /> on missing or malformed values. Never throws on a parse failure.
    /// </summary>
    /// <typeparam name="T">The target type. Must implement <see cref="ISpanParsable{TSelf}" />.</typeparam>
    /// <param name="key">The configuration key, in either dotted or colon-delimited form.</param>
    /// <param name="value">When this method returns <see langword="true" />, contains the parsed value.</param>
    /// <returns>
    /// <see langword="true" /> when the value was present and parseable; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    public bool TryGetValue<T>(string key, [MaybeNullWhen(false)] out T value)
        where T : ISpanParsable<T>
    {
        ThrowHelper.ThrowIfNull(key);
        var raw = LookupValue(Values, key);
        if (raw is not null && T.TryParse(raw.AsSpan(), CultureInfo.InvariantCulture, out value))
            return true;

        value = default;
        return false;
    }

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
        var value = LookupValue(Values, key);
        if (value is not null)
            return value;

        ConfigurationHelpers.ThrowConfigKeyNotPresent(key);
        return null!;
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
        var value = LookupValue(Values, key);
        return value ?? fallback;
    }

    /// <summary>
    /// Attempts to get the string value for <paramref name="key" /> without throwing.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="value">
    /// When this method returns, contains the value if found; otherwise, <see langword="null" />.
    /// </param>
    /// <returns><see langword="true" /> when the key was present; otherwise, <see langword="false" />.</returns>
    public bool TryGetString(string key, out string? value)
    {
        ThrowHelper.ThrowIfNull(key);
        value = LookupValue(Values, key);
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
        var raw = GetString(key);
        if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            return value;

        ConfigurationHelpers.ThrowValueNotConvertible(key, raw, nameof(Int32));
        return 0;
    }

    /// <summary>
    /// Gets the 32-bit integer value for <paramref name="key" />, returning <paramref name="fallback" /> on missing
    /// keys. Present-but-malformed values still throw <see cref="FormatException" />.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="fallback">The value to return when the key is absent.</param>
    /// <returns>The parsed value or <paramref name="fallback" />.</returns>
    public int GetInt32(string key, int fallback)
    {
        ThrowHelper.ThrowIfNull(key);
        var raw = LookupValue(Values, key);
        if (raw is null)
            return fallback;

        if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            return value;

        ConfigurationHelpers.ThrowValueNotConvertible(key, raw, nameof(Int32));
        return 0;
    }

    /// <summary>
    /// Attempts to parse the value for <paramref name="key" /> as a 32-bit integer.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="value">When this method returns, contains the parsed value; otherwise, zero.</param>
    /// <returns>
    /// <see langword="true" /> when the value was present and parseable; otherwise, <see langword="false" />.
    /// </returns>
    public bool TryGetInt32(string key, out int value)
    {
        ThrowHelper.ThrowIfNull(key);
        var raw = LookupValue(Values, key);
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
        var raw = GetString(key);
        if (long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            return value;

        ConfigurationHelpers.ThrowValueNotConvertible(key, raw, nameof(Int64));
        return 0L;
    }

    /// <summary>
    /// Gets the boolean value for <paramref name="key" /> using EditorConfig conventions (<c>true</c>/<c>false</c>,
    /// case-insensitive).
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <returns>The parsed boolean value.</returns>
    /// <exception cref="KeyNotFoundException">The key is absent.</exception>
    /// <exception cref="FormatException">The value cannot be parsed.</exception>
    public bool GetBoolean(string key)
    {
        var raw = GetString(key);
        if (bool.TryParse(raw, out var value))
            return value;

        ConfigurationHelpers.ThrowValueNotConvertible(key, raw, nameof(Boolean));
        return false;
    }

    /// <summary>
    /// Gets the boolean value for <paramref name="key" />, returning <paramref name="fallback" /> on missing keys.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="fallback">The value to return when the key is absent.</param>
    /// <returns>The parsed value or <paramref name="fallback" />.</returns>
    public bool GetBoolean(string key, bool fallback)
    {
        ThrowHelper.ThrowIfNull(key);
        var raw = LookupValue(Values, key);
        if (raw is null)
            return fallback;

        if (bool.TryParse(raw, out var value))
            return value;

        ConfigurationHelpers.ThrowValueNotConvertible(key, raw, nameof(Boolean));
        return false;
    }

    /// <summary>
    /// Attempts to parse the value for <paramref name="key" /> as a boolean.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="value">
    /// When this method returns, contains the parsed value; otherwise, <see langword="false" />.
    /// </param>
    /// <returns><see langword="true" /> when the value was present and parseable.</returns>
    public bool TryGetBoolean(string key, out bool value)
    {
        ThrowHelper.ThrowIfNull(key);
        var raw = LookupValue(Values, key);
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
        var raw = GetString(key);
        if (Enum.TryParse(raw, ignoreCase: true, out TEnum value) && Enum.IsDefined(typeof(TEnum), value))
            return value;

        ConfigurationHelpers.ThrowValueNotConvertible(key, raw, typeof(TEnum).Name);
        return default;
    }
}
