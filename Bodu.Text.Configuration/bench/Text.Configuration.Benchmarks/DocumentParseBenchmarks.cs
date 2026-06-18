// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DocumentParseBenchmarks.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using BenchmarkDotNet.Attributes;

namespace Bodu.Text.Configuration.Benchmarks;

/// <summary>
/// Measures throughput of <see cref="ConfigurationDocument.Parse(string)" /> across document sizes that span
/// a single-section <c>.editorconfig</c> through to multi-thousand-section synthetic documents.
/// </summary>
[MemoryDiagnoser]
public class DocumentParseBenchmarks
{
    private string _source = string.Empty;

    /// <summary>
    /// Gets or sets the number of sections in the synthetic document parsed by each iteration.
    /// </summary>
    [Params(10, 100, 1_000)]
    public int SectionCount { get; set; }

    /// <summary>
    /// Builds the synthetic document once so per-iteration cost reflects parse, not setup.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        StringBuilder sb = new();
        sb.AppendLine("# Synthetic configuration for benchmarking.");
        sb.AppendLine("root = true");
        sb.AppendLine();

        for (var i = 0; i < SectionCount; i++)
        {
            sb.Append('[').Append("section-").Append(i).Append("/*.cs").AppendLine("]");
            sb.AppendLine("indent_style = space");
            sb.AppendLine("indent_size = 4");
            sb.Append("custom.key.").Append(i).Append(" = value-").Append(i).AppendLine();
            sb.AppendLine();
        }

        _source = sb.ToString();
    }

    /// <summary>
    /// Benchmarks parsing under the default Bodu profile.
    /// </summary>
    /// <returns>The parsed document.</returns>
    [Benchmark(Baseline = true)]
    public ConfigurationDocument Parse_Default() =>
        ConfigurationDocument.Parse(_source);

    /// <summary>
    /// Benchmarks parsing under the EditorConfig-compatible profile, which selects strict header parsing and
    /// disables inline comments.
    /// </summary>
    /// <returns>The parsed document.</returns>
    [Benchmark]
    public ConfigurationDocument Parse_EditorConfigCompatible() =>
        ConfigurationDocument.Parse(_source, ConfigurationParseOptions.EditorConfigCompatible);

    /// <summary>
    /// Benchmarks parsing with collect-diagnostics mode, which incurs additional list allocation and
    /// diagnostic-object overhead even on clean input.
    /// </summary>
    /// <returns>The parse result including diagnostic collection.</returns>
    [Benchmark]
    public ConfigurationParseResult Parse_WithDiagnostics() =>
        ConfigurationDocument.ParseWithDiagnostics(_source, new ConfigurationParseOptions { DiagnosticMode = ConfigurationDiagnosticMode.Collect });
}
