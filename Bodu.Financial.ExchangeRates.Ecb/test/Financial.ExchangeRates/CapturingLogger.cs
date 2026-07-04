// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CapturingLogger.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// A minimal <see cref="ILogger" /> that records the level, event id, and rendered message of every entry, used to
/// assert that the provider emits — or deliberately does not emit — the expected diagnostics.
/// </summary>
internal sealed class CapturingLogger
    : ILogger
{
    /// <summary>
    /// Gets the recorded entries in emission order.
    /// </summary>
    public List<(LogLevel Level, EventId EventId, string Message)> Entries { get; } = new();

    /// <inheritdoc />
    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull =>
        NullScope.Instance;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) =>
        logLevel != LogLevel.None;

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        Entries.Add((logLevel, eventId, formatter(state, exception)));
    }

    /// <summary>
    /// A no-op scope returned by <see cref="BeginScope{TState}(TState)" />.
    /// </summary>
    private sealed class NullScope
        : IDisposable
    {
        /// <summary>
        /// Gets the shared instance.
        /// </summary>
        public static NullScope Instance { get; } = new();

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }
}
