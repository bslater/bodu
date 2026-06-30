// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ProviderLoadBenchmarks.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Configuration;

namespace Bodu.Extensions.Configuration.Text.Benchmarks;

/// <summary>
/// Measures cold-build cost of <see cref="ConfigurationBuilder" /> when the configuration source is a Bodu text
/// configuration. Covers both file-backed and stream-backed sources across small/medium/large documents.
/// </summary>
[MemoryDiagnoser]
public class ProviderLoadBenchmarks
{
    private string _filePath = string.Empty;
    private byte[] _bytes = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the number of sections in the synthetic document each iteration loads.
    /// </summary>
    [Params(10, 100, 1_000)]
    public int SectionCount { get; set; }

    /// <summary>
    /// Builds the synthetic document on disk so file-backed benchmarks measure the warm-OS-cache load cost.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        StringBuilder sb = new();
        sb.AppendLine("root = true");
        sb.AppendLine();
        for (var i = 0; i < SectionCount; i++)
        {
            sb.Append("[**/section-").Append(i).Append("/*.cs").AppendLine("]");
            sb.AppendLine("indent_style = space");
            sb.AppendLine("indent_size = 4");
        }

        _filePath = Path.Combine(Path.GetTempPath(), $"bodu-bench-{Guid.NewGuid():N}.boduconfig");
        var content = sb.ToString();
        File.WriteAllText(_filePath, content);
        _bytes = Encoding.UTF8.GetBytes(content);
    }

    /// <summary>
    /// Cleans up the temporary file produced by <see cref="Setup" />.
    /// </summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        if (File.Exists(_filePath))
            File.Delete(_filePath);
    }

    /// <summary>
    /// Benchmarks building a configuration from the file source, including provider load and resolution.
    /// </summary>
    /// <returns>The built configuration root.</returns>
    [Benchmark(Baseline = true)]
    public IConfigurationRoot Build_FromFile() =>
        new ConfigurationBuilder()
            .AddTextConfigurationFile(_filePath, targetPath: "src/section-0/Source.cs")
            .Build();

    /// <summary>
    /// Benchmarks building a configuration from an in-memory stream, isolating parse and resolve from any filesystem
    /// cost.
    /// </summary>
    /// <returns>The built configuration root.</returns>
    [Benchmark]
    public IConfigurationRoot Build_FromStream()
    {
        using MemoryStream stream = new(_bytes);
        return new ConfigurationBuilder()
            .AddTextConfigurationStream(stream, targetPath: "src/section-0/Source.cs")
            .Build();
    }

    /// <summary>
    /// Benchmarks building a configuration with <c>targetPath: null</c>, which yields only the preamble — a useful
    /// baseline for how much of the build cost is glob resolution versus everything else.
    /// </summary>
    /// <returns>The built configuration root.</returns>
    [Benchmark]
    public IConfigurationRoot Build_FromStream_PreambleOnly()
    {
        using MemoryStream stream = new(_bytes);
        return new ConfigurationBuilder()
            .AddTextConfigurationStream(stream, targetPath: null)
            .Build();
    }
}
