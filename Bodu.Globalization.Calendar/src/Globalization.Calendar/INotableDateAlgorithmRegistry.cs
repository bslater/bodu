// ---------------------------------------------------------------------------------------------------------------
// <copyright file="INotableDateAlgorithmRegistry.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Provides lookup of registered <see cref="INotableDateAlgorithm" /> implementations by stable string key.
/// </summary>
/// <remarks>
/// <para>
/// The registry decouples a <see cref="NotableDateRule" /> from a CLR <see cref="Type" />: rules reference algorithms by short
/// identifier (for example <c>"easter-sunday"</c> or <c>"lunar-new-year"</c>) and the registry resolves them to a concrete instance
/// supplied by dependency injection or test code. This makes definitions resilient to assembly renames and allows algorithms to
/// declare constructor dependencies.
/// </para>
/// </remarks>
public interface INotableDateAlgorithmRegistry
{
	/// <summary>
	/// Attempts to retrieve the <see cref="INotableDateAlgorithm" /> registered against the specified key.
	/// </summary>
	/// <param name="key">The registry key. Must not be <see langword="null" /> or whitespace.</param>
	/// <param name="algorithm">When this method returns <see langword="true" />, contains the resolved algorithm.</param>
	/// <returns><see langword="true" /> if a algorithm is registered for <paramref name="key" />; otherwise <see langword="false" />.</returns>
	bool TryGet(string key, out INotableDateAlgorithm algorithm);

	/// <summary>
	/// Indicates whether a algorithm is registered against the specified key.
	/// </summary>
	/// <param name="key">The registry key.</param>
	/// <returns><see langword="true" /> if a algorithm is registered; otherwise <see langword="false" />.</returns>
	bool Contains(string key);
}
