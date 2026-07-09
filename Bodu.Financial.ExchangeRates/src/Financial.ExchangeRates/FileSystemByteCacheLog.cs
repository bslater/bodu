// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FileSystemByteCacheLog.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

using Microsoft.Extensions.Logging;

/// <summary>
/// Provides the source-generated, allocation-free logging messages emitted by <see cref="FileSystemByteCache{TKey}" />
/// when a best-effort file-system failure is swallowed.
/// </summary>
/// <remarks>
/// The messages are produced by the <see cref="LoggerMessageAttribute" /> source generator, so a disabled or
/// <see cref="Microsoft.Extensions.Logging.Abstractions.NullLogger" /> logger short-circuits before any argument is
/// formatted.
/// </remarks>
internal static partial class FileSystemByteCacheLog
{
    /// <summary>
    /// Logs that reading a cached response file failed and was treated as a cache miss under the cache's best-effort
    /// contract, so the download proceeds but the degradation surfaces to operators.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="path">The full path of the cache file the read failed on.</param>
    /// <param name="exception">The swallowed file-system exception.</param>
    [LoggerMessage(
        EventId = 4410,
        Level = LogLevel.Warning,
        Message = "Response byte cache read of '{path}' failed and was treated as a miss; the response will be re-downloaded")]
    public static partial void ReadFailureSwallowed(ILogger logger, string path, Exception exception);

    /// <summary>
    /// Logs that persisting a downloaded response to the cache failed and the write was skipped under the cache's
    /// best-effort contract, so rate retrieval succeeds but the response will be re-downloaded next time.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="path">The full path of the cache file the write failed on.</param>
    /// <param name="exception">The swallowed file-system exception.</param>
    [LoggerMessage(
        EventId = 4411,
        Level = LogLevel.Warning,
        Message = "Response byte cache write of '{path}' failed and was skipped; the response will be re-downloaded next time")]
    public static partial void StoreFailureSwallowed(ILogger logger, string path, Exception exception);
}
