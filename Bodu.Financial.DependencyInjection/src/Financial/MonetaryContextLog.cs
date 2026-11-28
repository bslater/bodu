// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MonetaryContextLog.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Bodu.Financial;

/// <summary>
/// Provides the source-generated, allocation-free logging messages emitted by the monetary-context resolver.
/// </summary>
internal static partial class MonetaryContextLog
{
    /// <summary>
    /// Logs that rebuilding the monetary contexts after an options change failed, leaving the contexts built before in
    /// effect.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="exception">The validation failure.</param>
    [LoggerMessage(EventId = 1001, Level = LogLevel.Error, Message = "Rebuilding the monetary contexts after an options change failed; the previous contexts remain in effect")]
    public static partial void ReloadFailed(ILogger logger, Exception exception);

    /// <summary>
    /// Logs that an options change was applied and the monetary contexts were rebuilt.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="namedCount">The number of named contexts the options now configure.</param>
    [LoggerMessage(EventId = 1002, Level = LogLevel.Information, Message = "Options change applied; the monetary contexts were rebuilt with {namedCount} named contexts configured")]
    public static partial void ReloadApplied(ILogger logger, int namedCount);
}
