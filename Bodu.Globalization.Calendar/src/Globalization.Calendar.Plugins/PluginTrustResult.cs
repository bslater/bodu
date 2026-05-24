// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PluginTrustResult.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Plugins;

/// <summary>
/// Represents the outcome of an <see cref="IPluginTrustPolicy" /> evaluation.
/// </summary>
/// <param name="Trusted">
/// <see langword="true" /> when the plugin is trusted and may be loaded; otherwise <see langword="false" />.
/// </param>
/// <param name="Reason">
/// Optional human-readable reason. When <paramref name="Trusted" /> is <see langword="false" /> the loader surfaces
/// this reason on the resulting <see cref="PluginNotTrustedException" />.
/// </param>
/// <remarks>
/// <para>
/// The result is the single value an <see cref="IPluginTrustPolicy.Evaluate(PluginTrustContext)" /> implementation
/// returns. Trusted decisions may omit a reason; rejection decisions should populate <see cref="Reason" /> so the
/// resulting <see cref="PluginNotTrustedException" /> carries a useful diagnostic message back to the host.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// public PluginTrustResult Evaluate(PluginTrustContext context)
/// {
///     if (allowedHashes.TryGetValue(context.FileName, out var expectedHash)
///         && string.Equals(context.Sha256, expectedHash, StringComparison.OrdinalIgnoreCase))
///     {
///         return new PluginTrustResult(Trusted: true, Reason: null);
///     }
///
///     return new PluginTrustResult(
///         Trusted: false,
///         Reason:  $"SHA-256 mismatch for '{context.FileName}'.");
/// }
///]]>
/// </example>
public readonly record struct PluginTrustResult(
    bool Trusted,
    string? Reason);
