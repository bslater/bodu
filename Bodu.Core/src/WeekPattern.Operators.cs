// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekPattern.Operators.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial struct WeekPattern
{
    /// <summary>
    /// Implicitly converts a <see cref="WeekPattern" /> to its underlying <see cref="byte" /> bitmask.
    /// </summary>
    /// <param name="pattern">The <see cref="WeekPattern" /> to convert.</param>
    /// <returns>A <see cref="byte" /> representing the selected days.</returns>
    public static implicit operator byte(WeekPattern pattern)
    {
        return pattern._selectedDays;
    }

    /// <summary>
    /// Determines whether two <see cref="WeekPattern" /> instances have the same selected days.
    /// </summary>
    /// <param name="left">The first operand.</param>
    /// <param name="right">The second operand.</param>
    /// <returns><see langword="true" /> if both instances are equal; otherwise, <see langword="false" />.</returns>
    public static bool operator ==(WeekPattern left, WeekPattern right)
    {
        return left._selectedDays == right._selectedDays;
    }

    /// <summary>
    /// Returns a new <see cref="WeekPattern" /> with all seven days toggled — selected days become unselected and vice
    /// versa.
    /// </summary>
    /// <param name="pattern">The <see cref="WeekPattern" /> to complement.</param>
    /// <returns>The bitwise complement of <paramref name="pattern" />, masked to the valid seven-day range.</returns>
    public static WeekPattern operator ~(WeekPattern pattern)
    {
        return new WeekPattern(~pattern._selectedDays & MaxValue);
    }

    /// <summary>
    /// Returns a new <see cref="WeekPattern" /> containing all days selected in either operand (set union).
    /// </summary>
    /// <param name="left">The first operand.</param>
    /// <param name="right">The second operand.</param>
    /// <returns>A <see cref="WeekPattern" /> whose selected days are the union of the two operands.</returns>
    public static WeekPattern operator |(WeekPattern left, WeekPattern right)
    {
        return new WeekPattern(left._selectedDays | right._selectedDays);
    }

    /// <summary>
    /// Returns a new <see cref="WeekPattern" /> containing only the days selected in exactly one of the two operands
    /// (symmetric difference).
    /// </summary>
    /// <param name="left">The first operand.</param>
    /// <param name="right">The second operand.</param>
    /// <returns>
    /// A <see cref="WeekPattern" /> whose selected days are those that differ between the two operands.
    /// </returns>
    public static WeekPattern operator ^(WeekPattern left, WeekPattern right)
    {
        return new WeekPattern(left._selectedDays ^ right._selectedDays);
    }

    /// <summary>
    /// Returns a new <see cref="WeekPattern" /> containing only the days selected in both operands (set intersection).
    /// </summary>
    /// <param name="left">The first operand.</param>
    /// <param name="right">The second operand.</param>
    /// <returns>A <see cref="WeekPattern" /> whose selected days are the intersection of the two operands.</returns>
    public static WeekPattern operator &(WeekPattern left, WeekPattern right)
    {
        return new WeekPattern(left._selectedDays & right._selectedDays);
    }

    /// <summary>
    /// Determines whether two <see cref="WeekPattern" /> instances have different selected days.
    /// </summary>
    /// <param name="left">The first operand.</param>
    /// <param name="right">The second operand.</param>
    /// <returns><see langword="true" /> if the instances differ; otherwise, <see langword="false" />.</returns>
    public static bool operator !=(WeekPattern left, WeekPattern right)
    {
        return left._selectedDays != right._selectedDays;
    }

    /// <summary>
    /// Determines whether one <see cref="WeekPattern" /> precedes another in the underlying bitmask ordering.
    /// </summary>
    /// <param name="left">The first operand.</param>
    /// <param name="right">The second operand.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="left" /> is less than <paramref name="right" />; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// Ordering is based on the numeric value of the underlying bitmask and has no inherent day-of-week meaning.
    /// </remarks>
    public static bool operator <(WeekPattern left, WeekPattern right)
    {
        return left._selectedDays < right._selectedDays;
    }

    /// <summary>
    /// Determines whether one <see cref="WeekPattern" /> precedes or equals another in the underlying bitmask ordering.
    /// </summary>
    /// <param name="left">The first operand.</param>
    /// <param name="right">The second operand.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="left" /> is less than or equal to <paramref name="right" />;
    /// otherwise, <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// Ordering is based on the numeric value of the underlying bitmask and has no inherent day-of-week meaning.
    /// </remarks>
    public static bool operator <=(WeekPattern left, WeekPattern right)
    {
        return left._selectedDays <= right._selectedDays;
    }

    /// <summary>
    /// Determines whether one <see cref="WeekPattern" /> follows another in the underlying bitmask ordering.
    /// </summary>
    /// <param name="left">The first operand.</param>
    /// <param name="right">The second operand.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="left" /> is greater than <paramref name="right" />; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// Ordering is based on the numeric value of the underlying bitmask and has no inherent day-of-week meaning.
    /// </remarks>
    public static bool operator >(WeekPattern left, WeekPattern right)
    {
        return left._selectedDays > right._selectedDays;
    }

    /// <summary>
    /// Determines whether one <see cref="WeekPattern" /> follows or equals another in the underlying bitmask ordering.
    /// </summary>
    /// <param name="left">The first operand.</param>
    /// <param name="right">The second operand.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="left" /> is greater than or equal to <paramref name="right" />;
    /// otherwise, <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// Ordering is based on the numeric value of the underlying bitmask and has no inherent day-of-week meaning.
    /// </remarks>
    public static bool operator >=(WeekPattern left, WeekPattern right)
    {
        return left._selectedDays >= right._selectedDays;
    }
}
