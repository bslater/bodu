// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WorkingDayOffsetFromRuleStrategy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Calculates a date a specified number of working days before or after another rule's occurrence, such as the last
/// working day before a public-holiday shutdown.
/// </summary>
/// <remarks>
/// <para>
/// Unlike <see cref="OffsetFromRuleStrategy" />, which counts calendar days, this strategy counts working days: the
/// reference date is the origin and is not counted, and non-working days (rest days and applicable non-working
/// notable-date occurrences) are skipped. The working-day set is computed deterministically and independently of
/// resource-declaration order by the <see cref="StrategyResolutionContext" />. The referenced rule is resolved for
/// <c>year + referenceYearOffset</c>.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // The last working day before Boxing Day.
/// var strategy = new WorkingDayOffsetFromRuleStrategy("boxing-day", null, -1);
///]]>
/// </code>
/// </example>
/// </remarks>
/// <seealso cref="IDateCalculationStrategy" /> <seealso href="../guides/calendar/rule-authoring.html">Authoring notable
/// date rules (guide)</seealso>
public sealed class WorkingDayOffsetFromRuleStrategy
    : IDateCalculationStrategy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WorkingDayOffsetFromRuleStrategy" /> class.
    /// </summary>
    /// <param name="notableDateRef">The identifier of the referenced notable-date concept.</param>
    /// <param name="ruleRef">
    /// The identifier of the referenced rule, or <see langword="null" /> to use its sole rule.
    /// </param>
    /// <param name="offsetWorkingDays">The signed number of working days to move from the referenced occurrence.</param>
    /// <param name="referenceYearOffset">The signed year offset applied to the referenced rule.</param>
    /// <exception cref="ArgumentNullException"><paramref name="notableDateRef" /> is <see langword="null" />.</exception>
    public WorkingDayOffsetFromRuleStrategy(string notableDateRef, string? ruleRef, int offsetWorkingDays, int referenceYearOffset = 0)
    {
        ThrowHelper.ThrowIfNull(notableDateRef);

        NotableDateRef = notableDateRef;
        RuleRef = ruleRef;
        OffsetWorkingDays = offsetWorkingDays;
        ReferenceYearOffset = referenceYearOffset;
    }

    /// <summary>
    /// Gets the identifier of the referenced notable-date concept.
    /// </summary>
    /// <value>The referenced concept id.</value>
    public string NotableDateRef { get; }

    /// <summary>
    /// Gets the identifier of the referenced rule.
    /// </summary>
    /// <value>The referenced rule id, or <see langword="null" /> when the sole rule is used.</value>
    public string? RuleRef { get; }

    /// <summary>
    /// Gets the signed number of working days to move from the referenced occurrence.
    /// </summary>
    /// <value>The working-day offset.</value>
    public int OffsetWorkingDays { get; }

    /// <summary>
    /// Gets the signed year offset applied to the referenced rule.
    /// </summary>
    /// <value>The reference year offset.</value>
    public int ReferenceYearOffset { get; }

    /// <inheritdoc />
    public DateOnly? Calculate(int year, StrategyResolutionContext context)
    {
        ThrowHelper.ThrowIfNull(context);

        long referenceYear = (long)year + ReferenceYearOffset;
        if (referenceYear is < 1 or > 9999)
            return null;

        if (context.ResolveReference(NotableDateRef, RuleRef, (int)referenceYear) is not DateOnly reference)
            return null;

        return context.AddWorkingDays(reference, OffsetWorkingDays, context.Territory);
    }
}
