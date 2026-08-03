// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotAPlugin.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

[assembly: Bodu.Globalization.Calendar.Plugins.NotableDatePlugin(typeof(Bodu.Globalization.Calendar.Plugins.TestPlugin.NotPlugin.NotAPlugin))]

namespace Bodu.Globalization.Calendar.Plugins.TestPlugin.NotPlugin;

/// <summary>
/// A fixture type declared as the assembly's plugin entry point without implementing
/// <see cref="Bodu.Globalization.Calendar.Plugins.INotableDatePlugin" />, so activation must reject it.
/// </summary>
public sealed class NotAPlugin
{
    /// <summary>
    /// Gets a placeholder value so the type has observable state.
    /// </summary>
    /// <value>The placeholder value.</value>
    public static string Description =>
        "This type is deliberately not a plugin.";
}
