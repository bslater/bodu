// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationKey.Parse.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

public readonly partial struct BoduConfigurationKey
{
    /// <summary>
    /// Parses a raw configuration key into a <see cref="BoduConfigurationKey" /> using the default key options.
    /// </summary>
    /// <param name="rawKey">The raw key to parse.</param>
    /// <returns>A <see cref="BoduConfigurationKey" /> built from <paramref name="rawKey" />.</returns>
    /// <exception cref="ArgumentException"><paramref name="rawKey" /> is <see langword="null" />, empty, or
    /// contains only whitespace, or contains an empty segment when empty segments are not permitted.</exception>
    public static BoduConfigurationKey Parse(string rawKey) =>
        Parse(rawKey, options: null);

    /// <summary>
    /// Parses a raw configuration key into a <see cref="BoduConfigurationKey" /> using the supplied options.
    /// </summary>
    /// <param name="rawKey">The raw key to parse.</param>
    /// <param name="options">The key options to apply, or <see langword="null" /> for the defaults.</param>
    /// <returns>A <see cref="BoduConfigurationKey" /> built from <paramref name="rawKey" />.</returns>
    /// <exception cref="ArgumentException"><paramref name="rawKey" /> is <see langword="null" />, empty, or
    /// contains only whitespace, or contains an empty segment when empty segments are not permitted.</exception>
    public static BoduConfigurationKey Parse(string rawKey, BoduConfigurationKeyOptions? options) =>
        new(rawKey, options);

    /// <summary>
    /// Attempts to parse a raw configuration key.
    /// </summary>
    /// <param name="rawKey">The raw key to parse.</param>
    /// <param name="result">When this method returns, contains the parsed key if successful; otherwise, the
    /// default key.</param>
    /// <returns><see langword="true" /> when parsing succeeded; otherwise, <see langword="false" />.</returns>
    public static bool TryParse(string? rawKey, out BoduConfigurationKey result) =>
        TryParse(rawKey, options: null, out result);

    /// <summary>
    /// Attempts to parse a raw configuration key using the supplied options.
    /// </summary>
    /// <param name="rawKey">The raw key to parse.</param>
    /// <param name="options">The key options to apply, or <see langword="null" /> for the defaults.</param>
    /// <param name="result">When this method returns, contains the parsed key if successful; otherwise, the
    /// default key.</param>
    /// <returns><see langword="true" /> when parsing succeeded; otherwise, <see langword="false" />.</returns>
    public static bool TryParse(string? rawKey, BoduConfigurationKeyOptions? options, out BoduConfigurationKey result)
    {
        if (string.IsNullOrWhiteSpace(rawKey))
        {
            result = default;
            return false;
        }

        try
        {
            result = new BoduConfigurationKey(rawKey, options);
            return true;
        }
        catch (ArgumentException)
        {
            result = default;
            return false;
        }
    }
}
