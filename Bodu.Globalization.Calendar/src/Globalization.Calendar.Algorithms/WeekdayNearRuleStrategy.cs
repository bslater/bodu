// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekdayNearRuleStrategy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Calculates a weekday on, before, after, or nearest to the date produced by another rule, such as the first Monday
/// after Easter Sunday.
/// </summary>
/// <remarks>
/// <para>
/// This is the dynamic-reference counterpart to <see cref="WeekdayNearDateStrategy" />: the anchor is another rule's
/// occurrence rather than a fixed month and day. The referenced rule is resolved for <c>year + referenceYearOffset</c>,
/// letting a rule in one civil year anchor to a reference in an adjacent year. A missing, ambiguous, circular, or
/// recurring reference produces no occurrence and is reported by semantic validation where statically detectable.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // The first Monday after Easter Sunday.
/// var strategy = new WeekdayNearRuleStrategy("easter-sunday", null, DayOfWeek.Monday, WeekdayProximity.After);
///]]>
/// </code>
/// </example>
/// </remarks>
/// <seealso cref="IDateCalculationStrategy" /> <seealso href="../guides/calendar/rule-authoring.html">Authoring notable
/// date rules (guide)</seealso>
public sealed class WeekdayNearRuleStrategy
    : IDateCalculationStrategy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WeekdayNearRuleStrategy" /> class.
    /// </summary>
    /// <param name="notableDateRef">The identifier of the referenced notable-date concept.</param>
    /// <param name="ruleRef">
    /// The identifier of the referenced rule, or <see langword="null" /> to use its sole rule.
    /// </param>
    /// <param name="dayOfWeek">The weekday to seek.</param>
    /// <param name="direction">The direction and inclusivity to apply when seeking the weekday.</param>
    /// <param name="referenceYearOffset">The signed year offset applied to the referenced rule.</param>
    /// <exception cref="ArgumentNullException"><paramref name="notableDateRef" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="dayOfWeek" /> or <paramref name="direction" /> is not a defined enumeration value.
    /// </exception>
    public WeekdayNearRuleStrategy(string notableDateRef, string? ruleRef, DayOfWeek dayOfWeek, WeekdayProximity direction, int referenceYearOffset = 0)
    {
        ThrowHelper.ThrowIfNull(notableDateRef);
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);
        ThrowHelper.ThrowIfEnumValueIsUndefined(direction);

        NotableDateRef = notableDateRef;
        RuleRef = ruleRef;
        DayOfWeek = dayOfWeek;
        Direction = direction;
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
    /// Gets the direction and inclusivity applied when seeking the weekday.
    /// </summary>
    /// <value>The configured <see cref="WeekdayProximity" />.</value>
    public WeekdayProximity Direction { get; }

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

        return WeekdayMath.SeekOrNull(reference, DayOfWeek, Direction);
    }
}
