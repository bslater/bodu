// ---------------------------------------------------------------------------------------------------------------
// <copyright file="INotableDateCalculatorRegistry.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Provides lookup of registered <see cref="INotableDateCalculator" /> implementations by stable string key.
/// </summary>
/// <remarks>
/// <para>
/// The registry decouples a <see cref="NotableDateRule" /> from a CLR <see cref="Type" />: rules reference calculators by short
/// identifier (for example <c>"easter-sunday"</c> or <c>"lunar-new-year"</c>) and the registry resolves them to a concrete instance
/// supplied by dependency injection or test code. This makes definitions resilient to assembly renames and allows calculators to
/// declare constructor dependencies.
/// </para>
/// </remarks>
public interface INotableDateCalculatorRegistry
{
	/// <summary>
	/// Attempts to retrieve the <see cref="INotableDateCalculator" /> registered against the specified key.
	/// </summary>
	/// <param name="key">The registry key. Must not be <see langword="null" /> or whitespace.</param>
	/// <param name="calculator">When this method returns <see langword="true" />, contains the resolved calculator.</param>
	/// <returns><see langword="true" /> if a calculator is registered for <paramref name="key" />; otherwise <see langword="false" />.</returns>
	bool TryGet(string key, out INotableDateCalculator calculator);

	/// <summary>
	/// Indicates whether a calculator is registered against the specified key.
	/// </summary>
	/// <param name="key">The registry key.</param>
	/// <returns><see langword="true" /> if a calculator is registered; otherwise <see langword="false" />.</returns>
	bool Contains(string key);
}
