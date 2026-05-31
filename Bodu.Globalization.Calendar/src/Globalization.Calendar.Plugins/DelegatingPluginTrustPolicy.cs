// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelegatingPluginTrustPolicy.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Plugins;

/// <summary>
/// Adapts an arbitrary callback into an <see cref="IPluginTrustPolicy" />. Useful when trust logic depends on runtime
/// state (configuration, remote attestation, logged-in user) that does not fit the built-in allowlist policies.
/// </summary>
public sealed class DelegatingPluginTrustPolicy
    : IPluginTrustPolicy
{
    /// <summary>
    /// The delegate invoked to evaluate each candidate plugin context.
    /// </summary>
    private readonly Func<PluginTrustContext, PluginTrustResult> _evaluator;

    /// <summary>
    /// Initializes a new instance of the <see cref="DelegatingPluginTrustPolicy" /> class.
    /// </summary>
    /// <param name="evaluator">
    /// The callback invoked to evaluate each candidate. Must not be <see langword="null" />.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="evaluator" /> is <see langword="null" />.
    /// </exception>
    public DelegatingPluginTrustPolicy(Func<PluginTrustContext, PluginTrustResult> evaluator)
    {
        _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
    }

    /// <inheritdoc />
    public PluginTrustResult Evaluate(PluginTrustContext context) => _evaluator(context);
}
