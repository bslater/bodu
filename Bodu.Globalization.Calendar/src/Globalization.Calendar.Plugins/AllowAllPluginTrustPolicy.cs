// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AllowAllPluginTrustPolicy.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Plugins;

/// <summary>
/// A trust policy that marks every candidate plugin as trusted regardless of its identity or hash.
/// </summary>
/// <remarks>
/// <para>
/// <b>Dev and test only.</b> This policy performs no verification whatsoever. It exists so unit tests and local development
/// sessions can run without wiring up an allowlist every time. Production hosts — or any host that loads plugins from user-
/// controlled paths — must use <see cref="FileHashPluginTrustPolicy" />, <see cref="StrongNamePluginTrustPolicy" />, or a
/// bespoke <see cref="DelegatingPluginTrustPolicy" /> that enforces the relevant organisational policy.
/// </para>
/// <para>
/// The type is intentionally discoverable in IntelliSense so test code can reference it directly; its XML documentation makes
/// the production caveat explicit, and the type name is deliberately blunt to discourage its use outside of trusted contexts.
/// </para>
/// </remarks>
public sealed class AllowAllPluginTrustPolicy : IPluginTrustPolicy
{
	/// <inheritdoc />
	public PluginTrustResult Evaluate(PluginTrustContext context) => new(Trusted: true, Reason: null);
}
