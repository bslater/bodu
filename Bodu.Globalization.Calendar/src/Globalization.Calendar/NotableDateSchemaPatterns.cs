// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateSchemaPatterns.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.RegularExpressions;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Holds the patterns the notable-date XML schema enforces on identifiers, references and territory codes, as the
/// schema writes them, so the JSON reader applies the same ones.
/// </summary>
internal static partial class NotableDateSchemaPatterns
{
    /// <summary>The pattern of the schema's <c>identifier</c> type.</summary>
    internal const string Identifier = "[a-z][a-z0-9]*(-[a-z0-9]+)*";

    /// <summary>The pattern of the schema's <c>resourceIdentifier</c> type.</summary>
    internal const string ResourceIdentifier = "[a-z][a-z0-9]*([.-][a-z0-9]+)*";

    /// <summary>The pattern of the schema's <c>territoryCode</c> type.</summary>
    internal const string TerritoryCode = "[A-Z]{2}(-[A-Z0-9]{2,3})?";

    /// <summary>
    /// Returns the text of a pattern, as the schema writes it.
    /// </summary>
    /// <param name="pattern">The pattern.</param>
    /// <returns>The pattern's text.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="pattern" /> names no pattern.</exception>
    internal static string GetPattern(NotableDateSchemaPattern pattern) =>
        pattern switch
        {
            NotableDateSchemaPattern.Identifier => Identifier,
            NotableDateSchemaPattern.ResourceIdentifier => ResourceIdentifier,
            NotableDateSchemaPattern.TerritoryCode => TerritoryCode,
            _ => throw new ArgumentOutOfRangeException(nameof(pattern)),
        };

    /// <summary>
    /// Determines whether a value matches a pattern as the schema matches it: the whole value, with no part of it left
    /// over.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="pattern">The pattern.</param>
    /// <returns>
    /// <see langword="true" /> when the whole value matches the pattern; otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="pattern" /> names no pattern.</exception>
    internal static bool IsMatch(string value, NotableDateSchemaPattern pattern) =>
        pattern switch
        {
            NotableDateSchemaPattern.Identifier => IdentifierRegex().IsMatch(value),
            NotableDateSchemaPattern.ResourceIdentifier => ResourceIdentifierRegex().IsMatch(value),
            NotableDateSchemaPattern.TerritoryCode => TerritoryCodeRegex().IsMatch(value),
            _ => throw new ArgumentOutOfRangeException(nameof(pattern)),
        };

    /// <summary>
    /// Returns the expression that matches a whole value against the <see cref="Identifier" /> pattern.
    /// </summary>
    /// <returns>The expression.</returns>
    [GeneratedRegex("^(?:" + Identifier + ")\\z", RegexOptions.ExplicitCapture)]
    private static partial Regex IdentifierRegex();

    /// <summary>
    /// Returns the expression that matches a whole value against the <see cref="ResourceIdentifier" /> pattern.
    /// </summary>
    /// <returns>The expression.</returns>
    [GeneratedRegex("^(?:" + ResourceIdentifier + ")\\z", RegexOptions.ExplicitCapture)]
    private static partial Regex ResourceIdentifierRegex();

    /// <summary>
    /// Returns the expression that matches a whole value against the <see cref="TerritoryCode" /> pattern.
    /// </summary>
    /// <returns>The expression.</returns>
    [GeneratedRegex("^(?:" + TerritoryCode + ")\\z", RegexOptions.ExplicitCapture)]
    private static partial Regex TerritoryCodeRegex();
}
