// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SecondSet.Operators.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public readonly partial struct SecondSet
{
    /// <summary>
    /// Returns the seconds that both sets select.
    /// </summary>
    /// <param name="left">The first set.</param>
    /// <param name="right">The second set.</param>
    /// <returns>The intersection of <paramref name="left" /> and <paramref name="right" />.</returns>
    public static SecondSet operator &(SecondSet left, SecondSet right) =>
        new(left._bits & right._bits);

    /// <summary>
    /// Returns the seconds that either set selects.
    /// </summary>
    /// <param name="left">The first set.</param>
    /// <param name="right">The second set.</param>
    /// <returns>The union of <paramref name="left" /> and <paramref name="right" />.</returns>
    public static SecondSet operator |(SecondSet left, SecondSet right) =>
        new(left._bits | right._bits);

    /// <summary>
    /// Returns the seconds that exactly one of the sets selects.
    /// </summary>
    /// <param name="left">The first set.</param>
    /// <param name="right">The second set.</param>
    /// <returns>The symmetric difference of <paramref name="left" /> and <paramref name="right" />.</returns>
    public static SecondSet operator ^(SecondSet left, SecondSet right) =>
        new(left._bits ^ right._bits);

    /// <summary>
    /// Returns the seconds from 0 to 59 that the set does not select.
    /// </summary>
    /// <param name="value">The set.</param>
    /// <returns>The complement of <paramref name="value" /> within 0 to 59.</returns>
    public static SecondSet operator ~(SecondSet value) =>
        new(~value._bits & AllBits);

    /// <summary>
    /// Determines whether two sets select the same seconds.
    /// </summary>
    /// <param name="left">The first set.</param>
    /// <param name="right">The second set.</param>
    /// <returns><see langword="true" /> when the sets are equal; otherwise <see langword="false" />.</returns>
    public static bool operator ==(SecondSet left, SecondSet right) =>
        left._bits == right._bits;

    /// <summary>
    /// Determines whether two sets select different seconds.
    /// </summary>
    /// <param name="left">The first set.</param>
    /// <param name="right">The second set.</param>
    /// <returns><see langword="true" /> when the sets differ; otherwise <see langword="false" />.</returns>
    public static bool operator !=(SecondSet left, SecondSet right) =>
        left._bits != right._bits;
}
