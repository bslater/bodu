// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IAdjustmentHandlerRegistry.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;


/// <summary>
/// Provides lookup of registered <see cref="IAdjustmentHandler" /> instances by stable key.
/// </summary>
public interface IAdjustmentHandlerRegistry
{
	/// <summary>
	/// Attempts to retrieve the <see cref="IAdjustmentHandler" /> registered against the specified key.
	/// </summary>
	/// <param name="key">The handler key.</param>
	/// <param name="handler">When this method returns <see langword="true" />, contains the resolved handler.</param>
	/// <returns><see langword="true" /> if a handler is registered for <paramref name="key" />; otherwise <see langword="false" />.</returns>
	bool TryGet(string key, out IAdjustmentHandler handler);
}
