// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentTriggerContext.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Provides an <see cref="IAdjustmentTriggerHandler" /> with the information needed to decide whether a policy fires:
/// the calculated occurrence date, the requesting territory, the candidate policy, and access to the surrounding
/// resolution context.
/// </summary>
/// <remarks>
/// Trigger evaluation runs during candidate gathering, before placement, so no occupied-day probe is exposed: the set
/// of claimed non-working days is still being assembled and would be unreliable. Handlers that need to reason about
/// other occurrences should resolve them through <see cref="ResolutionContext" />.
/// </remarks>
public sealed class AdjustmentTriggerContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AdjustmentTriggerContext" /> class.
    /// </summary>
    /// <param name="baseDate">The calculated (actual) occurrence date.</param>
    /// <param name="territory">The territory code the resolution was requested for.</param>
    /// <param name="policy">The adjustment policy whose custom trigger is being evaluated.</param>
    /// <param name="resolutionContext">The resolution context used to resolve referenced rules.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="territory" />, <paramref name="policy" />, or <paramref name="resolutionContext" /> is
    /// <see langword="null" />.
    /// </exception>
    public AdjustmentTriggerContext(
        DateOnly baseDate,
        string territory,
        AdjustmentPolicy policy,
        StrategyResolutionContext resolutionContext)
    {
        ThrowHelper.ThrowIfNull(territory);
        ThrowHelper.ThrowIfNull(policy);
        ThrowHelper.ThrowIfNull(resolutionContext);

        BaseDate = baseDate;
        Territory = territory;
        Policy = policy;
        ResolutionContext = resolutionContext;
    }

    /// <summary>
    /// Gets the calculated (actual) occurrence date being evaluated.
    /// </summary>
    /// <value>The calculated date.</value>
    public DateOnly BaseDate { get; }

    /// <summary>
    /// Gets the territory code the resolution was requested for.
    /// </summary>
    /// <value>The requested territory code.</value>
    public string Territory { get; }

    /// <summary>
    /// Gets the adjustment policy whose custom trigger is being evaluated.
    /// </summary>
    /// <value>The candidate <see cref="AdjustmentPolicy" />.</value>
    public AdjustmentPolicy Policy { get; }

    /// <summary>
    /// Gets the resolution context the handler can use to resolve referenced rules for the same year.
    /// </summary>
    /// <value>The <see cref="StrategyResolutionContext" />.</value>
    public StrategyResolutionContext ResolutionContext { get; }

    /// <summary>
    /// Gets the author-supplied parameters declared by the policy.
    /// </summary>
    /// <value>The policy's <see cref="AdjustmentPolicy.HandlerParameters" />; empty when none are declared.</value>
    public IReadOnlyDictionary<string, string> Parameters =>
        Policy.HandlerParameters;
}
