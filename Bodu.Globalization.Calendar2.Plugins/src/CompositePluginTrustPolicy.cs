// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompositePluginTrustPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2.Plugins;

/// <summary>
/// A trust policy that trusts a candidate only when every composed policy trusts it; the first rejection short-circuits
/// and its reason is surfaced.
/// </summary>
public sealed class CompositePluginTrustPolicy : IPluginTrustPolicy
{
    /// <summary>
    /// The composed policies, all of which must trust the candidate.
    /// </summary>
    private readonly IPluginTrustPolicy[] _policies;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositePluginTrustPolicy" /> class.
    /// </summary>
    /// <param name="policies">The policies to compose under conjunction.</param>
    /// <exception cref="ArgumentNullException"><paramref name="policies" /> is <see langword="null" />.</exception>
    public CompositePluginTrustPolicy(params IPluginTrustPolicy[] policies)
    {
        ThrowHelper.ThrowIfNull(policies);

        this._policies = policies.ToArray();
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"><paramref name="context" /> is <see langword="null" />.</exception>
    public PluginTrustResult Evaluate(PluginTrustContext context)
    {
        ThrowHelper.ThrowIfNull(context);

        foreach (IPluginTrustPolicy policy in this._policies)
        {
            PluginTrustResult result = policy.Evaluate(context);
            if (!result.IsTrusted)
                return result;
        }

        return PluginTrustResult.Trusted();
    }
}
