// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PluginNotTrustedException.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Plugins;

/// <summary>
/// Thrown by <see cref="ExternalPluginLoader" /> when the configured <see cref="IPluginTrustPolicy" /> rejects a candidate
/// plugin assembly before it is loaded into the process.
/// </summary>
public sealed class PluginNotTrustedException : NotableDatePluginException
{
	/// <summary>
	/// Initialises a new instance of the <see cref="PluginNotTrustedException" /> class.
	/// </summary>
	/// <param name="assemblyPath">The path of the rejected plugin.</param>
	/// <param name="reason">The trust policy's stated reason, or <see langword="null" /> if the policy did not supply one.</param>
	public PluginNotTrustedException(string assemblyPath, string? reason)
		: base($"Plugin assembly '{assemblyPath}' was rejected by the trust policy: {reason ?? "no reason supplied"}.")
	{
		AssemblyPath = assemblyPath;
		Reason = reason;
	}

	/// <summary>
	/// Gets the path of the rejected plugin assembly.
	/// </summary>
	/// <returns>The absolute filesystem path supplied at construction. Never <see langword="null" />.</returns>
	public string AssemblyPath { get; }

	/// <summary>
	/// Gets the reason surfaced by the trust policy, or <see langword="null" />.
	/// </summary>
	/// <returns>The trust policy's stated reason for rejection, or <see langword="null" /> when the policy did not supply one.</returns>
	public string? Reason { get; }
}
