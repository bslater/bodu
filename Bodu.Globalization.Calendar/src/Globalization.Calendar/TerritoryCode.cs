// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TerritoryCode.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Represents an ISO 3166-1 alpha-2 country code with an optional ISO 3166-2 subdivision code (e.g. <c>AU</c> or <c>AU-NSW</c>).
/// </summary>
/// <remarks>
/// <para>
/// <see cref="TerritoryCode" /> is the canonical representation of regional scoping for notable date rules, observance adjustments, and
/// queries. It supersedes ad-hoc string comparisons and removes ambiguity when matching parent territories against subdivisions.
/// </para>
/// <para>
/// A territory is considered to <em>contain</em> another when it is the parent country and the other is one of its subdivisions, or
/// when the two are identical. Comparisons are performed case-insensitively against the canonical upper-case form.
/// </para>
/// </remarks>
/// <example>
/// <para>Parse and compare territory codes:</para>
/// <code>
/// // Parse a country-level territory:
/// if (TerritoryCode.TryParse("AU", out TerritoryCode australia))
///     Console.WriteLine(australia.Country);        // "AU"
///
/// // Parse a subdivision:
/// TerritoryCode nsw = TerritoryCode.Parse("AU-NSW");
/// Console.WriteLine(nsw.HasSubdivision);           // true
/// Console.WriteLine(nsw.Subdivision);              // "NSW"
/// Console.WriteLine(nsw);                          // "AU-NSW"
///
/// // Containment: AU contains AU-NSW but not NZ:
/// Console.WriteLine(australia.Contains(nsw));      // true
/// Console.WriteLine(nsw.Contains(australia));      // false
///
/// // Parse a comma-separated list (invalid entries are silently skipped):
/// IReadOnlyList&lt;TerritoryCode&gt; codes = TerritoryCode.ParseList("AU, AU-NSW, AU-VIC");
/// </code>
/// </example>
public readonly record struct TerritoryCode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TerritoryCode" /> struct from a parsed country and optional subdivision.
    /// </summary>
    /// <param name="country">The two-letter ISO 3166-1 alpha-2 country code in upper case.</param>
    /// <param name="subdivision">Optional ISO 3166-2 subdivision code in upper case, or <see langword="null" /> for no subdivision.</param>
    private TerritoryCode(string country, string? subdivision)
    {
        Country = country;
        Subdivision = subdivision;
    }

    /// <summary>
    /// Gets the two-letter ISO 3166-1 alpha-2 country code (e.g. <c>AU</c>, <c>US</c>).
    /// </summary>
    /// <returns>The upper-case two-letter country code. Never <see langword="null" /> for an initialised value.</returns>
    public string Country { get; }

    /// <summary>
    /// Gets the optional ISO 3166-2 subdivision code (e.g. <c>NSW</c>, <c>CA</c>), or <see langword="null" /> when the territory refers
    /// to the country as a whole.
    /// </summary>
    /// <returns>The upper-case subdivision code, or <see langword="null" /> for a country-level territory.</returns>
    public string? Subdivision { get; }

    /// <summary>
    /// Gets a value indicating whether this territory refers to a subdivision rather than a whole country.
    /// </summary>
    /// <returns><see langword="true" /> when <see cref="Subdivision" /> is non-<see langword="null" />; otherwise <see langword="false" />.</returns>
    public bool HasSubdivision => Subdivision is not null;

    /// <summary>
    /// Attempts to parse a string into a <see cref="TerritoryCode" />.
    /// </summary>
    /// <param name="value">The candidate value, in the form <c>CC</c> or <c>CC-SUB</c>. Whitespace is trimmed.</param>
    /// <param name="result">When this method returns <see langword="true" />, contains the parsed territory.</param>
    /// <returns><see langword="true" /> if <paramref name="value" /> represents a valid territory; otherwise, <see langword="false" />.</returns>
    public static bool TryParse(string? value, out TerritoryCode result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        ReadOnlySpan<char> span = value.AsSpan().Trim();
        var dashIndex = span.IndexOf('-');

        ReadOnlySpan<char> countrySpan = dashIndex >= 0 ? span.Slice(0, dashIndex) : span;
        ReadOnlySpan<char> subSpan = dashIndex >= 0 ? span.Slice(dashIndex + 1) : [];

        if (countrySpan.Length != 2 || !IsAllAsciiLetters(countrySpan))
            return false;

        if (dashIndex >= 0 && (subSpan.Length < 1 || subSpan.Length > 3 || !IsAllAsciiAlphanumeric(subSpan)))
            return false;

        var country = countrySpan.ToString().ToUpperInvariant();
        var subdivision = dashIndex >= 0 ? subSpan.ToString().ToUpperInvariant() : null;

        result = new TerritoryCode(country, subdivision);
        return true;
    }

    /// <summary>
    /// Parses a string into a <see cref="TerritoryCode" />.
    /// </summary>
    /// <param name="value">The candidate value, in the form <c>CC</c> or <c>CC-SUB</c>.</param>
    /// <returns>The parsed territory.</returns>
    /// <exception cref="FormatException">Thrown when <paramref name="value" /> is not a valid ISO 3166 territory code.</exception>
    public static TerritoryCode Parse(string value)
    {
        if (TryParse(value, out TerritoryCode result))
            return result;

        throw new FormatException(string.Format(CultureInfo.InvariantCulture, CalendarResourceStrings.FormatException_TerritoryCodeInvalid, value));
    }

    /// <summary>
    /// Parses a comma-separated list of territory codes into a sequence of <see cref="TerritoryCode" /> values.
    /// </summary>
    /// <param name="value">The comma-separated list, or <see langword="null" />/empty for an empty result.</param>
    /// <returns>The parsed territories. Invalid entries are skipped silently.</returns>
    public static IReadOnlyList<TerritoryCode> ParseList(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return [];

        var parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var list = new List<TerritoryCode>(parts.Length);
        foreach (var part in parts)
        {
            if (TryParse(part, out TerritoryCode parsed))
                list.Add(parsed);
        }

        return list;
    }

    /// <summary>
    /// Determines whether this territory contains <paramref name="other" />.
    /// </summary>
    /// <param name="other">The territory to test for containment.</param>
    /// <returns>
    /// <see langword="true" /> if the two territories are equal, or if this territory is a country and <paramref name="other" /> is one
    /// of its subdivisions; otherwise <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// <para><c>AU</c> contains both <c>AU</c> and <c>AU-NSW</c>, but never <c>AUT</c> or <c>NZ</c>.</para>
    /// <para><c>AU-NSW</c> contains only <c>AU-NSW</c>.</para>
    /// </remarks>
    public bool Contains(TerritoryCode other)
    {
        if (!string.Equals(Country, other.Country, StringComparison.OrdinalIgnoreCase))
            return false;

        // Country-only contains both itself and any subdivision.
        if (Subdivision is null)
            return true;

        // Subdivision matches exactly only when the other has the same subdivision.
        return string.Equals(Subdivision, other.Subdivision, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns the canonical string form of the territory (<c>CC</c> or <c>CC-SUB</c>).
    /// </summary>
    /// <returns>The canonical string representation.</returns>
    public override string ToString() =>
        Subdivision is null ? Country : string.Concat(Country, "-", Subdivision);

    /// <summary>
    /// Returns <see langword="true" /> if every character in <paramref name="value" /> is an
    /// ASCII letter (A–Z or a–z).
    /// </summary>
    /// <param name="value">The character span to test.</param>
    /// <returns><see langword="true" /> if all characters are ASCII letters; otherwise <see langword="false" />.</returns>
    private static bool IsAllAsciiLetters(ReadOnlySpan<char> value)
    {
        foreach (var c in value)
        {
            if (c < 'A' || c > 'Z')
            {
                if (c < 'a' || c > 'z')
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Returns <see langword="true" /> if every character in <paramref name="value" /> is an
    /// ASCII letter or digit.
    /// </summary>
    /// <param name="value">The character span to test.</param>
    /// <returns><see langword="true" /> if all characters are ASCII alphanumerics; otherwise <see langword="false" />.</returns>
    private static bool IsAllAsciiAlphanumeric(ReadOnlySpan<char> value)
    {
        foreach (var c in value)
        {
            var isUpper = c >= 'A' && c <= 'Z';
            var isLower = c >= 'a' && c <= 'z';
            var isDigit = c >= '0' && c <= '9';
            if (!isUpper && !isLower && !isDigit)
                return false;
        }

        return true;
    }
}
