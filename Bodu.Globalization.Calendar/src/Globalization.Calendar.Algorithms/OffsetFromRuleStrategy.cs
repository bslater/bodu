// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OffsetFromRuleStrategy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Calculates a notable date as a fixed day offset from another rule's occurrence, such as Good Friday two days before
/// Easter Sunday.
/// </summary>
/// <remarks>
/// <para>
/// The referenced rule is identified by stable id. When the reference cannot be resolved unambiguously the strategy
/// produces no occurrence; the loader reports the unresolved or ambiguous reference as a validation error.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // The rule shape this strategy realizes - a day count from another concept's rule
/// // (Good Friday: two days before Easter Sunday). The referenced concept must exist
/// // in the document (or arrive via an import).
/// NotableDateResource resource = NotableDateDocumentBuilder.Create("demo")
///     .AddImport("christian-western", i => i.Use("easter-sunday"))
///     .AddNotableDate("good-friday", "Good Friday", NotableDateCategory.PublicHoliday, c => c
///         .AddRule("default", r => r.OffsetFromRule("easter-sunday", -2)))
///     .Build(CommonNotableDateResources.Resolver);
///]]>
/// </code>
/// </example>
/// </remarks>
/// <seealso cref="IDateCalculationStrategy" /> <seealso href="../guides/calendar/rule-authoring.html">Authoring notable
/// date rules (guide)</seealso>
public sealed class OffsetFromRuleStrategy
    : IDateCalculationStrategy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OffsetFromRuleStrategy" /> class.
    /// </summary>
    /// <param name="notableDateRef">The identifier of the referenced notable-date concept.</param>
    /// <param name="ruleRef">
    /// The identifier of the referenced rule, or <see langword="null" /> to use its sole rule.
    /// </param>
    /// <param name="offsetDays">The signed number of days to offset from the referenced occurrence.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="notableDateRef" /> is <see langword="null" />.
    /// </exception>
    public OffsetFromRuleStrategy(string notableDateRef, string? ruleRef, int offsetDays)
    {
        ThrowHelper.ThrowIfNull(notableDateRef);

        NotableDateRef = notableDateRef;
        RuleRef = ruleRef;
        OffsetDays = offsetDays;
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
    /// Gets the signed number of days to offset from the referenced occurrence.
    /// </summary>
    /// <value>The day offset.</value>
    public int OffsetDays { get; }

    /// <inheritdoc />
    public DateOnly? Calculate(int year, StrategyResolutionContext context)
    {
        ThrowHelper.ThrowIfNull(context);

        if (context.ResolveReference(NotableDateRef, RuleRef, year) is not DateOnly reference)
            return null;

        // Guard the projection against rolling past the representable date range at the year extremes; the engine
        // treats an out-of-range offset as "no occurrence" rather than failing the query.
        long target = (long)reference.DayNumber + OffsetDays;
        if (target < DateOnly.MinValue.DayNumber || target > DateOnly.MaxValue.DayNumber)
            return null;

        return DateOnly.FromDayNumber((int)target);
    }
}
