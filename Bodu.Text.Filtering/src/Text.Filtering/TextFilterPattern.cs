// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextFilterPattern.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Text.Filtering;

/// <summary>
/// Represents one immutable filtering rule: a pattern, the action taken when it matches, the syntax the pattern is
/// written in, and an optional case-sensitivity override.
/// </summary>
/// <remarks>
/// <para>
/// Patterns do not match values on their own; they are compiled — together with the other patterns of a rule set — into
/// a <see cref="TextFilter" /> via
/// <see cref="TextFilter.Build(System.Collections.Generic.IEnumerable{TextFilterPattern})" />. Wildcard grammar errors
/// and regular-expression syntax errors are reported at that point, not by this constructor.
/// </para>
/// <para>
/// The constructor validates only shape-independent rules: the pattern text must be non-empty, must not consist solely
/// of white space, and must not exceed <see cref="MaxPatternLength" /> characters (a guard against pathological
/// patterns, mirroring the limits used elsewhere in the Bodu libraries).
/// </para>
/// </remarks>
public sealed class TextFilterPattern
{
    /// <summary>The maximum number of characters permitted in a pattern.</summary>
    internal const int MaxPatternLength = 4096;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextFilterPattern" /> class.
    /// </summary>
    /// <param name="pattern">The pattern text. Must not be <see langword="null" />.</param>
    /// <param name="action">The action taken when the pattern matches a value.</param>
    /// <param name="kind">The syntax used to interpret <paramref name="pattern" />.</param>
    /// <param name="ignoreCase">
    /// An optional case-sensitivity override for this pattern; <see langword="null" /> inherits the
    /// <see cref="TextFilterOptions.IgnoreCase" /> setting of the filter the pattern is compiled into.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="pattern" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="pattern" /> is empty or consists solely of white space.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="pattern" /> is longer than <see cref="MaxPatternLength" /> characters, or
    /// <paramref name="action" /> or <paramref name="kind" /> is not a defined enumeration value.
    /// </exception>
    public TextFilterPattern(
        string pattern,
        TextFilterAction action,
        TextFilterPatternKind kind = TextFilterPatternKind.Wildcard,
        bool? ignoreCase = null)
    {
        ThrowHelper.ThrowIfNull(pattern);
        if (string.IsNullOrWhiteSpace(pattern)) throw new ArgumentException(FilteringResourceStrings.Arg_Invalid_PatternEmpty, nameof(pattern));
        if (pattern.Length > MaxPatternLength) throw new ArgumentOutOfRangeException(nameof(pattern), string.Format(CultureInfo.CurrentCulture, FilteringResourceStrings.Arg_OutOfRange_PatternTooLong, MaxPatternLength));
        ThrowHelper.ThrowIfEnumValueIsUndefined(action);
        ThrowHelper.ThrowIfEnumValueIsUndefined(kind);

        Pattern = pattern;
        Action = action;
        Kind = kind;
        IgnoreCase = ignoreCase;
    }

    /// <summary>
    /// Gets the pattern text as originally supplied.
    /// </summary>
    public string Pattern { get; }

    /// <summary>
    /// Gets the action taken when the pattern matches a value.
    /// </summary>
    public TextFilterAction Action { get; }

    /// <summary>
    /// Gets the syntax used to interpret <see cref="Pattern" />.
    /// </summary>
    public TextFilterPatternKind Kind { get; }

    /// <summary>
    /// Gets the case-sensitivity override for this pattern.
    /// </summary>
    /// <value>
    /// <see langword="true" /> to ignore case, <see langword="false" /> to match case-sensitively, or
    /// <see langword="null" /> to inherit the <see cref="TextFilterOptions.IgnoreCase" /> setting of the filter the
    /// pattern is compiled into.
    /// </value>
    public bool? IgnoreCase { get; }

    /// <summary>
    /// Creates an include pattern.
    /// </summary>
    /// <param name="pattern">The pattern text. Must not be <see langword="null" />.</param>
    /// <param name="kind">The syntax used to interpret <paramref name="pattern" />.</param>
    /// <param name="ignoreCase">
    /// An optional case-sensitivity override for this pattern; <see langword="null" /> inherits the filter's setting.
    /// </param>
    /// <returns>
    /// A new <see cref="TextFilterPattern" /> whose <see cref="Action" /> is <see cref="TextFilterAction.Include" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="pattern" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="pattern" /> is empty or consists solely of white space.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="pattern" /> is longer than <see cref="MaxPatternLength" /> characters, or
    /// <paramref name="kind" /> is not a defined enumeration value.
    /// </exception>
    public static TextFilterPattern Include(
        string pattern,
        TextFilterPatternKind kind = TextFilterPatternKind.Wildcard,
        bool? ignoreCase = null) =>
        new(pattern, TextFilterAction.Include, kind, ignoreCase);

    /// <summary>
    /// Creates an exclude pattern.
    /// </summary>
    /// <param name="pattern">The pattern text. Must not be <see langword="null" />.</param>
    /// <param name="kind">The syntax used to interpret <paramref name="pattern" />.</param>
    /// <param name="ignoreCase">
    /// An optional case-sensitivity override for this pattern; <see langword="null" /> inherits the filter's setting.
    /// </param>
    /// <returns>
    /// A new <see cref="TextFilterPattern" /> whose <see cref="Action" /> is <see cref="TextFilterAction.Exclude" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="pattern" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="pattern" /> is empty or consists solely of white space.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="pattern" /> is longer than <see cref="MaxPatternLength" /> characters, or
    /// <paramref name="kind" /> is not a defined enumeration value.
    /// </exception>
    public static TextFilterPattern Exclude(
        string pattern,
        TextFilterPatternKind kind = TextFilterPatternKind.Wildcard,
        bool? ignoreCase = null) =>
        new(pattern, TextFilterAction.Exclude, kind, ignoreCase);

    /// <summary>
    /// Returns a diagnostic representation of the pattern in the form <c>+wildcard:text</c> or <c>-regex:text</c>,
    /// where the leading sign reflects <see cref="Action" />.
    /// </summary>
    /// <returns>A string describing the pattern for diagnostic display.</returns>
    public override string ToString() =>
        string.Concat(
            Action == TextFilterAction.Include ? "+" : "-",
            Kind == TextFilterPatternKind.Wildcard ? "wildcard:" : "regex:",
            Pattern);
}
