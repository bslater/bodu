// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Log.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Provides the source-generated, allocation-free logging messages emitted by the dependency-injection registrations.
/// </summary>
internal static partial class Log
{
    /// <summary>
    /// Logs that rebuilding the notable-date resource after an options change failed, leaving the previously loaded
    /// resource in effect.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="exception">The factory failure.</param>
    [LoggerMessage(EventId = 4001, Level = LogLevel.Error, Message = "Rebuilding the notable-date resource after an options change failed; the previous resource remains in effect")]
    public static partial void OptionsReloadFailed(ILogger logger, Exception exception);

    /// <summary>
    /// Logs that an options change was applied and the notable-date resource was reloaded.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="resourceId">The identifier of the newly loaded resource.</param>
    [LoggerMessage(EventId = 4002, Level = LogLevel.Information, Message = "Options change applied; notable-date resource '{resourceId}' reloaded")]
    public static partial void OptionsReloadApplied(ILogger logger, string resourceId);
}
