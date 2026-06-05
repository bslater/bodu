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
public interface INotableDateAlgorithmPlugin : INotableDatePlugin
{
    /// <summary>
    /// Gets the algorithms the plugin contributes, keyed by the algorithm key a rule references.
    /// </summary>
    /// <returns>The contributed key/algorithm pairs.</returns>
    IEnumerable<KeyValuePair<string, INotableDateAlgorithm>> GetAlgorithms();
}
