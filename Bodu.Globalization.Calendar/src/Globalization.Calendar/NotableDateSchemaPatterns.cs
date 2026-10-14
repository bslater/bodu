// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateSchemaPatterns.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Holds the patterns the notable-date XML schema enforces on identifiers, references, territory codes and ordinals, as
/// the schema writes them, so the JSON reader and the strategy constructors apply the same ones.
/// </summary>
internal static partial class NotableDateSchemaPatterns
{
    /// <summary>The pattern of the schema's <c>identifier</c> type.</summary>
    internal const string Identifier = "[a-z][a-z0-9]*(-[a-z0-9]+)*";

    /// <summary>The pattern of the schema's <c>resourceIdentifier</c> type.</summary>
    internal const string ResourceIdentifier = "[a-z][a-z0-9]*([.-][a-z0-9]+)*";

    /// <summary>The pattern of the schema's <c>territoryCode</c> type.</summary>
    internal const string TerritoryCode = "[A-Z]{2}(-[A-Z0-9]{2,3})?";

    /// <summary>The pattern of the schema's <c>nonZeroOrdinal</c> type.</summary>
    internal const string NonZeroOrdinal = "-?[1-9][0-9]*";

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
            NotableDateSchemaPattern.NonZeroOrdinal => NonZeroOrdinal,
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
            NotableDateSchemaPattern.NonZeroOrdinal => NonZeroOrdinalRegex().IsMatch(value),
            _ => throw new ArgumentOutOfRangeException(nameof(pattern)),
        };

    /// <summary>
    /// Checks a value against a pattern, returning the <c>BODU-CAL-SCHEMA</c> error for a value that does not match it.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="pattern">The pattern the schema enforces on the value.</param>
    /// <param name="field">The name of the JSON property the value was read from, used in the diagnostic.</param>
    /// <returns>The diagnostic, or <see langword="null" /> when the value matches the pattern.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="pattern" /> names no pattern.</exception>
    internal static NotableDateValidationDiagnostic? Check(string value, NotableDateSchemaPattern pattern, string field) =>
        IsMatch(value, pattern)
            ? null
            : new(
                NotableDateValidationSeverity.Error,
                "BODU-CAL-SCHEMA",
                string.Format(CultureInfo.CurrentCulture, CalendarResourceStrings.Validation_SchemaPatternMismatch, field, value, GetPattern(pattern)));

    /// <summary>
    /// Throws an <see cref="ArgumentException" /> when a value does not match a pattern as the schema matches it, so a
    /// model constructor refuses a value the loader would reject.
    /// </summary>
    /// <param name="value">The value. Must not be <see langword="null" />.</param>
    /// <param name="pattern">The pattern the schema enforces on the value.</param>
    /// <param name="paramName">The name of the parameter that supplied the value.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="value" /> does not match <paramref name="pattern" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="pattern" /> names no pattern.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfNotMatch(string value, NotableDateSchemaPattern pattern, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (!IsMatch(value, pattern)) throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, CalendarResourceStrings.Arg_Invalid_SchemaPatternMismatch, value, GetPattern(pattern)), paramName);
    }

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

    /// <summary>
    /// Returns the expression that matches a whole value against the <see cref="NonZeroOrdinal" /> pattern.
    /// </summary>
    /// <returns>The expression.</returns>
    [GeneratedRegex("^(?:" + NonZeroOrdinal + ")\\z", RegexOptions.ExplicitCapture)]
    private static partial Regex NonZeroOrdinalRegex();
}
