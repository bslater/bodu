// ---------------------------------------------------------------------------------------------------------------
// <copyright file="INotableDateAlgorithmPlugin.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Globalization.Calendar.Plugins;

/// <summary>
/// A plugin that contributes one or more custom <see cref="INotableDateAlgorithm" /> implementations keyed by algorithm
/// key, which a host registers with a <see cref="NotableDateAlgorithmRegistry" />.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface on the plugin's entry-point type, declare that type with an assembly-level
/// <see cref="NotableDatePluginAttribute" />, and return each algorithm under the key its referencing rules use. A host
/// activates the plugin through <see cref="NotableDatePluginLoader" /> and registers the algorithms with
/// <see cref="NotableDatePluginLoader.RegisterAlgorithms" />.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // The plugin entry point, declared at assembly level.
/// [assembly: NotableDatePlugin(typeof(ContosoCalendarPlugin))]
///
/// public sealed class ContosoCalendarPlugin : INotableDateAlgorithmPlugin
/// {
///     public string Name => "Contoso Calendar";
///     public Version Version => new(1, 0, 0);
///
///     public IEnumerable<KeyValuePair<string, INotableDateAlgorithm>> GetAlgorithms()
///     {
///         yield return new("contoso.march-equinox", new MarchEquinoxAlgorithm());
///     }
/// }
///]]>
/// </code>
/// </example>
/// <seealso cref="NotableDatePluginAttribute" />
/// <seealso cref="NotableDatePluginLoader" />
/// <seealso href="../guides/calendar/algorithms.html">Date calculation algorithms (guide)</seealso>
public interface INotableDateAlgorithmPlugin : INotableDatePlugin
{
    /// <summary>
    /// Gets the algorithms the plugin contributes, keyed by the algorithm key a rule references.
    /// </summary>
    /// <returns>The contributed key/algorithm pairs.</returns>
    IEnumerable<KeyValuePair<string, INotableDateAlgorithm>> GetAlgorithms();
}
