// ---------------------------------------------------------------------------------------------------------------
// <copyright file="INotableDateCalculatorPlugin.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Plugins;

/// <summary>
/// Marks a notable-date plugin that contributes one or more named <see cref="INotableDateCalculator" /> registrations to the
/// host. A plugin that only contributes rule providers need not implement this interface.
/// </summary>
/// <remarks>
/// Each key/calculator pair returned from <see cref="GetCalculators" /> is registered on the host's
/// <see cref="INotableDateCalculatorRegistry" /> and becomes reachable from any rule whose <see cref="NotableDateRule.Strategy" />
/// is <see cref="DateResolutionStrategy.Calculator" /> and whose <see cref="NotableDateRule.CalculatorKey" /> matches the
/// registered key. Keys are compared case-insensitively; a plugin must not register two calculators under the same key.
/// </remarks>
public interface INotableDateCalculatorPlugin : INotableDatePlugin
{
	/// <summary>
	/// Returns the calculator registrations this plugin contributes.
	/// </summary>
	/// <returns>Zero or more key-keyed calculator pairs. Never <see langword="null" />.</returns>
	IEnumerable<KeyValuePair<string, INotableDateCalculator>> GetCalculators();
}
