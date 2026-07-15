// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Log.Storage.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Provides the source-generated, allocation-free logging messages emitted by the file-backed <see cref="IRateCache" />
/// implementations when a best-effort storage failure is swallowed.
/// </summary>
internal static partial class Log
{
    /// <summary>
    /// Logs that a file-cache storage operation failed and was swallowed under the cache's best-effort contract,
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
        EventId = 4513,
        Level = LogLevel.Warning,
        Message = "File exchange-rate cache for provider '{provider}' degraded: a storage {operation} failed and was swallowed as best-effort; {suppressedSinceLastWarning} further failure(s) suppressed since the previous warning")]
    public static partial void FileStorageFailureSwallowed(ILogger logger, string provider, string operation, int suppressedSinceLastWarning, Exception exception);

    /// <summary>
    /// Logs that a cache file could not be deserialized and was treated as empty under the cache's
    /// malformed-file-is-empty contract, so a corrupt or incompatible file surfaces to operators instead of silently
    /// reading as a miss until it is rewritten.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="provider">The provider whose cache file is corrupt.</param>
    /// <param name="path">The full path of the corrupt cache file.</param>
    /// <param name="exception">The swallowed deserialization exception.</param>
    [LoggerMessage(
        EventId = 4514,
        Level = LogLevel.Warning,
        Message = "File exchange-rate cache for provider '{provider}' treated corrupt cache file '{path}' as empty; it will be repaired by the next successful write")]
    public static partial void CacheFileCorrupt(ILogger logger, string provider, string path, Exception exception);

    /// <summary>
    /// Logs that a best-effort cleanup step (removing an emptied pair directory or an orphaned temporary file) failed
    /// and was swallowed so cleanup never masks the primary operation.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="provider">The provider whose cache cleanup was skipped.</param>
    /// <param name="path">The directory or file the cleanup step could not remove.</param>
    /// <param name="exception">The swallowed cleanup exception.</param>
    [LoggerMessage(
        EventId = 4515,
        Level = LogLevel.Debug,
        Message = "File exchange-rate cache for provider '{provider}' skipped best-effort cleanup of '{path}'")]
    public static partial void CleanupFailureSwallowed(ILogger logger, string provider, string path, Exception exception);
}
