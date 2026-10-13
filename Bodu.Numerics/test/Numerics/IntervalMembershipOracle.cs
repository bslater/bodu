// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalMembershipOracle.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Numerics;

/// <summary>
/// Provides a point-membership oracle for the interval and interval-set operations over floating-point endpoints: a
/// catalogue of intervals whose endpoints lie in { -Infinity, 0, 1, +Infinity }, and one probe value for each of those
/// endpoints and for each open gap between consecutive ones.
/// </summary>
/// <remarks>
/// <para>
/// Every catalogue interval is a union of the atoms the probes stand for, so the probes it contains, read through
/// <see cref="Interval{T}.Contains(T)" />, decide every subset, overlap, difference and contiguity question about the
/// catalogue exactly. A probe mask holds bit <c>i</c> when probe <c>i</c> is a member.
/// </para>
/// <para>
/// The four half-bounded intervals anchored at the far infinity (<c>AtLeast(+Infinity)</c>,
/// <c>GreaterThan(+Infinity)</c>, <c>AtMost(-Infinity)</c> and <c>LessThan(-Infinity)</c>) hold no value, yet are not
/// empty, so they are left out of the catalogue.
/// </para>
/// </remarks>
internal static class IntervalMembershipOracle
{
    /// <summary>
    /// Returns the probe values in ascending order: each catalogue endpoint, and one value inside each open gap between
    /// consecutive endpoints.
    /// </summary>
    /// <typeparam name="T">The floating-point endpoint type.</typeparam>
    /// <returns>The probe values.</returns>
    public static T[] Probes<T>()
        where T : IFloatingPointIeee754<T> =>
        [
            T.NegativeInfinity,
            T.CreateChecked(-2),
            T.Zero,
            T.CreateChecked(0.5),
            T.One,
            T.CreateChecked(2),
            T.PositiveInfinity,
        ];

    /// <summary>
    /// Returns the catalogue: bounded intervals with an infinite endpoint, open or closed, on either side or both, the
    /// unbounded shapes anchored at a finite value or at the near infinity, and finite controls.
    /// </summary>
    /// <typeparam name="T">The floating-point endpoint type.</typeparam>
    /// <returns>The catalogue intervals, none of them empty.</returns>
    public static Interval<T>[] Catalog<T>()
        where T : IFloatingPointIeee754<T>
    {
        T negativeInfinity = T.NegativeInfinity;
        T positiveInfinity = T.PositiveInfinity;
        T zero = T.Zero;
        T one = T.One;

        return
        [
            Interval<T>.Closed(zero, positiveInfinity),
            Interval<T>.ClosedOpen(zero, positiveInfinity),
            Interval<T>.OpenClosed(zero, positiveInfinity),
            Interval<T>.Open(zero, positiveInfinity),
            Interval<T>.Closed(one, positiveInfinity),
            Interval<T>.Closed(negativeInfinity, zero),
            Interval<T>.ClosedOpen(negativeInfinity, zero),
            Interval<T>.OpenClosed(negativeInfinity, zero),
            Interval<T>.Open(negativeInfinity, zero),
            Interval<T>.ClosedOpen(negativeInfinity, one),
            Interval<T>.Singleton(positiveInfinity),
            Interval<T>.Singleton(negativeInfinity),
            Interval<T>.Closed(negativeInfinity, positiveInfinity),
            Interval<T>.Open(negativeInfinity, positiveInfinity),
            Interval<T>.ClosedOpen(negativeInfinity, positiveInfinity),
            Interval<T>.OpenClosed(negativeInfinity, positiveInfinity),
            Interval<T>.Closed(zero, one),
            Interval<T>.Open(zero, one),
            Interval<T>.All,
            Interval<T>.AtLeast(zero),
            Interval<T>.GreaterThan(zero),
            Interval<T>.AtMost(zero),
            Interval<T>.LessThan(zero),
            Interval<T>.AtLeast(one),
            Interval<T>.LessThan(one),
            Interval<T>.AtLeast(negativeInfinity),
            Interval<T>.GreaterThan(negativeInfinity),
            Interval<T>.AtMost(positiveInfinity),
            Interval<T>.LessThan(positiveInfinity),
        ];
    }

    /// <summary>
    /// Returns the probe mask of <paramref name="interval" />.
    /// </summary>
    /// <typeparam name="T">The floating-point endpoint type.</typeparam>
    /// <param name="interval">The interval whose members are probed.</param>
    /// <returns>A mask holding bit <c>i</c> when probe <c>i</c> is a member of <paramref name="interval" />.</returns>
    public static int Mask<T>(Interval<T> interval)
        where T : IFloatingPointIeee754<T>
    {
        T[] probes = Probes<T>();
        var mask = 0;
        for (int i = 0; i < probes.Length; i++)
        {
            if (interval.Contains(probes[i]))
                mask |= 1 << i;
        }

        return mask;
    }

    /// <summary>
    /// Returns the probe mask of <paramref name="set" />.
    /// </summary>
    /// <typeparam name="T">The floating-point endpoint type.</typeparam>
    /// <param name="set">The set whose members are probed.</param>
    /// <returns>A mask holding bit <c>i</c> when probe <c>i</c> is a member of <paramref name="set" />.</returns>
    public static int Mask<T>(IntervalSet<T> set)
        where T : IFloatingPointIeee754<T>
    {
        T[] probes = Probes<T>();
        var mask = 0;
        for (int i = 0; i < probes.Length; i++)
        {
            if (set.Contains(probes[i]))
                mask |= 1 << i;
        }

        return mask;
    }

    /// <summary>
    /// Returns the combined probe mask of <paramref name="pieces" />.
    /// </summary>
    /// <typeparam name="T">The floating-point endpoint type.</typeparam>
    /// <param name="pieces">The intervals whose members are probed.</param>
    /// <returns>A mask holding bit <c>i</c> when probe <c>i</c> is a member of any piece.</returns>
    public static int Mask<T>(IEnumerable<Interval<T>> pieces)
        where T : IFloatingPointIeee754<T>
    {
        var mask = 0;
        foreach (Interval<T> piece in pieces)
            mask |= Mask(piece);

        return mask;
    }

    /// <summary>
    /// Determines whether the probes in <paramref name="mask" /> form one unbroken run, so that the values they stand
    /// for make up a single interval.
    /// </summary>
    /// <param name="mask">The probe mask.</param>
    /// <returns><see langword="true" /> when the mask is non-zero and has no gap; otherwise <see langword="false" />.</returns>
    public static bool IsContiguous(int mask)
    {
        if (mask == 0)
            return false;

        int run = mask >> BitOperations.TrailingZeroCount(mask);
        return (run & (run + 1)) == 0;
    }

    /// <summary>
    /// Returns the pieces of <paramref name="pair" /> in order.
    /// </summary>
    /// <typeparam name="T">The floating-point endpoint type.</typeparam>
    /// <param name="pair">The pair to read.</param>
    /// <returns>The pieces of <paramref name="pair" />.</returns>
    public static Interval<T>[] Pieces<T>(IntervalPair<T> pair)
        where T : IFloatingPointIeee754<T>
    {
        var pieces = new Interval<T>[pair.Count];
        for (int i = 0; i < pieces.Length; i++)
            pieces[i] = pair[i];

        return pieces;
    }

    /// <summary>
    /// Returns the pieces of <paramref name="set" /> in order.
    /// </summary>
    /// <typeparam name="T">The floating-point endpoint type.</typeparam>
    /// <param name="set">The set to read.</param>
    /// <returns>The pieces of <paramref name="set" />.</returns>
    public static Interval<T>[] Pieces<T>(IntervalSet<T> set)
        where T : IFloatingPointIeee754<T>
    {
        var pieces = new Interval<T>[set.Count];
        for (int i = 0; i < pieces.Length; i++)
            pieces[i] = set[i];

        return pieces;
    }

    /// <summary>
    /// Describes the first way <paramref name="pieces" /> departs from a list of disjoint pieces in ascending order:
    /// a piece that holds no probe, a piece whose probes are not one run, or a piece that does not start after the one
    /// before it ends (or, when <paramref name="requireGap" /> is set, touches it).
    /// </summary>
    /// <typeparam name="T">The floating-point endpoint type.</typeparam>
    /// <param name="pieces">The pieces to inspect.</param>
    /// <param name="requireGap">
    /// <see langword="true" /> to require a value between consecutive pieces, as a normalized interval set does.
    /// </param>
    /// <returns>The description, or <see langword="null" /> when the pieces are well formed.</returns>
    public static string? FindPieceDefect<T>(IReadOnlyList<Interval<T>> pieces, bool requireGap)
        where T : IFloatingPointIeee754<T>
    {
        int previousHighest = -1;
        for (int i = 0; i < pieces.Count; i++)
        {
            int mask = Mask(pieces[i]);
            if (mask == 0)
                return $"piece {pieces[i]} holds no value";

            if (!IsContiguous(mask))
                return $"piece {pieces[i]} is not one run";

            int lowest = BitOperations.TrailingZeroCount(mask);
            int minimumStart = requireGap ? previousHighest + 2 : previousHighest + 1;
            if (i > 0 && lowest < minimumStart)
                return $"piece {pieces[i]} does not start clear of {pieces[i - 1]}";

            previousHighest = 31 - BitOperations.LeadingZeroCount((uint)mask);
        }

        return null;
    }
}
