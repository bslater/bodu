// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DayOfMonthSet.Operators.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public readonly partial struct DayOfMonthSet
{
    /// <summary>
    /// Returns the days that both sets select.
    /// </summary>
    /// <param name="left">The first set.</param>
    /// <param name="right">The second set.</param>
    /// <returns>The intersection of <paramref name="left" /> and <paramref name="right" />.</returns>
    public static DayOfMonthSet operator &(DayOfMonthSet left, DayOfMonthSet right) =>
        new(left._bits & right._bits);

    /// <summary>
    /// Returns the days that either set selects.
    /// </summary>
    /// <param name="left">The first set.</param>
    /// <param name="right">The second set.</param>
    /// <returns>The union of <paramref name="left" /> and <paramref name="right" />.</returns>
    public static DayOfMonthSet operator |(DayOfMonthSet left, DayOfMonthSet right) =>
        new(left._bits | right._bits);

    /// <summary>
    /// Returns the days that exactly one of the sets selects.
    /// </summary>
    /// <param name="left">The first set.</param>
    /// <param name="right">The second set.</param>
    /// <returns>The symmetric difference of <paramref name="left" /> and <paramref name="right" />.</returns>
    public static DayOfMonthSet operator ^(DayOfMonthSet left, DayOfMonthSet right) =>
        new(left._bits ^ right._bits);

    /// <summary>
    /// Returns the days from 1 to 31 that the set does not select.
    /// </summary>
    /// <param name="value">The set.</param>
    /// <returns>The complement of <paramref name="value" /> within 1 to 31.</returns>
    public static DayOfMonthSet operator ~(DayOfMonthSet value) =>
        new(~value._bits & AllBits);

    /// <summary>
    /// Determines whether two sets select the same days.
    /// </summary>
    /// <param name="left">The first set.</param>
    /// <param name="right">The second set.</param>
    /// <returns><see langword="true" /> when the sets are equal; otherwise <see langword="false" />.</returns>
    public static bool operator ==(DayOfMonthSet left, DayOfMonthSet right) =>
        left._bits == right._bits;

    /// <summary>
    /// Determines whether two sets select different days.
    /// </summary>
    /// <param name="left">The first set.</param>
    /// <param name="right">The second set.</param>
    /// <returns><see langword="true" /> when the sets differ; otherwise <see langword="false" />.</returns>
    public static bool operator !=(DayOfMonthSet left, DayOfMonthSet right) =>
        left._bits != right._bits;
}
