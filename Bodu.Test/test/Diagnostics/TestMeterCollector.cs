// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TestMeterCollector.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.Metrics;

namespace Bodu.Test.Diagnostics;

/// <summary>
/// Collects <see cref="Counter{T}" /> measurements from a named <see cref="Meter" /> for the lifetime of the
/// collector, so a test can assert what a component recorded without any external metrics pipeline.
/// </summary>
/// <remarks>
/// <para>
/// Production meters are process-wide statics and tests run in parallel, so a test must not assert global totals:
/// filter with <see cref="Measurements(string)" /> plus a tag value unique to the test (a unique provider name or
/// territory) and assert only over that slice.
/// </para>
/// <para>
/// The collector subscribes to every instrument published by meters whose <see cref="Meter.Name" /> matches the one
/// supplied, including instruments created before the collector started. Dispose the collector to stop listening.
/// </para>
/// </remarks>
public sealed class TestMeterCollector
    : IDisposable
{
    /// <summary>The recorded measurements.</summary>
    private readonly List<(string Instrument, long Value, KeyValuePair<string, object?>[] Tags)> _measurements = new();

    /// <summary>The underlying listener.</summary>
    private readonly MeterListener _listener;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestMeterCollector" /> class listening to one meter.
    /// </summary>
    /// <param name="meterName">The <see cref="Meter.Name" /> whose instruments are collected.</param>
    public TestMeterCollector(string meterName)
    {
        _listener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (string.Equals(instrument.Meter.Name, meterName, StringComparison.Ordinal))
                    listener.EnableMeasurementEvents(instrument);
            },
        };

        _listener.SetMeasurementEventCallback<long>((instrument, value, tags, state) =>
        {
            lock (_measurements)
                _measurements.Add((instrument.Name, value, tags.ToArray()));
        });

        _listener.Start();
    }

    /// <summary>
    /// Returns the measurements recorded for one instrument.
    /// </summary>
    /// <param name="instrument">The instrument name, such as <c>bodu.financial.rate_cache.hits</c>.</param>
    /// <returns>The matching measurements, in recording order.</returns>
    public IReadOnlyList<(string Instrument, long Value, KeyValuePair<string, object?>[] Tags)> Measurements(string instrument)
    {
        lock (_measurements)
            return _measurements.Where(m => string.Equals(m.Instrument, instrument, StringComparison.Ordinal)).ToArray();
    }

    /// <summary>
    /// Sums the values recorded for one instrument, optionally restricted to measurements carrying a tag value.
    /// </summary>
    /// <param name="instrument">The instrument name.</param>
    /// <param name="tagKey">The tag to filter on, or <see langword="null" /> for no tag filter.</param>
    /// <param name="tagValue">The tag value the measurement must carry when <paramref name="tagKey" /> is supplied.</param>
    /// <returns>The summed value across the matching measurements.</returns>
    public long Sum(string instrument, string? tagKey = null, object? tagValue = null)
    {
        long sum = 0;
        foreach ((_, long value, KeyValuePair<string, object?>[] tags) in Measurements(instrument))
        {
            if (tagKey is null || tags.Any(t => string.Equals(t.Key, tagKey, StringComparison.Ordinal) && Equals(t.Value, tagValue)))
                sum += value;
        }

        return sum;
    }

    /// <summary>
    /// Stops listening and releases the underlying listener.
    /// </summary>
    public void Dispose() => _listener.Dispose();
}
