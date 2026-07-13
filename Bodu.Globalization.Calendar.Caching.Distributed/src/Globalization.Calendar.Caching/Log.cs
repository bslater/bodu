// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Log.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// Provides the source-generated, allocation-free logging message emitted by <see cref="DistributedNotableDateCache" />
/// when a best-effort storage failure is swallowed.
/// </summary>
internal static partial class Log
{
    /// <summary>
    /// Logs that a distributed notable-date cache storage operation failed and was swallowed under the cache's
    /// best-effort contract, with the count of similar failures suppressed since the previous warning.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="operation">The storage operation that failed, such as <c>read</c> or <c>store</c>.</param>
    /// <param name="suppressedSinceLastWarning">
    /// The number of similar swallowed failures since the previous warning that were rate-limited away.
    /// </param>
    /// <param name="exception">The swallowed storage exception.</param>
    [LoggerMessage(
        EventId = 4621,
        Level = LogLevel.Warning,
        Message = "Distributed notable-date cache degraded: a storage {operation} failed and was swallowed as best-effort; {suppressedSinceLastWarning} further failure(s) suppressed since the previous warning")]
    public static partial void StorageFailureSwallowed(ILogger logger, string operation, int suppressedSinceLastWarning, Exception exception);
}
