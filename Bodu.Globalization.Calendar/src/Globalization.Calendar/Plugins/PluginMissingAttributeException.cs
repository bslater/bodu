// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PluginMissingAttributeException.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Plugins;

/// <summary>
/// Thrown by <see cref="ExternalPluginLoader" /> when a trusted plugin assembly loads successfully but does not expose a valid
/// <see cref="NotableDatePluginAttribute" /> naming a type that implements <see cref="INotableDatePlugin" />.
/// </summary>
public sealed class PluginMissingAttributeException : NotableDatePluginException
{
	/// <summary>
	/// Initializes a new instance of the <see cref="PluginMissingAttributeException" /> class.
	/// </summary>
	/// <param name="assemblyPath">The path of the plugin assembly that failed the check.</param>
	/// <param name="reason">Description of the specific fault (missing attribute, attribute PluginType does not implement the interface, etc.).</param>
	public PluginMissingAttributeException(string assemblyPath, string reason)
		: base($"Plugin assembly '{assemblyPath}' is missing a valid NotableDatePluginAttribute: {reason}.")
	{
		AssemblyPath = assemblyPath;
		Reason = reason;
	}

	/// <summary>
	/// Gets the path of the plugin assembly that failed the check.
	/// </summary>
	public string AssemblyPath { get; }

	/// <summary>
	/// Gets the description of the specific fault.
	/// </summary>
	public string Reason { get; }
}
