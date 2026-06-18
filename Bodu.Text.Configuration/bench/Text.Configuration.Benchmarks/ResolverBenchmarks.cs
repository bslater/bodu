// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResolverBenchmarks.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using BenchmarkDotNet.Attributes;
using Bodu.Text.Ini;

namespace Bodu.Text.Configuration.Benchmarks;

/// <summary>
/// Measures throughput of <see cref="ConfigurationExtensions.Resolve(IniDocumentBase, string?)" /> when applied
/// to documents with progressively more sections, exercising the compiled-pattern cache that
/// <see cref="ConfigurationPattern" /> maintains.
/// </summary>
[MemoryDiagnoser]
public class ResolverBenchmarks
{
    private ConfigurationDocument _document = null!;
    private string _targetPath = string.Empty;

    /// <summary>
    /// Gets or sets the number of section patterns in the document resolved by each iteration.
    /// </summary>
    [Params(10, 100, 1_000)]
    public int SectionCount { get; set; }

    /// <summary>
    /// Parses the synthetic document once and chooses a target path that matches roughly one in ten sections.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        StringBuilder sb = new();
        sb.AppendLine("root = true");
        sb.AppendLine();

        for (var i = 0; i < SectionCount; i++)
        {
            sb.Append("[**/section-").Append(i).Append("/*.{cs,vb,fs}").AppendLine("]");
            sb.Append("indent_size = ").Append(i % 8 + 1).AppendLine();
        }

        _document = ConfigurationDocument.Parse(sb.ToString());
        _targetPath = $"src/section-{SectionCount / 2}/Source.cs";
    }

    /// <summary>
    /// Benchmarks resolution against a target path that matches a single section near the middle of the
    /// document. The compiled-pattern cache is warm after the first call.
    /// </summary>
    /// <returns>The resolved configuration view.</returns>
    [Benchmark(Baseline = true)]
    public ConfigurationView Resolve_SingleTarget() =>
        _document.Resolve(_targetPath);

    /// <summary>
    /// Benchmarks resolution against a target path that matches no section in the document, forcing every
    /// section pattern to be compared.
    /// </summary>
    /// <returns>The resolved configuration view (containing only preamble values).</returns>
    [Benchmark]
    public ConfigurationView Resolve_NonMatchingTarget() =>
        _document.Resolve("src/no-such-section/Source.cs");
}
