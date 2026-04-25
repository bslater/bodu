// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PluginActivationException.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Plugins;

/// <summary>
/// Thrown by <see cref="ExternalPluginLoader" /> when the declared plugin type cannot be instantiated — typically because the
/// type lacks a public parameterless constructor, or because the constructor threw.
/// </summary>
public sealed class PluginActivationException : NotableDatePluginException
{
	/// <summary>
	/// Initializes a new instance of the <see cref="PluginActivationException" /> class.
	/// </summary>
	/// <param name="assemblyPath">The path of the plugin assembly whose declared type could not be activated.</param>
	/// <param name="pluginType">The declared plugin type, if discoverable.</param>
	/// <param name="innerException">The underlying activation failure.</param>
	public PluginActivationException(string assemblyPath, Type? pluginType, Exception innerException)
		: base($"Failed to activate plugin type '{pluginType?.FullName ?? "<unknown>"}' from assembly '{assemblyPath}': {innerException.Message}", innerException)
	{
		AssemblyPath = assemblyPath;
		PluginType = pluginType;
	}

	/// <summary>
	/// Gets the path of the plugin assembly whose declared type could not be activated.
	/// </summary>
	public string AssemblyPath { get; }

	/// <summary>
	/// Gets the declared plugin type, or <see langword="null" /> if it could not be resolved.
	/// </summary>
	public Type? PluginType { get; }
}
