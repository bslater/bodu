// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CapturingLogger.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Bodu.Financial.ExchangeRates.Caching.Sqlite;

/// <summary>
/// An <see cref="ILogger" /> that records every logged entry so tests can assert on the level, event id, formatted
/// message, and exception of the cache's degradation warnings.
/// </summary>
internal sealed class CapturingLogger
    : ILogger
{
    /// <summary>The recorded entries, guarded by its own lock for concurrent writes.</summary>
    private readonly List<(LogLevel Level, EventId EventId, string Message, Exception? Exception)> _entries = new();

    /// <summary>
    /// Gets a snapshot of the entries recorded so far.
    /// </summary>
    /// <value>The recorded log entries in order.</value>
    public IReadOnlyList<(LogLevel Level, EventId EventId, string Message, Exception? Exception)> Entries
    {
        get
        {
            lock (_entries)
                return _entries.ToList();
        }
    }

    /// <inheritdoc />
    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull =>
        NullScope.Instance;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        lock (_entries)
            _entries.Add((logLevel, eventId, formatter(state, exception), exception));
    }

    /// <summary>
    /// A no-op scope returned by <see cref="BeginScope{TState}(TState)" />.
    /// </summary>
    private sealed class NullScope
        : IDisposable
    {
        /// <summary>The shared scope instance.</summary>
        public static readonly NullScope Instance = new();

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }
}
