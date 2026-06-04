// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelegatingPluginTrustPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2.Plugins;

/// <summary>
/// A trust policy that delegates the decision to a caller-supplied function, suitable for runtime state, configuration,
/// or remote attestation.
/// </summary>
public sealed class DelegatingPluginTrustPolicy : IPluginTrustPolicy
{
    /// <summary>
    /// The decision function.
    /// </summary>
    private readonly Func<PluginTrustContext, PluginTrustResult> _decide;

    /// <summary>
    /// Initializes a new instance of the <see cref="DelegatingPluginTrustPolicy" /> class.
    /// </summary>
    /// <param name="decide">The function that decides trust for a candidate.</param>
    /// <exception cref="ArgumentNullException"><paramref name="decide" /> is <see langword="null" />.</exception>
    public DelegatingPluginTrustPolicy(Func<PluginTrustContext, PluginTrustResult> decide)
    {
        ThrowHelper.ThrowIfNull(decide);

        this._decide = decide;
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"><paramref name="context" /> is <see langword="null" />.</exception>
    public PluginTrustResult Evaluate(PluginTrustContext context)
    {
        ThrowHelper.ThrowIfNull(context);

        return this._decide(context);
    }
}
