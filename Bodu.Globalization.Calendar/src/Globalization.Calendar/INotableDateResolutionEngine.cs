// ---------------------------------------------------------------------------------------------------------------
// <copyright file="INotableDateResolutionEngine.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
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
