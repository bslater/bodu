// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NthWeekdayFromRuleStrategy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Calculates the Nth occurrence of a weekday strictly before or strictly after another rule's occurrence, such as the
/// second Monday after a festival.
/// </summary>
/// <remarks>
/// <para>
/// A positive ordinal selects the Nth matching weekday strictly after the reference; a negative ordinal selects the Nth
/// matching weekday strictly before it. The reference date is never counted, even when its weekday matches. The
/// referenced rule is resolved for <c>year + referenceYearOffset</c>. A missing, ambiguous, circular, or recurring
/// reference produces no occurrence.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // The second Monday after a referenced festival.
/// var strategy = new NthWeekdayFromRuleStrategy("festival", null, DayOfWeek.Monday, 2);
///]]>
/// </code>
/// </example>
/// </remarks>
/// <seealso cref="IDateCalculationStrategy" /> <seealso href="../guides/calendar/rule-authoring.html">Authoring notable
/// date rules (guide)</seealso>
public sealed class NthWeekdayFromRuleStrategy
    : IDateCalculationStrategy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NthWeekdayFromRuleStrategy" /> class.
    /// </summary>
    /// <param name="notableDateRef">The identifier of the referenced notable-date concept.</param>
    /// <param name="ruleRef">
    /// The identifier of the referenced rule, or <see langword="null" /> to use its sole rule.
    /// </param>
    /// <param name="dayOfWeek">The weekday to seek.</param>
    /// <param name="ordinal">The signed ordinal count of matching weekdays from the reference.</param>
    /// <param name="referenceYearOffset">The signed year offset applied to the referenced rule.</param>
    /// <exception cref="ArgumentNullException"><paramref name="notableDateRef" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="dayOfWeek" /> is not a defined <see cref="System.DayOfWeek" />, or <paramref name="ordinal" /> is
    /// zero.
    /// </exception>
    public NthWeekdayFromRuleStrategy(string notableDateRef, string? ruleRef, DayOfWeek dayOfWeek, int ordinal, int referenceYearOffset = 0)
    {
        ThrowHelper.ThrowIfNull(notableDateRef);
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);
        if (ordinal == 0)
            throw new ArgumentOutOfRangeException(nameof(ordinal), CalendarResourceStrings.Arg_OutOfRange_OrdinalZero);

        NotableDateRef = notableDateRef;
        RuleRef = ruleRef;
        DayOfWeek = dayOfWeek;
        Ordinal = ordinal;
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
    /// Gets the weekday to seek.
    /// </summary>
    /// <value>The target weekday.</value>
    public DayOfWeek DayOfWeek { get; }

    /// <summary>
    /// Gets the signed ordinal count of matching weekdays from the reference.
    /// </summary>
    /// <value>The ordinal; positive counts strictly after, negative strictly before.</value>
    public int Ordinal { get; }

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

        try
        {
            DateOnly firstMatch = Ordinal > 0
                ? reference.NextDateOfWeek(DayOfWeek)
                : reference.PreviousDateOfWeek(DayOfWeek);

            long target = (long)firstMatch.DayNumber + ((long)(Math.Abs(Ordinal) - 1) * 7 * Math.Sign(Ordinal));
            if (target < DateOnly.MinValue.DayNumber || target > DateOnly.MaxValue.DayNumber)
                return null;

            return DateOnly.FromDayNumber((int)target);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }
}
