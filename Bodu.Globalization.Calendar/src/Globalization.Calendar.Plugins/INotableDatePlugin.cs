// ---------------------------------------------------------------------------------------------------------------
// <copyright file="INotableDatePlugin.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Plugins;

/// <summary>
/// Defines the minimum surface every external notable-date plugin must expose: a human-readable name and a semantic version. A
/// concrete plugin implements this interface together with one or both of <see cref="INotableDateRulePlugin" /> and
/// <see cref="INotableDateCalculatorPlugin" /> depending on what it contributes.
/// </summary>
/// <remarks>
/// <para>
/// Plugins are loaded from external assemblies through <see cref="ExternalPluginLoader" />. The loader reflects over the
/// <see cref="NotableDatePluginAttribute" /> declared on the plugin assembly to discover the plugin type; it never scans for
/// arbitrary types or invokes methods outside the contracts defined here. See <see cref="IPluginTrustPolicy" /> for how a
/// consumer establishes trust in a plugin assembly before it is loaded.
/// </para>
/// <para>
/// Plugin authors keep <see cref="Name" /> and <see cref="Version" /> populated with stable values so the host can surface
/// diagnostic messages and reason about plugin identity across process restarts.
/// </para>
/// </remarks>
public interface INotableDatePlugin
{
	/// <summary>
	/// Gets a stable, human-readable name identifying the plugin. Used in diagnostic messages and for authoring trust policies.
	/// </summary>
	string Name { get; }

	/// <summary>
	/// Gets the semantic version of the plugin's contents. Authors increment this when rules or calculators change.
	/// </summary>
	Version Version { get; }
}
