// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ProviderReloadBenchmarks.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO;
using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Configuration;

namespace Bodu.Extensions.Configuration.Text.Benchmarks;

/// <summary>
/// Measures the cost of <see cref="IConfigurationRoot.Reload" /> on a Bodu-text-backed configuration,
/// approximating the work the change-token plumbing would trigger when the underlying file changes.
/// </summary>
[MemoryDiagnoser]
public class ProviderReloadBenchmarks
{
    private string _filePath = string.Empty;
    private IConfigurationRoot _root = null!;

    /// <summary>
    /// Gets or sets the number of sections in the synthetic document each iteration reloads.
    /// </summary>
    [Params(10, 100, 1_000)]
    public int SectionCount { get; set; }

    /// <summary>
    /// Builds the synthetic document on disk and constructs the configuration root once so reload benchmarks
    /// measure reload cost only.
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

        _filePath = Path.Combine(Path.GetTempPath(), $"bodu-bench-reload-{Guid.NewGuid():N}.boduconfig");
        File.WriteAllText(_filePath, sb.ToString());

        _root = new ConfigurationBuilder()
            .AddConfiguration(_filePath, targetPath: "src/section-0/Source.cs", optional: false, reloadOnChange: true)
            .Build();
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
    /// Benchmarks the cost of an explicit <see cref="IConfigurationRoot.Reload" /> call, which re-reads and
    /// re-resolves the underlying file without rebuilding the entire <see cref="IConfigurationBuilder" />.
    /// </summary>
    [Benchmark]
    public void Reload() =>
        _root.Reload();
}
