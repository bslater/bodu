// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextFilter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text.RegularExpressions;

namespace Bodu.Text.Filtering;

/// <summary>
/// Represents an immutable, compiled include/exclude text filter that evaluates values against a set of wildcard and
/// regular-expression patterns, running the cheapest pattern strategies first.
/// </summary>
/// <remarks>
/// <para>
/// A filter is built once from its patterns via <see cref="Build(IEnumerable{TextFilterPattern})" /> (or parsed from
/// raw lines via <see cref="Parse(IEnumerable{string})" />) and then evaluated many times. At build time every wildcard
/// pattern is classified into the cheapest strategy its shape permits — whole-string equality for literals,
/// prefix/suffix comparison for <c>abc*</c> / <c>*abc</c> / <c>abc*def</c>, substring search for <c>*abc*</c>, the
/// general wildcard matcher otherwise — and regular expressions compile preferring the linear-time
/// <see cref="RegexOptions.NonBacktracking" /> engine. In <see cref="TextFilterEvaluationMode.AnyMatch" /> mode the
/// include and exclude groups are additionally evaluated cheapest-strategy-first, which cannot change the outcome
/// because group matching is an order-independent OR.
/// </para>
/// <para>
/// <b>Semantics.</b> In <see cref="TextFilterEvaluationMode.AnyMatch" /> (the default), a value is accepted when the
/// include set is empty or at least one include matches, and no exclude matches — the Ant / MSBuild model. In
/// <see cref="TextFilterEvaluationMode.LastMatchWins" />, the last matching rule decides and unmatched values are
/// included — the gitignore model.
/// </para>
/// <para>
/// <b>Thread safety.</b> The compiled matching state is immutable, so <see cref="IsMatch(string)" />,
/// <see cref="Evaluate" />, and the filtering members are safe for concurrent use and always produce exact results. The
/// statistics counters are deliberately not interlocked and may undercount under concurrent evaluation; see
/// <see cref="TextFilterStatistics" />.
/// </para>
/// </remarks>
public sealed partial class TextFilter
{
    /// <summary>The options applied when the caller supplies none.</summary>
    private static readonly TextFilterOptions s_defaultOptions = new();

    /// <summary>The declared patterns in declaration order; <see cref="CompiledPattern.SourceIndex" /> values index this array.</summary>
    private readonly TextFilterPattern[] _patterns;

    /// <summary>The evaluation mode resolved from the build options.</summary>
    private readonly TextFilterEvaluationMode _mode;

    /// <summary>Whether the include group is empty in <see cref="TextFilterEvaluationMode.AnyMatch" /> mode, making every value included unless an exclude vetoes it.</summary>
    private readonly bool _includeAll;

    /// <summary>The compiled include patterns in cost order (<see cref="TextFilterEvaluationMode.AnyMatch" /> mode only).</summary>
    private readonly CompiledPattern[] _includes;

    /// <summary>The compiled exclude patterns in cost order (<see cref="TextFilterEvaluationMode.AnyMatch" /> mode only).</summary>
    private readonly CompiledPattern[] _excludes;

    /// <summary>All compiled patterns in declaration order (<see cref="TextFilterEvaluationMode.LastMatchWins" /> mode only).</summary>
    private readonly CompiledPattern[] _ordered;

    /// <summary>Whether evaluation time is measured and accumulated into the statistics.</summary>
    private readonly bool _captureTime;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextFilter" /> class from compiled state.
    /// </summary>
    /// <param name="patterns">The declared patterns in declaration order.</param>
    /// <param name="mode">The evaluation mode.</param>
    /// <param name="ignoreCase">The resolved case-insensitivity default.</param>
    /// <param name="captureTime">Whether evaluation time is accumulated.</param>
    /// <param name="includes">The compiled include patterns in cost order (empty outside AnyMatch mode).</param>
    /// <param name="excludes">The compiled exclude patterns in cost order (empty outside AnyMatch mode).</param>
    /// <param name="ordered">All compiled patterns in declaration order (empty outside LastMatchWins mode).</param>
    private TextFilter(
        TextFilterPattern[] patterns,
        TextFilterEvaluationMode mode,
        bool ignoreCase,
        bool captureTime,
        CompiledPattern[] includes,
        CompiledPattern[] excludes,
        CompiledPattern[] ordered)
    {
        _patterns = patterns;
        _mode = mode;
        IgnoreCase = ignoreCase;
        _captureTime = captureTime;
        _includes = includes;
        _excludes = excludes;
        _ordered = ordered;
        _includeAll = mode == TextFilterEvaluationMode.AnyMatch && includes.Length == 0;
        _hitCounts = new long[patterns.Length];
    }

    /// <summary>
    /// Gets the declared patterns in declaration order.
    /// </summary>
    public IReadOnlyList<TextFilterPattern> Patterns =>
        _patterns;

    /// <summary>
    /// Gets the evaluation mode this filter was built with.
    /// </summary>
    public TextFilterEvaluationMode Mode =>
        _mode;

    /// <summary>
    /// Gets a value indicating whether patterns without a per-pattern override match case-insensitively.
    /// </summary>
    public bool IgnoreCase { get; }

    /// <summary>
    /// Gets or sets the observer notified of every evaluation decision, or <see langword="null" /> for none.
    /// </summary>
    /// <remarks>
    /// When unset the evaluation path pays only a single null check. The observer is invoked synchronously on the
    /// evaluating thread; exceptions it throws propagate to the caller of the evaluating member.
    /// </remarks>
    public ITextFilterObserver? Observer { get; set; }

    /// <summary>
    /// Builds a filter from the specified patterns with default options.
    /// </summary>
    /// <param name="patterns">
    /// The patterns to compile. Must not be <see langword="null" /> or contain <see langword="null" /> entries.
    /// </param>
    /// <returns>The compiled filter.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="patterns" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="patterns" /> contains a <see langword="null" /> entry, a wildcard pattern is malformed, or a
    /// regular-expression pattern is not valid (the original parse failure is preserved as the inner exception).
    /// </exception>
    /// <remarks>
    /// An empty pattern set is legal and yields a filter that accepts every value.
    /// </remarks>
    public static TextFilter Build(IEnumerable<TextFilterPattern> patterns) =>
        Build(patterns, null);

    /// <summary>
    /// Builds a filter from the specified patterns and options.
    /// </summary>
    /// <param name="patterns">
    /// The patterns to compile. Must not be <see langword="null" /> or contain <see langword="null" /> entries.
    /// </param>
    /// <param name="options">The build options, or <see langword="null" /> for defaults.</param>
    /// <returns>The compiled filter.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="patterns" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="patterns" /> contains a <see langword="null" /> entry, a wildcard pattern is malformed, or a
    /// regular-expression pattern is not valid (the original parse failure is preserved as the inner exception).
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The options carry an undefined <see cref="TextFilterOptions.Mode" /> or a
    /// <see cref="TextFilterOptions.RegexMatchTimeout" /> that is neither positive nor
    /// <see cref="Regex.InfiniteMatchTimeout" />.
    /// </exception>
    /// <remarks>
    /// An empty pattern set is legal and yields a filter that accepts every value.
    /// </remarks>
    public static TextFilter Build(IEnumerable<TextFilterPattern> patterns, TextFilterOptions? options)
    {
        ThrowHelper.ThrowIfNull(patterns);
        options ??= s_defaultOptions;
        ThrowHelper.ThrowIfEnumValueIsUndefined(options.Mode);
        if (options.RegexMatchTimeout != Regex.InfiniteMatchTimeout && options.RegexMatchTimeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(options), FilteringResourceStrings.Arg_OutOfRange_RegexMatchTimeout);

        var declared = new List<TextFilterPattern>(patterns);
        if (declared.Contains(null!)) throw new ArgumentException(FilteringResourceStrings.Arg_Invalid_PatternsContainsNull, nameof(patterns));

        var compiled = new List<CompiledPattern>();
        for (var i = 0; i < declared.Count; i++)
            CompilePattern(declared[i], i, options, compiled);

        CompiledPattern[] includes = [];
        CompiledPattern[] excludes = [];
        CompiledPattern[] ordered = [];
        if (options.Mode == TextFilterEvaluationMode.AnyMatch)
        {
            // Stable cost sort: group matching is an order-independent OR, so running cheap strategies first can
            // only change which matching pattern is reported, never the decision.
            includes = [.. compiled.Where(static p => !p.IsExclude).OrderBy(static p => p.CostTier)];
            excludes = [.. compiled.Where(static p => p.IsExclude).OrderBy(static p => p.CostTier)];
        }
        else
        {
            // LastMatchWins keeps declaration order verbatim — order IS the semantics there, so no cost sorting.
            // Brace-expanded alternatives stay adjacent to their siblings, preserving each rule's position.
            ordered = [.. compiled];
        }

        return new TextFilter([.. declared], options.Mode, options.IgnoreCase, options.CaptureEvaluationTime, includes, excludes, ordered);
    }

    /// <summary>
    /// Compiles one declared pattern into its compiled entries, expanding wildcard brace alternations into one entry
    /// per alternative (all sharing the declared pattern's index).
    /// </summary>
    /// <param name="pattern">The declared pattern.</param>
    /// <param name="sourceIndex">The pattern's declaration index.</param>
    /// <param name="options">The build options.</param>
    /// <param name="compiled">The accumulator receiving the compiled entries.</param>
    /// <exception cref="ArgumentException">The pattern is malformed.</exception>
    private static void CompilePattern(TextFilterPattern pattern, int sourceIndex, TextFilterOptions options, List<CompiledPattern> compiled)
    {
        var ignoreCase = pattern.IgnoreCase ?? options.IgnoreCase;
        var isExclude = pattern.Action == TextFilterAction.Exclude;

        if (pattern.Kind == TextFilterPatternKind.Wildcard)
        {
            foreach (var alternative in WildcardPattern.ExpandBraces(pattern.Pattern))
            {
                var program = WildcardPattern.Compile(alternative);
                compiled.Add(new CompiledPattern(program, null, ignoreCase, isExclude, sourceIndex));
            }
        }
        else
        {
            var regex = CreateRegex(pattern.Pattern, ignoreCase, options.RegexMatchTimeout);
            compiled.Add(new CompiledPattern(null, regex, ignoreCase, isExclude, sourceIndex));
        }
    }

    /// <summary>
    /// Constructs the <see cref="Regex" /> for a regular-expression pattern, preferring the linear-time
    /// <see cref="RegexOptions.NonBacktracking" /> engine and falling back to the backtracking engine for constructs it
    /// does not support.
    /// </summary>
    /// <param name="pattern">The regular-expression text.</param>
    /// <param name="ignoreCase">Whether the expression matches case-insensitively.</param>
    /// <param name="timeout">The per-match timeout.</param>
    /// <returns>The constructed expression.</returns>
    /// <exception cref="ArgumentException">The pattern is not a valid regular expression.</exception>
    /// <remarks>
    /// <see cref="RegexOptions.NonBacktracking" /> guarantees linear-time matching — exactly what a bulk filtering
    /// engine wants — but rejects backreferences, lookarounds, and automata beyond its node budget with
    /// <see cref="NotSupportedException" />; those patterns rebuild on the backtracking engine, where
    /// <paramref name="timeout" /> is the ReDoS guard.
    /// </remarks>
    private static Regex CreateRegex(string pattern, bool ignoreCase, TimeSpan timeout)
    {
        var regexOptions = RegexOptions.CultureInvariant | (ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
        try
        {
            return new Regex(pattern, regexOptions | RegexOptions.NonBacktracking, timeout);
        }
        catch (NotSupportedException)
        {
            // Fall through to the backtracking engine below.
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, FilteringResourceStrings.Arg_Invalid_RegexPattern, pattern), nameof(pattern), ex);
        }

        try
        {
            return new Regex(pattern, regexOptions, timeout);
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, FilteringResourceStrings.Arg_Invalid_RegexPattern, pattern), nameof(pattern), ex);
        }
    }
}
