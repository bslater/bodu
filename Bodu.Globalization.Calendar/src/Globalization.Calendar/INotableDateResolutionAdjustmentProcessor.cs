// ---------------------------------------------------------------------------------------------------------------
// <copyright file="INotableDateResolutionAdjustmentProcessor.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Applies observance adjustments to a chronological notable-date resolution window.
/// </summary>
internal interface INotableDateResolutionAdjustmentProcessor
{
    /// <summary>
    /// Applies adjustments for the specified base occurrences.
    /// </summary>
    /// <param name="window">The resolution window containing the known base occurrences.</param>
    /// <param name="request">The originating resolution request.</param>
    /// <param name="occurrences">The base occurrences to evaluate for adjustment.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="window" />, <paramref name="request" />, or <paramref name="occurrences" /> is
    /// <see langword="null" />.
    /// </exception>
    void ApplyAdjustments(
        NotableDateResolutionWindow window,
        NotableDateResolutionRequest request,
        IReadOnlyList<ResolvedNotableDateOccurrence> occurrences);
}
