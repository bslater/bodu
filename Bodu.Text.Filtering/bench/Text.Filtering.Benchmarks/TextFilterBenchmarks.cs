// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextFilterBenchmarks.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.RegularExpressions;
using BenchmarkDotNet.Attributes;

namespace Bodu.Text.Filtering.Benchmarks;

/// <summary>
/// Benchmarks the <see cref="TextFilter" /> engine over a 100k-value corpus across pattern-set shapes and sizes,
/// against a naive one-regex-per-pattern baseline.
/// </summary>
[MemoryDiagnoser]
public class TextFilterBenchmarks
{
    private string[] _corpus = [];
    private TextFilter _literalFilter = null!;
    private TextFilter _mixedFilter = null!;
    private TextFilter _regexHeavyFilter = null!;
    private TextFilter _excludesOnlyFilter = null!;
    private TextFilter _lastMatchWinsFilter = null!;
    private TextFilter _observedFilter = null!;
    private Regex[] _baselineIncludes = [];
    private Regex[] _baselineExcludes = [];

    /// <summary>
    /// Gets or sets the number of patterns compiled into each filter.
    /// </summary>
    [Params(10, 50, 100)]
    public int PatternCount { get; set; }

    /// <summary>
    /// Builds the corpus, the filters, and the baseline regexes.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(20260801);
        string[] stems = ["error", "warn", "info", "debug", "trace", "fatal", "audit", "metric", "session", "request"];
        _corpus = new string[100_000];
        for (var i = 0; i < _corpus.Length; i++)
            _corpus[i] = $"{stems[random.Next(stems.Length)]}-{random.Next(10_000):d4}{(random.Next(4) == 0 ? ".tmp" : ".log")}";

        _literalFilter = TextFilter.Build(BuildPatterns(static i => $"literal-value-{i:d4}.log"));
        _mixedFilter = TextFilter.Build(BuildMixedPatterns());
        _regexHeavyFilter = TextFilter.Build(BuildPatterns(static i => $"^(error|warn)-{i:d4}\\.log$", TextFilterPatternKind.Regex));
        _excludesOnlyFilter = TextFilter.Build(BuildPatterns(static i => $"*-{i:d4}.*", action: TextFilterAction.Exclude));
        _lastMatchWinsFilter = TextFilter.Build(BuildMixedPatterns(), new TextFilterOptions { Mode = TextFilterEvaluationMode.LastMatchWins });
        _observedFilter = TextFilter.Build(BuildMixedPatterns());
        _observedFilter.Observer = new NoOpObserver();

        var baseline = BuildMixedPatterns();
        _baselineIncludes = [.. baseline.Where(static p => p.Action == TextFilterAction.Include).Select(ToBaselineRegex)];
        _baselineExcludes = [.. baseline.Where(static p => p.Action == TextFilterAction.Exclude).Select(ToBaselineRegex)];
    }

    /// <summary>
    /// Filters the corpus through an all-literal pattern set (the cheapest tier).
    /// </summary>
    /// <returns>The number of accepted values.</returns>
    [Benchmark]
    public int Literals() =>
        CountMatches(_literalFilter);

    /// <summary>
    /// Filters the corpus through a mixed-tier pattern set (literal, prefix, suffix, contains, general, regex).
    /// </summary>
    /// <returns>The number of accepted values.</returns>
    [Benchmark]
    public int MixedTiers() =>
        CountMatches(_mixedFilter);

    /// <summary>
    /// Filters the corpus through an all-regex pattern set (the most expensive tier).
    /// </summary>
    /// <returns>The number of accepted values.</returns>
    [Benchmark]
    public int RegexHeavy() =>
        CountMatches(_regexHeavyFilter);

    /// <summary>
    /// Filters the corpus through an excludes-only set (the include-all fast path).
    /// </summary>
    /// <returns>The number of accepted values.</returns>
    [Benchmark]
    public int ExcludesOnly() =>
        CountMatches(_excludesOnlyFilter);

    /// <summary>
    /// Filters the corpus through the mixed set under gitignore-style ordered evaluation.
    /// </summary>
    /// <returns>The number of accepted values.</returns>
    [Benchmark]
    public int LastMatchWins() =>
        CountMatches(_lastMatchWinsFilter);

    /// <summary>
    /// Filters the corpus through the mixed set with a no-op observer attached, quantifying the hook overhead.
    /// </summary>
    /// <returns>The number of accepted values.</returns>
    [Benchmark]
    public int MixedTiersObserved() =>
        CountMatches(_observedFilter);

    /// <summary>
    /// The naive baseline: every pattern (wildcards included) evaluated as its own regex per value.
    /// </summary>
    /// <returns>The number of accepted values.</returns>
    [Benchmark(Baseline = true)]
    public int BaselinePerPatternRegex()
    {
        var count = 0;
        foreach (var value in _corpus)
        {
            var included = _baselineIncludes.Length == 0;
            foreach (var include in _baselineIncludes)
            {
                if (include.IsMatch(value))
                {
                    included = true;
                    break;
                }
            }

            if (!included)
                continue;

            var excluded = false;
            foreach (var exclude in _baselineExcludes)
            {
                if (exclude.IsMatch(value))
                {
                    excluded = true;
                    break;
                }
            }

            if (!excluded)
                count++;
        }

        return count;
    }

    private static Regex ToBaselineRegex(TextFilterPattern pattern)
    {
        var text = pattern.Kind == TextFilterPatternKind.Regex
            ? pattern.Pattern
            : "^" + Regex.Escape(pattern.Pattern).Replace(@"\*", ".*", StringComparison.Ordinal).Replace(@"\?", ".", StringComparison.Ordinal).Replace(@"\{", "(", StringComparison.Ordinal).Replace(@"\}", ")", StringComparison.Ordinal).Replace(",", "|", StringComparison.Ordinal) + "$";

        return new Regex(text, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    }

    private int CountMatches(TextFilter filter)
    {
        var count = 0;
        foreach (var value in _corpus)
        {
            if (filter.IsMatch(value))
                count++;
        }

        return count;
    }

    private List<TextFilterPattern> BuildPatterns(Func<int, string> factory, TextFilterPatternKind kind = TextFilterPatternKind.Wildcard, TextFilterAction action = TextFilterAction.Include)
    {
        var patterns = new List<TextFilterPattern>(PatternCount);
        for (var i = 0; i < PatternCount; i++)
            patterns.Add(new TextFilterPattern(factory(i), action, kind));

        return patterns;
    }

    private List<TextFilterPattern> BuildMixedPatterns()
    {
        var patterns = new List<TextFilterPattern>(PatternCount);
        for (var i = 0; i < PatternCount; i++)
        {
            patterns.Add((i % 6) switch
            {
                0 => TextFilterPattern.Include($"error-{i:d4}.log"),
                1 => TextFilterPattern.Include($"warn-{i % 10}*"),
                2 => TextFilterPattern.Include($"*-{i:d4}.log"),
                3 => TextFilterPattern.Include($"*session-{i % 10}*"),
                4 => TextFilterPattern.Include($"audit-?{i % 10}*.log"),
                _ => TextFilterPattern.Exclude($"^trace-{i % 10}\\d+\\.tmp$", TextFilterPatternKind.Regex),
            });
        }

        return patterns;
    }

    /// <summary>
    /// An observer that does nothing, isolating the cost of the hook dispatch itself.
    /// </summary>
    private sealed class NoOpObserver : ITextFilterObserver
    {
        public void OnEvaluated(ReadOnlySpan<char> value, TextFilterDecision decision, TextFilterPattern? pattern)
        {
        }
    }
}
