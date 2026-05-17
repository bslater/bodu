// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IPluginTrustPolicy.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Plugins;

/// <summary>
/// Gates whether a candidate plugin assembly may be loaded. The host passes every prospective plugin through a trust
/// policy before <see cref="ExternalPluginLoader" /> materializes any code from the assembly.
/// </summary>
/// <remarks>
/// <para>
/// Built-in policies cover the common patterns: <see cref="FileHashPluginTrustPolicy" /> pins a SHA-256 hash for each
/// trusted assembly, <see cref="StrongNamePluginTrustPolicy" /> allows assemblies whose public-key token appears in an
/// allowlist, <see cref="DelegatingPluginTrustPolicy" /> turns an arbitrary predicate into a policy, and
/// <see cref="CompositePluginTrustPolicy" /> requires every composed child policy to return trusted. For development
/// and unit tests an <see cref="AllowAllPluginTrustPolicy" /> is provided; it is <em>not</em> suitable for production.
/// </para>
/// <para>
/// Trust policies are expected to be fast and side-effect free — they execute once per
/// <see cref="ExternalPluginLoader.Load" /> call and must not themselves load or inspect the plugin beyond what is
/// already present on the supplied <see cref="PluginTrustContext" />.
/// </para>
/// </remarks>
public interface IPluginTrustPolicy
{
    /// <summary>
    /// Evaluates the candidate plugin and returns whether it is trusted.
    /// </summary>
    /// <param name="context">The context describing the candidate. Never <see langword="default" />.</param>
    /// <returns>A <see cref="PluginTrustResult" /> indicating the decision and an optional reason.</returns>
    PluginTrustResult Evaluate(PluginTrustContext context);
}
