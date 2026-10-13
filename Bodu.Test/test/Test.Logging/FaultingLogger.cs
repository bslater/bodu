// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FaultingLogger.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Bodu.Test.Logging;

/// <summary>
/// An <see cref="ILogger" /> that throws <see cref="InvalidOperationException" /> whenever an entry with one chosen
/// event id is logged while <see cref="IsFaulting" /> is set, so a test can make a component fail part-way through
/// work that logs as it goes.
/// </summary>
/// <remarks>
/// Every level other than <see cref="LogLevel.None" /> is enabled, so a diagnostic logged at a low level such as
/// <see cref="LogLevel.Trace" /> still reaches <see cref="Log{TState}" />. Entries with any other event id are
/// discarded.
/// </remarks>
public sealed class FaultingLogger
    : ILogger
{
    /// <summary>The event id whose entries throw while faulting is enabled.</summary>
    private readonly int _faultingEventId;

    /// <summary>
    /// Initializes a new instance of the <see cref="FaultingLogger" /> class.
    /// </summary>
    /// <param name="faultingEventId">The event id whose entries throw while faulting is enabled.</param>
    public FaultingLogger(int faultingEventId)
    {
        _faultingEventId = faultingEventId;
    }

    /// <summary>
    /// Gets or sets a value indicating whether an entry with the chosen event id throws.
    /// </summary>
    /// <value><see langword="true" /> to throw (the initial state); <see langword="false" /> to discard the entry.</value>
    public bool IsFaulting { get; set; } = true;

    /// <summary>
    /// Gets the number of entries that have thrown.
    /// </summary>
    /// <value>The number of times <see cref="Log{TState}" /> has thrown.</value>
    public int FaultCount { get; private set; }

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull =>
        null;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) =>
        logLevel != LogLevel.None;

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">
    /// <see cref="IsFaulting" /> is set and <paramref name="eventId" /> is the chosen event id.
    /// </exception>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsFaulting || eventId.Id != _faultingEventId)
            return;

        FaultCount++;
        throw new InvalidOperationException($"Simulated logging failure for event {eventId.Id}.");
    }
}
