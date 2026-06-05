// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PluginTrustResult.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Plugins;

/// <summary>
/// The outcome of a trust-policy evaluation.
/// </summary>
/// <param name="IsTrusted">Whether the candidate plugin assembly is trusted.</param>
/// <param name="Reason">An optional explanation, typically supplied when the candidate is rejected.</param>
public sealed record PluginTrustResult(bool IsTrusted, string? Reason)
{
    /// <summary>
    /// Creates a trusted result.
    /// </summary>
    /// <returns>A result indicating trust.</returns>
    public static PluginTrustResult Trusted() =>
        new(true, null);

    /// <summary>
    /// Creates a rejected result with an explanation.
    /// </summary>
    /// <param name="reason">The reason the candidate was rejected.</param>
    /// <returns>A result indicating rejection.</returns>
    public static PluginTrustResult Rejected(string reason) =>
        new(false, reason);
}
