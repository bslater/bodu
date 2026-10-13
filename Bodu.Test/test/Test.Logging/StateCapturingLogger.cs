// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StateCapturingLogger.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Bodu.Test.Logging;

/// <summary>
/// An <see cref="ILogger" /> that records the structured values of every logged entry, keyed by their template names,
/// so tests can assert on the arguments a component logged rather than on its formatted text.
/// </summary>
/// <remarks>
/// Every level is enabled. Recording is thread-safe and <see cref="Entries" /> returns a snapshot in emission order.
/// An entry whose state is not a list of name and value pairs records no values.
/// </remarks>
public sealed class StateCapturingLogger
    : ILogger
{
    /// <summary>The recorded entries, guarded by its own lock for concurrent writes.</summary>
    private readonly List<(EventId EventId, IReadOnlyDictionary<string, object?> Values)> _entries = new();

    /// <summary>
    /// Gets a snapshot of the entries recorded so far.
    /// </summary>
    /// <value>Each entry's event id and its structured values by template name, in emission order.</value>
    public IReadOnlyList<(EventId EventId, IReadOnlyDictionary<string, object?> Values)> Entries
    {
        get
        {
            lock (_entries)
                return _entries.ToList();
        }
    }

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull =>
        null;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) =>
        logLevel != LogLevel.None;

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Dictionary<string, object?> values = new(StringComparer.Ordinal);
        if (state is IEnumerable<KeyValuePair<string, object?>> pairs)
        {
            foreach (KeyValuePair<string, object?> pair in pairs)
                values[pair.Key] = pair.Value;
        }

        lock (_entries)
            _entries.Add((eventId, values));
    }
}
