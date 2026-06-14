// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Log.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Provides the source-generated, allocation-free logging messages emitted while loading notable-date resources and
/// reloading them at runtime.
/// </summary>
/// <remarks>
/// The messages are produced by the <see cref="LoggerMessageAttribute" /> source generator, so a disabled or
/// <see cref="Microsoft.Extensions.Logging.Abstractions.NullLogger" /> logger short-circuits before any argument is
/// formatted.
/// </remarks>
internal static partial class Log
{
    /// <summary>
    /// Logs that a notable-date resource was loaded and validated.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="resourceId">The identifier of the loaded resource.</param>
    /// <param name="definitionCount">The number of notable-date definitions in the resource.</param>
    /// <param name="policyCount">The number of adjustment policies in the resource.</param>
    [LoggerMessage(EventId = 3001, Level = LogLevel.Debug, Message = "Loaded notable-date resource '{resourceId}' with {definitionCount} definition(s) and {policyCount} adjustment policy(ies)")]
    public static partial void ResourceLoaded(ILogger logger, string resourceId, int definitionCount, int policyCount);

    /// <summary>
    /// Logs that a notable-date document failed validation and the load will be rejected.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="errorCount">The number of error-severity diagnostics produced.</param>
    [LoggerMessage(EventId = 3002, Level = LogLevel.Warning, Message = "Notable-date document validation failed with {errorCount} error diagnostic(s)")]
    public static partial void ResourceValidationFailed(ILogger logger, int errorCount);

    /// <summary>
    /// Logs that the resource currently in effect was replaced at runtime.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="resourceId">The identifier of the resource that becomes current.</param>
    [LoggerMessage(EventId = 3003, Level = LogLevel.Information, Message = "Reloaded notable-date resource; resource '{resourceId}' is now in effect")]
    public static partial void ResourceReloaded(ILogger logger, string resourceId);

    /// <summary>
    /// Logs that a reloadable service rebuilt its resolution state after detecting a reloaded resource.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="resourceId">The identifier of the resource the resolution state was rebuilt from.</param>
    [LoggerMessage(EventId = 3004, Level = LogLevel.Debug, Message = "Rebuilt notable-date resolution state from reloaded resource '{resourceId}'")]
    public static partial void ResolutionStateRebuilt(ILogger logger, string resourceId);
}
