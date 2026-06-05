// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Plugin4.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar.Plugins;

[assembly: NotableDatePlugin(typeof(Bodu.Globalization.Calendar.Plugin4.TestAssembly.NotAPlugin))]

namespace Bodu.Globalization.Calendar.Plugin4.TestAssembly;

/// <summary>
/// Test-only type deliberately declared as the plugin entry-point via <c>NotableDatePluginAttribute</c> but does not
/// implement <see cref="INotableDatePlugin" />. Exercises the attribute-present-but-wrong-type branch in
/// <see cref="ExternalPluginLoader" />, which surfaces as a <see cref="PluginMissingAttributeException" />.
/// </summary>
public sealed class NotAPlugin
{
}
