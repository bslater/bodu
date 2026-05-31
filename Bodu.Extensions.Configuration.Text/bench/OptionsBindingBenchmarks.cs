// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OptionsBindingBenchmarks.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO;
using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bodu.Extensions.Configuration.Text.Benchmarks;

/// <summary>
/// Measures the cost of binding a Bodu-text-backed configuration onto a POCO via
/// <see cref="ConfigurationOptionsExtensions.AddConfigurationOptions{TOptions}(IServiceCollection, IConfiguration, string)" />
/// and resolving <see cref="IOptions{TOptions}" /> from the resulting service provider.
/// </summary>
[MemoryDiagnoser]
public class OptionsBindingBenchmarks
{
    private const string Source = """
        root = true

        [**/Source.cs]
        logging.level = Information
        logging.provider = Console
        logging.scopes = true
        logging.interval = 5
        """;

    private IConfigurationRoot _configuration = null!;

    /// <summary>
    /// Builds the configuration once so the benchmark measures only the options-binding path.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(Source));
        _configuration = new ConfigurationBuilder()
            .AddConfiguration(stream, targetPath: "src/Source.cs")
            .Build();
    }

    /// <summary>
    /// Benchmarks the helper's full path: register the options, build the provider, and resolve
    /// <see cref="IOptions{TOptions}" /> once.
    /// </summary>
    /// <returns>The resolved options value.</returns>
    [Benchmark(Baseline = true)]
    public LoggingOptions AddOptionsAndResolve()
    {
        ServiceCollection services = new();
        services.AddConfigurationOptions<LoggingOptions>(_configuration, sectionName: "logging");

        using ServiceProvider provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptions<LoggingOptions>>().Value;
    }

    /// <summary>
    /// Benchmarks resolving <see cref="IOptions{TOptions}" /> against a pre-built service provider, isolating
    /// the steady-state cost of options access from one-time wiring.
    /// </summary>
    /// <returns>The resolved options value.</returns>
    [Benchmark]
    public LoggingOptions ResolveFromPreBuiltProvider()
    {
        ServiceCollection services = new();
        services.AddConfigurationOptions<LoggingOptions>(_configuration, sectionName: "logging");
        using ServiceProvider provider = services.BuildServiceProvider();

        // First resolution to warm the options cache.
        _ = provider.GetRequiredService<IOptions<LoggingOptions>>().Value;
        return provider.GetRequiredService<IOptions<LoggingOptions>>().Value;
    }
}

/// <summary>
/// POCO used by <see cref="OptionsBindingBenchmarks" /> to exercise the configuration-binder path under a
/// realistic small set of properties.
/// </summary>
public sealed class LoggingOptions
{
    /// <summary>Gets or sets the configured log level.</summary>
    public string? Level { get; set; }

    /// <summary>Gets or sets the configured logging provider name.</summary>
    public string? Provider { get; set; }

    /// <summary>Gets or sets a value indicating whether scopes are included in log output.</summary>
    public bool Scopes { get; set; }

    /// <summary>Gets or sets the configured flush interval, in seconds.</summary>
    public int Interval { get; set; }
}
