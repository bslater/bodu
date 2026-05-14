// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateResolutionEngine.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Coordinates notable-date occurrence materialization for a requested chronological window.
/// </summary>
/// <remarks>
/// <para>
/// This engine is the integration seam between the rule occurrence resolver and the chronological resolution window.
/// </para>
/// <para>
/// When adjustment processing is enabled, the engine materializes a small padded source window around the requested output
/// window. This allows adjusted dates whose original anchor lies just outside the requested range to still be projected into
/// the caller's observed-date output.
/// </para>
/// </remarks>
internal sealed class NotableDateResolutionEngine
    : INotableDateResolutionEngine
{
    private const int AdjustmentMaterializationPaddingDays = 14;

    private readonly INotableDateRuleOccurrenceResolver _occurrenceResolver;
    private readonly INotableDateResolutionAdjustmentProcessor? _adjustmentProcessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDateResolutionEngine" /> class.
    /// </summary>
    /// <param name="occurrenceResolver">The resolver used to materialize base rule occurrences.</param>
    /// <param name="adjustmentProcessor">The optional processor used to apply observance adjustments.</param>
    /// <exception cref="ArgumentNullException"><paramref name="occurrenceResolver" /> is <see langword="null" />.</exception>
    public NotableDateResolutionEngine(
        INotableDateRuleOccurrenceResolver occurrenceResolver,
        INotableDateResolutionAdjustmentProcessor? adjustmentProcessor = null)
    {
        ArgumentNullException.ThrowIfNull(occurrenceResolver);

        this._occurrenceResolver = occurrenceResolver;
        this._adjustmentProcessor = adjustmentProcessor;
    }

    /// <inheritdoc />
    public IReadOnlyList<NotableDate> Resolve(NotableDateResolutionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        NotableDateResolutionWindow window = new(request.StartDate, request.EndDate);
        NotableDateResolutionRequest materializationRequest = CreateMaterializationRequest(request);
        IReadOnlyList<ResolvedNotableDateOccurrence> occurrences = _occurrenceResolver.ResolveOccurrences(materializationRequest);

        foreach (ResolvedNotableDateOccurrence occurrence in occurrences)
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

        _adjustmentProcessor?.ApplyAdjustments(window, request, occurrences);

        return window.OutputDates;
    }

    /// <summary>
    /// Creates the occurrence materialization request used by the engine.
    /// </summary>
    /// <param name="request">The caller's original request.</param>
    /// <returns>The materialization request.</returns>
    private NotableDateResolutionRequest CreateMaterializationRequest(NotableDateResolutionRequest request)
    {
        if (_adjustmentProcessor is null)
            return request;

        DateTime startDate = AddDaysWithinRange(request.StartDate, -AdjustmentMaterializationPaddingDays);
        DateTime endDate = AddDaysWithinRange(request.EndDate, AdjustmentMaterializationPaddingDays);

        return new NotableDateResolutionRequest(
            startDate,
            endDate,
            request.Projection,
            request.TerritoryCode,
            request.CalendarType,
            request.Filter);
    }

    /// <summary>
    /// Determines whether a resolved occurrence should be emitted for the original request projection.
    /// </summary>
    /// <param name="occurrence">The resolved occurrence.</param>
    /// <param name="request">The original resolution request.</param>
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
    /// Adds days to a date while preserving <see cref="DateTime" /> bounds.
    /// </summary>
    /// <param name="date">The source date.</param>
    /// <param name="days">The number of days to add.</param>
    /// <returns>The adjusted date, clamped to the supported <see cref="DateTime" /> range.</returns>
    private static DateTime AddDaysWithinRange(DateTime date, int days)
    {
        DateTime value = date.Date;

        if (days < 0 && value <= DateTime.MinValue.AddDays(-days))
            return DateTime.MinValue.Date;

        if (days > 0 && value >= DateTime.MaxValue.AddDays(-days))
            return DateTime.MaxValue.Date;

        return value.AddDays(days);
    }

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
