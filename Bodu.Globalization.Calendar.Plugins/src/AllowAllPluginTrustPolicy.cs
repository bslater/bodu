// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AllowAllPluginTrustPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Plugins;

/// <summary>
/// A trust policy that trusts every candidate assembly.
/// </summary>
/// <remarks>
/// <para>
/// This policy performs no verification and is intended for development and testing only; production hosts should use a
/// hash- or strong-name-based policy.
/// </para>
/// </remarks>
public sealed class AllowAllPluginTrustPolicy
    : IPluginTrustPolicy
{
    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"><paramref name="context" /> is <see langword="null" />.</exception>
    public PluginTrustResult Evaluate(PluginTrustContext context)
    {
        ThrowHelper.ThrowIfNull(context);

        return PluginTrustResult.Trusted();
    }
}
