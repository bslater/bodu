// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationDocument.Parse.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO;

namespace Bodu.Text.Configuration;

public sealed partial class BoduConfigurationDocument
{
    /// <summary>
    /// Parses the supplied configuration text into a <see cref="BoduConfigurationDocument" /> using the
    /// default Bodu parse options.
    /// </summary>
    /// <param name="text">The configuration text to parse.</param>
    /// <returns>A populated <see cref="BoduConfigurationDocument" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text" /> is <see langword="null" />.</exception>
    /// <exception cref="BoduConfigurationParseException">The text could not be parsed.</exception>
    public static BoduConfigurationDocument Parse(string text) =>
        Parse(text, options: null);

    /// <summary>
    /// Parses the supplied configuration text into a <see cref="BoduConfigurationDocument" />.
    /// </summary>
    /// <param name="text">The configuration text to parse.</param>
    /// <param name="options">The parse options to apply, or <see langword="null" /> for the defaults.</param>
    /// <returns>A populated <see cref="BoduConfigurationDocument" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text" /> is <see langword="null" />.</exception>
    /// <exception cref="BoduConfigurationParseException">The text could not be parsed.</exception>
    public static BoduConfigurationDocument Parse(string text, BoduConfigurationParseOptions? options)
    {
        ThrowHelper.ThrowIfNull(text);

        using StringReader reader = new(text);
        return new BoduConfigurationReader(options ?? BoduConfigurationParseOptions.Bodu).Read(reader, path: null);
    }

    /// <summary>
    /// Attempts to parse the supplied configuration text into a <see cref="BoduConfigurationDocument" />.
    /// </summary>
    /// <param name="text">The configuration text to parse.</param>
    /// <param name="result">When this method returns, contains the parsed document if successful; otherwise,
    /// <see langword="null" />.</param>
    /// <returns><see langword="true" /> when parsing succeeded; otherwise, <see langword="false" />.</returns>
    public static bool TryParse(string? text, out BoduConfigurationDocument? result) =>
        TryParse(text, options: null, out result);

    /// <summary>
    /// Attempts to parse the supplied configuration text using the specified options.
    /// </summary>
    /// <param name="text">The configuration text to parse.</param>
    /// <param name="options">The parse options to apply, or <see langword="null" /> for the defaults.</param>
    /// <param name="result">When this method returns, contains the parsed document if successful; otherwise,
    /// <see langword="null" />.</param>
    /// <returns><see langword="true" /> when parsing succeeded; otherwise, <see langword="false" />.</returns>
    public static bool TryParse(string? text, BoduConfigurationParseOptions? options, out BoduConfigurationDocument? result)
    {
        if (text is null)
        {
            result = null;
            return false;
        }

        try
        {
            result = Parse(text, options);
            return true;
        }
        catch (BoduConfigurationParseException)
        {
            result = null;
            return false;
        }
    }
}
