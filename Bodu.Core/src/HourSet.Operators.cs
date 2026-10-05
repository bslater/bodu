// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HourSet.Operators.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public readonly partial struct HourSet
{
    /// <summary>
    /// Returns the hours that both sets select.
    /// </summary>
    /// <param name="left">The first set.</param>
    /// <param name="right">The second set.</param>
    /// <returns>The intersection of <paramref name="left" /> and <paramref name="right" />.</returns>
    public static HourSet operator &(HourSet left, HourSet right) =>
        new(left._bits & right._bits);

    /// <summary>
    /// Returns the hours that either set selects.
    /// </summary>
    /// <param name="left">The first set.</param>
    /// <param name="right">The second set.</param>
    /// <returns>The union of <paramref name="left" /> and <paramref name="right" />.</returns>
    public static HourSet operator |(HourSet left, HourSet right) =>
        new(left._bits | right._bits);

    /// <summary>
    /// Returns the hours that exactly one of the sets selects.
    /// </summary>
    /// <param name="left">The first set.</param>
    /// <param name="right">The second set.</param>
    /// <returns>The symmetric difference of <paramref name="left" /> and <paramref name="right" />.</returns>
    public static HourSet operator ^(HourSet left, HourSet right) =>
        new(left._bits ^ right._bits);

    /// <summary>
    /// Returns the hours from 0 to 23 that the set does not select.
    /// </summary>
    /// <param name="value">The set.</param>
    /// <returns>The complement of <paramref name="value" /> within 0 to 23.</returns>
    public static HourSet operator ~(HourSet value) =>
        new(~value._bits & AllBits);

    /// <summary>
    /// Determines whether two sets select the same hours.
    /// </summary>
    /// <param name="left">The first set.</param>
    /// <param name="right">The second set.</param>
    /// <returns><see langword="true" /> when the sets are equal; otherwise <see langword="false" />.</returns>
    public static bool operator ==(HourSet left, HourSet right) =>
        left._bits == right._bits;

    /// <summary>
    /// Determines whether two sets select different hours.
    /// </summary>
    /// <param name="left">The first set.</param>
    /// <param name="right">The second set.</param>
    /// <returns><see langword="true" /> when the sets differ; otherwise <see langword="false" />.</returns>
    public static bool operator !=(HourSet left, HourSet right) =>
        left._bits != right._bits;
}
