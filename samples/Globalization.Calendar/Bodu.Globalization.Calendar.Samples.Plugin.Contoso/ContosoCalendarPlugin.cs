// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ContosoCalendarPlugin.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar.Algorithms;
using Bodu.Globalization.Calendar.Plugins;

// The assembly-level attribute is the plugin's entry point: it is what the loader looks for, so a
// plugin assembly needs no naming convention, no manifest file, and no reflection over every type.
[assembly: NotableDatePlugin(typeof(Bodu.Globalization.Calendar.Samples.Plugin.Contoso.ContosoCalendarPlugin))]

namespace Bodu.Globalization.Calendar.Samples.Plugin.Contoso;

/// <summary>
/// A plugin assembly contributing one date-calculation algorithm. This project is deliberately
/// separate from the host: it is built to its own DLL, which the host discovers and loads by path,
/// exactly as a third-party plugin would be.
/// </summary>
/// <remarks>
/// The plugin depends only on the contract packages — <c>Bodu.Globalization.Calendar</c> for
/// <see cref="INotableDateAlgorithm" /> and <c>Bodu.Globalization.Calendar.Plugins</c> for this
/// interface and the attribute. It has no reference to the host application, and the host has no
/// compile-time knowledge of the type you are reading.
/// </remarks>
public sealed class ContosoCalendarPlugin
    : INotableDateAlgorithmPlugin
{
    /// <summary>Gets the display name the host reports when it loads the plugin.</summary>
    public string Name => "Contoso Calendar";

    /// <summary>Gets the plugin version, so a host can log or gate on it.</summary>
    public Version Version => new(1, 0, 0);

    /// <summary>
    /// Gets the algorithms this plugin contributes, keyed by the key a rule references.
    /// </summary>
    /// <returns>The contributed key/algorithm pairs.</returns>
    /// <remarks>
    /// The key is the contract between the plugin and the rule documents: a rule says
    /// <c>algorithm="contoso.founding-day"</c> and the registry resolves it to this instance. Keys are
    /// namespaced by convention so two plugins cannot collide.
    /// </remarks>
    public IEnumerable<KeyValuePair<string, INotableDateAlgorithm>> GetAlgorithms()
    {
        yield return new KeyValuePair<string, INotableDateAlgorithm>(
            "contoso.founding-day", new FoundingDayAlgorithm());
    }
}
