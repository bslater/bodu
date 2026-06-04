// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDatePluginAttribute.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2.Plugins;

/// <summary>
/// Declares, at assembly level, the entry-point type the plugin loader activates to obtain the assembly's
/// <see cref="INotableDatePlugin" />.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class NotableDatePluginAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDatePluginAttribute" /> class.
    /// </summary>
    /// <param name="pluginType">The type that implements <see cref="INotableDatePlugin" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="pluginType" /> is <see langword="null" />.</exception>
    public NotableDatePluginAttribute(Type pluginType)
    {
        ThrowHelper.ThrowIfNull(pluginType);

        this.PluginType = pluginType;
    }

    /// <summary>
    /// Gets the type that implements <see cref="INotableDatePlugin" />.
    /// </summary>
    /// <returns>The plugin entry-point type.</returns>
    public Type PluginType { get; }
}
