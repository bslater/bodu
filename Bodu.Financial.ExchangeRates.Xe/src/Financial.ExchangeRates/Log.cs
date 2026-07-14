// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Log.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

using Microsoft.Extensions.Logging;

/// <summary>
/// Provides the source-generated, allocation-free logging messages emitted by
/// <see cref="XeScrapingAuthTokenProvider" /> while scanning the XE.com bootstrap page for the charting credential.
/// </summary>
/// <remarks>
/// The messages are produced by the <see cref="LoggerMessageAttribute" /> source generator, so a disabled or
/// <see cref="Microsoft.Extensions.Logging.Abstractions.NullLogger" /> logger short-circuits before any argument is
/// formatted.
/// </remarks>
internal static partial class Log
{
    /// <summary>
    /// Logs that downloading a reconstructed script chunk failed and the chunk was skipped so the token scan can
    /// continue over the remaining chunks — expected for lazy chunks that no longer exist, but useful when diagnosing
    /// scraper drift after an XE.com site change.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="url">The chunk URL whose download failed.</param>
    /// <param name="exception">The swallowed request exception.</param>
    [LoggerMessage(
        EventId = 4420,
        Level = LogLevel.Debug,
        Message = "Skipped unreachable XE script chunk '{url}' while scanning for the charting token")]
    public static partial void TokenChunkSkipped(ILogger logger, Uri url, Exception exception);
}
