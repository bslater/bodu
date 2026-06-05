// ---------------------------------------------------------------------------------------------------------------
// <copyright file="INotableDateAlgorithmPlugin.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Plugins;

/// <summary>
/// Marks a notable-date plugin that contributes one or more named <see cref="INotableDateAlgorithm" /> registrations to
/// the host. A plugin that only contributes rule providers need not implement this interface.
/// </summary>
/// <remarks>
/// Each key/algorithm pair returned from <see cref="GetAlgorithms" /> is registered on the host's
/// <see cref="INotableDateAlgorithmRegistry" /> and becomes reachable from any rule whose
/// <see cref="NotableDateRule.Strategy" /> is <see cref="DateResolutionStrategy.Algorithm" /> and whose
/// <see cref="NotableDateRule.AlgorithmKey" /> matches the registered key. Keys are compared case-insensitively; a
/// plugin must not register two algorithms under the same key.
/// </remarks>
public interface INotableDateAlgorithmPlugin
    : INotableDatePlugin
{
    /// <summary>
    /// Returns the algorithm registrations this plugin contributes.
    /// </summary>
    /// <returns>Zero or more key-keyed algorithm pairs. Never <see langword="null" />.</returns>
    IEnumerable<KeyValuePair<string, INotableDateAlgorithm>> GetAlgorithms();
}
