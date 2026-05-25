// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PatternCompileBenchmarks.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using BenchmarkDotNet.Attributes;

namespace Bodu.Text.Configuration.Benchmarks;

/// <summary>
/// Measures the cost of compiling EditorConfig-style glob patterns of varying complexity, including the
/// effect of the process-wide compile cache.
/// </summary>
[MemoryDiagnoser]
public class PatternCompileBenchmarks
{
    private const string SimplePattern = "*.cs";
    private const string ModeratePattern = "**/src/**/*.{cs,vb,fs}";
    private const string ComplexPattern = "**/src/**/{component,module}-[0-9]/file-{1..50}.{cs,vb,fs,csproj}";

    /// <summary>
    /// Benchmarks compilation of a trivial pattern with no alternations or ranges.
    /// </summary>
    /// <returns>The compiled pattern.</returns>
    [Benchmark(Baseline = true)]
    public ConfigurationPattern Compile_Simple() =>
        ConfigurationPattern.Compile(SimplePattern);

    /// <summary>
    /// Benchmarks compilation of a pattern with one brace alternation and one globstar.
    /// </summary>
    /// <returns>The compiled pattern.</returns>
    [Benchmark]
    public ConfigurationPattern Compile_Moderate() =>
        ConfigurationPattern.Compile(ModeratePattern);

    /// <summary>
    /// Benchmarks compilation of a pattern combining nested alternation, character class, and a 50-element
    /// numeric range.
    /// </summary>
    /// <returns>The compiled pattern.</returns>
    [Benchmark]
    public ConfigurationPattern Compile_Complex() =>
        ConfigurationPattern.Compile(ComplexPattern);

    /// <summary>
    /// Benchmarks a cache hit: the same pattern compiled in the prior iteration is served from the
    /// process-wide compile cache rather than re-translated.
    /// </summary>
    /// <returns>The compiled pattern.</returns>
    [Benchmark]
    public ConfigurationPattern Compile_CacheHit() =>
        ConfigurationPattern.Compile(SimplePattern);
}
