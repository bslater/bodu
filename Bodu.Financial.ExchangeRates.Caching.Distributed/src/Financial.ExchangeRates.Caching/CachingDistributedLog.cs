// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingDistributedLog.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Provides the source-generated, allocation-free logging messages emitted by <see cref="DistributedRateCache" /> when
/// a best-effort storage failure is swallowed.
/// </summary>
/// <remarks>
/// The messages are produced by the <see cref="LoggerMessageAttribute" /> source generator, so a disabled or
/// <see cref="Microsoft.Extensions.Logging.Abstractions.NullLogger" /> logger short-circuits before any argument is
/// formatted.
/// </remarks>
internal static partial class CachingDistributedLog
{
    /// <summary>
    /// Logs that a distributed-cache storage operation failed and was swallowed under the cache's best-effort contract,
    /// surfacing the degradation operators would otherwise not see, with the count of similar failures suppressed since
    /// the previous warning so a sustained outage does not flood the log.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="provider">The provider whose cache degraded.</param>
    /// <param name="operation">The storage operation that failed, such as <c>read</c> or <c>store</c>.</param>
    /// <param name="suppressedSinceLastWarning">
    /// The number of similar swallowed failures since the previous warning that were rate-limited away.
    /// </param>
    /// <param name="exception">The swallowed storage exception.</param>
    [LoggerMessage(
        EventId = 4530,
        Level = LogLevel.Warning,
        Message = "Distributed exchange-rate cache for provider '{provider}' degraded: a storage {operation} failed and was swallowed as best-effort; {suppressedSinceLastWarning} further failure(s) suppressed since the previous warning")]
    public static partial void StorageFailureSwallowed(ILogger logger, string provider, string operation, int suppressedSinceLastWarning, Exception exception);
}
