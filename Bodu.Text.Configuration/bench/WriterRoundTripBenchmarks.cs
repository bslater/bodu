// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WriterRoundTripBenchmarks.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO;
using System.Text;
using BenchmarkDotNet.Attributes;
using Bodu.Text.Ini;

namespace Bodu.Text.Configuration.Benchmarks;

/// <summary>
/// Measures round-trip throughput: parse a document and then write it back via
/// <see cref="ConfigurationDocument.Save(IniDocument, Stream, ConfigurationWriteOptions?, bool)" />.
/// </summary>
[MemoryDiagnoser]
public class WriterRoundTripBenchmarks
{
    private string _source = string.Empty;
    private IniDocument _parsed = null!;

    /// <summary>
    /// Gets or sets the number of sections in the synthetic document round-tripped by each iteration.
    /// </summary>
    [Params(10, 100, 1_000)]
    public int SectionCount { get; set; }

    /// <summary>
    /// Builds the synthetic document text once and pre-parses it so write benchmarks measure write cost only.
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
            sb.Append("[section-").Append(i).Append("/*.cs").AppendLine("]");
            sb.AppendLine("indent_style = space");
            sb.AppendLine("indent_size = 4");
            sb.AppendLine();
        }

        _source = sb.ToString();
        _parsed = ConfigurationDocument.Parse(_source);
    }

    /// <summary>
    /// Benchmarks parse followed by save, capturing the cold round-trip cost a tool would pay when reading and
    /// rewriting a configuration file.
    /// </summary>
    /// <returns>The number of bytes written.</returns>
    [Benchmark(Baseline = true)]
    public long ParseAndSave()
    {
        IniDocument doc = ConfigurationDocument.Parse(_source);
        using MemoryStream stream = new();
        ConfigurationDocument.Save(doc, stream);
        return stream.Length;
    }

    /// <summary>
    /// Benchmarks save against a pre-parsed document, isolating the write half of the round trip.
    /// </summary>
    /// <returns>The number of bytes written.</returns>
    [Benchmark]
    public long SaveOnly()
    {
        using MemoryStream stream = new();
        ConfigurationDocument.Save(_parsed, stream);
        return stream.Length;
    }
}
