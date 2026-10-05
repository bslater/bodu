// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MonthSet.Operators.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public readonly partial struct MonthSet
{
    /// <summary>
    /// Returns the months that both sets select.
    /// </summary>
    /// <param name="left">The first set.</param>
    /// <param name="right">The second set.</param>
    /// <returns>The intersection of <paramref name="left" /> and <paramref name="right" />.</returns>
    public static MonthSet operator &(MonthSet left, MonthSet right) =>
        new(left._bits & right._bits);

    /// <summary>
    /// Returns the months that either set selects.
    /// </summary>
    /// <param name="left">The first set.</param>
    /// <param name="right">The second set.</param>
    /// <returns>The union of <paramref name="left" /> and <paramref name="right" />.</returns>
    public static MonthSet operator |(MonthSet left, MonthSet right) =>
        new(left._bits | right._bits);

    /// <summary>
    /// Returns the months that exactly one of the sets selects.
    /// </summary>
    /// <param name="left">The first set.</param>
    /// <param name="right">The second set.</param>
    /// <returns>The symmetric difference of <paramref name="left" /> and <paramref name="right" />.</returns>
    public static MonthSet operator ^(MonthSet left, MonthSet right) =>
        new(left._bits ^ right._bits);

    /// <summary>
    /// Returns the months from 1 to 12 that the set does not select.
    /// </summary>
    /// <param name="value">The set.</param>
    /// <returns>The complement of <paramref name="value" /> within 1 to 12.</returns>
    public static MonthSet operator ~(MonthSet value) =>
        new(~value._bits & AllBits);

    /// <summary>
    /// Determines whether two sets select the same months.
    /// </summary>
    /// <param name="left">The first set.</param>
    /// <param name="right">The second set.</param>
    /// <returns><see langword="true" /> when the sets are equal; otherwise <see langword="false" />.</returns>
    public static bool operator ==(MonthSet left, MonthSet right) =>
        left._bits == right._bits;

    /// <summary>
    /// Determines whether two sets select different months.
    /// </summary>
    /// <param name="left">The first set.</param>
    /// <param name="right">The second set.</param>
    /// <returns><see langword="true" /> when the sets differ; otherwise <see langword="false" />.</returns>
    public static bool operator !=(MonthSet left, MonthSet right) =>
        left._bits != right._bits;
}
