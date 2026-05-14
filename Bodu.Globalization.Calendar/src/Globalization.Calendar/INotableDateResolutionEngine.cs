// ---------------------------------------------------------------------------------------------------------------
// <copyright file="INotableDateResolutionEngine.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Resolves notable dates for an internal chronological request.
/// </summary>
internal interface INotableDateResolutionEngine
{
    /// <summary>
    /// Resolves notable dates for the specified request.
    /// </summary>
    /// <param name="request">The resolution request.</param>
    /// <returns>The resolved notable dates.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request" /> is <see langword="null" />.</exception>
    IReadOnlyList<NotableDate> Resolve(NotableDateResolutionRequest request);
}
