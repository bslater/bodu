// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedBenchmarks.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using BenchmarkDotNet.Attributes;

namespace Bodu.Text.Formats.Benchmarks;

/// <summary>
/// Measures the throughput of <see cref="Bodu.Text.Delimited.Delimited" /> parse and format at row counts
/// representative of configuration files, application data, and ETL extracts.
/// </summary>
[MemoryDiagnoser]
public class DelimitedBenchmarks
{
    private string _source = string.Empty;
    private Bodu.Text.Delimited.DelimitedDocument _document = null!;

    /// <summary>
    /// Gets or sets the row count used to construct the input document for this iteration.
    /// </summary>
    [Params(10, 1_000, 100_000, 1_000_000)]
    public int RowCount { get; set; }

    /// <summary>
    /// Builds a fixed CSV source of <see cref="RowCount" /> rows with a three-column header.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        StringBuilder sb = new();
        sb.AppendLine("Id,Name,Value");

        for (var i = 0; i < RowCount; i++)
            sb.Append(i).Append(",Row").Append(i).Append(',').AppendLine(i.ToString(System.Globalization.CultureInfo.InvariantCulture));

        _source = sb.ToString();
        _document = Bodu.Text.Delimited.Delimited.Parse(_source.AsSpan());
    }

    /// <summary>
    /// Benchmarks delimited parse over the prepared CSV source.
    /// </summary>
    /// <returns>The parsed document.</returns>
    [Benchmark]
    public Bodu.Text.Delimited.DelimitedDocument ParseDocument() =>
        Bodu.Text.Delimited.Delimited.Parse(_source.AsSpan());

    /// <summary>
    /// Benchmarks delimited format over the prepared document.
    /// </summary>
    /// <returns>The serialised CSV string.</returns>
    [Benchmark]
    public string FormatDocument() =>
        Bodu.Text.Delimited.Delimited.Format(_document, Bodu.Text.Delimited.DelimitedParseOptions.Default);
}
