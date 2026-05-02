// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateResolutionEngine.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Coordinates notable-date occurrence materialisation for a requested chronological window.
/// </summary>
/// <remarks>
/// <para>
/// This engine is the integration seam between the rule occurrence resolver and the chronological resolution window.
/// This initial implementation materialises base occurrences only. Observance adjustments and dynamic window expansion
/// are intentionally handled by later refactoring slices.
/// </para>
/// </remarks>
internal sealed class NotableDateResolutionEngine : INotableDateResolutionEngine
{
    private readonly INotableDateRuleOccurrenceResolver occurrenceResolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDateResolutionEngine" /> class.
    /// </summary>
    /// <param name="occurrenceResolver">The resolver used to materialise base rule occurrences.</param>
    /// <exception cref="ArgumentNullException"><paramref name="occurrenceResolver" /> is <see langword="null" />.</exception>
    public NotableDateResolutionEngine(INotableDateRuleOccurrenceResolver occurrenceResolver)
    {
        ArgumentNullException.ThrowIfNull(occurrenceResolver);

        this.occurrenceResolver = occurrenceResolver;
    }

    /// <inheritdoc />
    public IReadOnlyList<NotableDate> Resolve(NotableDateResolutionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        NotableDateResolutionWindow window = new(request.StartDate, request.EndDate);

        foreach (ResolvedNotableDateOccurrence occurrence in occurrenceResolver.ResolveOccurrences(request))
        {
            if (ShouldEmit(occurrence, request))
            {
                window.AddBase(occurrence);
            }
            else
            {
                window.AddBlocker(occurrence.BaseDate);
            }
        }

        return window.OutputDates;
    }

    /// <summary>
    /// Determines whether a resolved occurrence should be emitted for the request projection.
    /// </summary>
    /// <param name="occurrence">The resolved occurrence.</param>
    /// <param name="request">The resolution request.</param>
    /// <returns><see langword="true" /> when the occurrence should be emitted; otherwise, <see langword="false" />.</returns>
    private static bool ShouldEmit(
        ResolvedNotableDateOccurrence occurrence,
        NotableDateResolutionRequest request) =>
        request.Projection switch
        {
            NotableDateResolutionProjection.AnchorDate =>
                occurrence.AnchorDate.Date >= request.StartDate &&
                occurrence.AnchorDate.Date <= request.EndDate,

            NotableDateResolutionProjection.ObservedDate =>
                Intersects(request.StartDate, request.EndDate, occurrence.BaseDate.Date, occurrence.BaseDate.EndDate),

            _ => throw new ArgumentOutOfRangeException(nameof(request), request.Projection, "The requested projection is not supported."),
        };

    /// <summary>
    /// Determines whether two inclusive date spans intersect.
    /// </summary>
    /// <param name="leftStart">The first span start.</param>
    /// <param name="leftEnd">The first span end.</param>
    /// <param name="rightStart">The second span start.</param>
    /// <param name="rightEnd">The second span end.</param>
    /// <returns><see langword="true" /> when the spans intersect.</returns>
    private static bool Intersects(
        DateTime leftStart,
        DateTime leftEnd,
        DateTime rightStart,
        DateTime rightEnd) =>
        rightStart.Date <= leftEnd.Date &&
        rightEnd.Date >= leftStart.Date;
}
