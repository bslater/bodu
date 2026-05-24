// ---------------------------------------------------------------------------------------------------------------
// <copyright file="INotableDateRuleOccurrenceResolver.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Resolves notable-date rule occurrences for an internal chronological request.
/// </summary>
internal interface INotableDateRuleOccurrenceResolver
{
    /// <summary>
    /// Resolves base notable-date occurrences for the specified request.
    /// </summary>
    /// <param name="request">The resolution request.</param>
    /// <returns>The resolved base occurrences.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request" /> is <see langword="null" />.</exception>
    IReadOnlyList<ResolvedNotableDateOccurrence> ResolveOccurrences(NotableDateResolutionRequest request);
}