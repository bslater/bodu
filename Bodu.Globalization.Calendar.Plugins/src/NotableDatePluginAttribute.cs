// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDatePluginAttribute.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Plugins;

/// <summary>
/// Declares, at assembly level, the entry-point type the plugin loader activates to obtain the assembly's
/// <see cref="INotableDatePlugin" />.
/// </summary>
/// <remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Declared once at assembly level (for example in the plugin project's AssemblyInfo.cs or any source file).
/// [assembly: NotableDatePlugin(typeof(ContosoCalendarPlugin))]
///
/// // NotableDatePluginLoader reads this attribute to locate and activate the entry-point type.
///]]>
/// </code>
/// </example>
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class NotableDatePluginAttribute
    : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDatePluginAttribute" /> class.
    /// </summary>
    /// <param name="pluginType">The type that implements <see cref="INotableDatePlugin" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="pluginType" /> is <see langword="null" />.</exception>
    public NotableDatePluginAttribute(Type pluginType)
    {
        ThrowHelper.ThrowIfNull(pluginType);

        PluginType = pluginType;
    }

    /// <summary>
    /// Gets the type that implements <see cref="INotableDatePlugin" />.
    /// </summary>
    /// <value>The plugin entry-point type.</value>
    public Type PluginType { get; }
}
