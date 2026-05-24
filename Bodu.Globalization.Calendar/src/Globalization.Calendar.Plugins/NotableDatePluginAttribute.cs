// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDatePluginAttribute.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Plugins;

/// <summary>
/// Declares the entry-point type exposed by an external notable-date plugin assembly. Exactly one such attribute is
/// required on every plugin assembly; <see cref="ExternalPluginLoader" /> reads this attribute to identify the plugin
/// type and does not scan for other types or attributes in the assembly.
/// </summary>
/// <remarks>
/// <para>
/// The referenced <see cref="PluginType" /> must implement <see cref="INotableDatePlugin" /> together with at least one
/// of <see cref="INotableDateRulePlugin" /> or <see cref="INotableDateAlgorithmPlugin" />, and must expose a public
/// parameterless constructor so the loader can activate it with <see cref="Activator.CreateInstance(Type)" />.
/// </para>
/// <para>
/// Example usage in an external plugin assembly:
/// </para>
/// <code>
///<![CDATA[
/// [assembly: NotableDatePlugin(typeof(AcmeHolidayPlugin))]
///]]>
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
public sealed class NotableDatePluginAttribute
    : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDatePluginAttribute" /> class.
    /// </summary>
    /// <param name="pluginType">
    /// The plugin entry-point type. Must not be <see langword="null" />. The loader verifies that the type implements
    /// <see cref="INotableDatePlugin" />; activation failures are surfaced as <see cref="PluginActivationException" />.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="pluginType" /> is <see langword="null" />.
    /// </exception>
    public NotableDatePluginAttribute(Type pluginType)
    {
        PluginType = pluginType ?? throw new ArgumentNullException(nameof(pluginType));
    }

    /// <summary>
    /// Gets the plugin entry-point type declared on the assembly.
    /// </summary>
    /// <returns>
    /// The CLR <see cref="Type" /> the loader instantiates when activating the plugin. Never <see langword="null" />.
    /// </returns>
    public Type PluginType { get; }
}
