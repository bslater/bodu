// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextFilterBuilder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Filtering;

/// <summary>
/// Provides a fluent builder for assembling the patterns of a <see cref="TextFilter" />, in the style of
/// <c>Microsoft.Extensions.FileSystemGlobbing</c>'s <c>Matcher</c>.
/// </summary>
/// <remarks>
/// Patterns accumulate in the order they are added — the order that becomes significant under
/// <see cref="TextFilterEvaluationMode.LastMatchWins" />. The builder itself performs only per-pattern validation;
/// grammar and regular-expression compilation errors surface from <see cref="Build" />.
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// var filter = new TextFilterBuilder()
///     .AddInclude("error*")
///     .AddInclude("{warn,fatal}*")
///     .AddExclude("*debug*")
///     .Build();
///]]>
/// </code>
/// </example>
public sealed class TextFilterBuilder
{
    /// <summary>The accumulated patterns in the order they were added.</summary>
    private readonly List<TextFilterPattern> _patterns = [];

    /// <summary>The options the built filter compiles with, or <see langword="null" /> for defaults.</summary>
    private readonly TextFilterOptions? _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextFilterBuilder" /> class with default options.
    /// </summary>
    public TextFilterBuilder()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextFilterBuilder" /> class with the specified options.
    /// </summary>
    /// <param name="options">The options the built filter compiles with. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="options" /> is <see langword="null" />.</exception>
    public TextFilterBuilder(TextFilterOptions options)
    {
        ThrowHelper.ThrowIfNull(options);

        _options = options;
    }

    /// <summary>
    /// Adds a wildcard include pattern.
    /// </summary>
    /// <param name="globPattern">The wildcard pattern text. Must not be <see langword="null" />.</param>
    /// <returns>This builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="globPattern" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="globPattern" /> is empty or consists solely of white space.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="globPattern" /> exceeds the maximum pattern length.
    /// </exception>
    public TextFilterBuilder AddInclude(string globPattern)
    {
        _patterns.Add(TextFilterPattern.Include(globPattern));

        return this;
    }

    /// <summary>
    /// Adds a wildcard exclude pattern.
    /// </summary>
    /// <param name="globPattern">The wildcard pattern text. Must not be <see langword="null" />.</param>
    /// <returns>This builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="globPattern" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="globPattern" /> is empty or consists solely of white space.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="globPattern" /> exceeds the maximum pattern length.
    /// </exception>
    public TextFilterBuilder AddExclude(string globPattern)
    {
        _patterns.Add(TextFilterPattern.Exclude(globPattern));

        return this;
    }

    /// <summary>
    /// Adds a regular-expression include pattern.
    /// </summary>
    /// <param name="regexPattern">The regular-expression text. Must not be <see langword="null" />.</param>
    /// <returns>This builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="regexPattern" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="regexPattern" /> is empty or consists solely of white space.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="regexPattern" /> exceeds the maximum pattern length.
    /// </exception>
    public TextFilterBuilder AddIncludeRegex(string regexPattern)
    {
        _patterns.Add(TextFilterPattern.Include(regexPattern, TextFilterPatternKind.Regex));

        return this;
    }

    /// <summary>
    /// Adds a regular-expression exclude pattern.
    /// </summary>
    /// <param name="regexPattern">The regular-expression text. Must not be <see langword="null" />.</param>
    /// <returns>This builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="regexPattern" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="regexPattern" /> is empty or consists solely of white space.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="regexPattern" /> exceeds the maximum pattern length.
    /// </exception>
    public TextFilterBuilder AddExcludeRegex(string regexPattern)
    {
        _patterns.Add(TextFilterPattern.Exclude(regexPattern, TextFilterPatternKind.Regex));

        return this;
    }

    /// <summary>
    /// Adds an already constructed pattern.
    /// </summary>
    /// <param name="pattern">The pattern to add. Must not be <see langword="null" />.</param>
    /// <returns>This builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="pattern" /> is <see langword="null" />.</exception>
    public TextFilterBuilder Add(TextFilterPattern pattern)
    {
        ThrowHelper.ThrowIfNull(pattern);

        _patterns.Add(pattern);

        return this;
    }

    /// <summary>
    /// Parses one raw line using the gitignore conventions and adds the resulting pattern, if any.
    /// </summary>
    /// <param name="line">The line to parse. Must not be <see langword="null" />.</param>
    /// <returns>This builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="line" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">The line's pattern text is empty (for example a lone <c>!</c>).</exception>
    /// <remarks>
    /// Blank lines and <c>#</c> comment lines are skipped; a leading <c>!</c> makes the pattern an exclude; <c>\!</c>
    /// and <c>\#</c> escape a literal leading character. See <see cref="TextFilter.Parse(IEnumerable{string})" />.
    /// </remarks>
    public TextFilterBuilder AddParsed(string line)
    {
        ThrowHelper.ThrowIfNull(line);

        if (TextFilter.TryParseLine(line, out var pattern))
            _patterns.Add(pattern);

        return this;
    }

    /// <summary>
    /// Parses raw lines using the gitignore conventions and adds the resulting patterns.
    /// </summary>
    /// <param name="lines">
    /// The lines to parse. Must not be <see langword="null" /> or contain <see langword="null" /> entries.
    /// </param>
    /// <returns>This builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="lines" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="lines" /> contains a <see langword="null" /> entry, or a line's pattern text is empty.
    /// </exception>
    public TextFilterBuilder AddParsed(IEnumerable<string> lines)
    {
        ThrowHelper.ThrowIfNull(lines);

        foreach (var line in lines)
        {
            if (line is null) throw new ArgumentException(FilteringResourceStrings.Arg_Invalid_PatternsContainsNull, nameof(lines));

            if (TextFilter.TryParseLine(line, out var pattern))
                _patterns.Add(pattern);
        }

        return this;
    }

    /// <summary>
    /// Compiles the accumulated patterns into a <see cref="TextFilter" />.
    /// </summary>
    /// <returns>The compiled filter.</returns>
    /// <exception cref="ArgumentException">
    /// A wildcard pattern is malformed or a regular-expression pattern is not valid.
    /// </exception>
    /// <remarks>
    /// The builder can keep accumulating patterns after a build; later builds include them.
    /// </remarks>
    public TextFilter Build() =>
        TextFilter.Build(_patterns, _options);
}
