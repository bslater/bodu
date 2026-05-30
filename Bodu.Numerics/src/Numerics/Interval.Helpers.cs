// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Interval.Helpers.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Numerics;

/// <summary>
/// Provides type-inferring factory methods for <see cref="Interval{T}" />.
/// </summary>
/// <remarks>
/// The methods on this class mirror the static factories declared on <see cref="Interval{T}" /> but accept the
/// endpoint values directly, letting the compiler infer <typeparamref name="T" /> from the arguments. This avoids
/// the need to repeat the generic parameter at the call site, so <c>Interval.Closed(1, 5)</c> compiles to a
/// <see cref="Interval{T}" /> over <see cref="int" /> without an explicit type argument.
/// </remarks>
public static class Interval
{
    /// <summary>
    /// Creates a closed-closed interval <c>[lower, upper]</c> with <typeparamref name="T" /> inferred from the
    /// arguments.
    /// </summary>
    /// <typeparam name="T">The endpoint type, inferred at the call site.</typeparam>
    /// <param name="lower">The lower endpoint.</param>
    /// <param name="upper">The upper endpoint.</param>
    /// <returns>A closed-closed interval over the supplied bounds.</returns>
    public static Interval<T> Closed<T>(T lower, T upper)
        where T : INumber<T> =>
        Interval<T>.Closed(lower, upper);

    /// <summary>
    /// Creates an open-open interval <c>(lower, upper)</c> with <typeparamref name="T" /> inferred from the
    /// arguments.
    /// </summary>
    /// <typeparam name="T">The endpoint type, inferred at the call site.</typeparam>
    /// <param name="lower">The lower endpoint.</param>
    /// <param name="upper">The upper endpoint.</param>
    /// <returns>An open-open interval over the supplied bounds.</returns>
    public static Interval<T> Open<T>(T lower, T upper)
        where T : INumber<T> =>
        Interval<T>.Open(lower, upper);

    /// <summary>
    /// Creates a closed-open interval <c>[lower, upper)</c> with <typeparamref name="T" /> inferred from the
    /// arguments.
    /// </summary>
    /// <typeparam name="T">The endpoint type, inferred at the call site.</typeparam>
    /// <param name="lower">The lower endpoint (included).</param>
    /// <param name="upper">The upper endpoint (excluded).</param>
    /// <returns>A closed-open interval over the supplied bounds.</returns>
    public static Interval<T> ClosedOpen<T>(T lower, T upper)
        where T : INumber<T> =>
        Interval<T>.ClosedOpen(lower, upper);

    /// <summary>
    /// Creates an open-closed interval <c>(lower, upper]</c> with <typeparamref name="T" /> inferred from the
    /// arguments.
    /// </summary>
    /// <typeparam name="T">The endpoint type, inferred at the call site.</typeparam>
    /// <param name="lower">The lower endpoint (excluded).</param>
    /// <param name="upper">The upper endpoint (included).</param>
    /// <returns>An open-closed interval over the supplied bounds.</returns>
    public static Interval<T> OpenClosed<T>(T lower, T upper)
        where T : INumber<T> =>
        Interval<T>.OpenClosed(lower, upper);

    /// <summary>
    /// Creates a degenerate single-point interval <c>[value, value]</c> with <typeparamref name="T" /> inferred
    /// from the argument.
    /// </summary>
    /// <typeparam name="T">The endpoint type, inferred at the call site.</typeparam>
    /// <param name="value">The single value the interval contains.</param>
    /// <returns>A degenerate interval over the single value.</returns>
    public static Interval<T> Singleton<T>(T value)
        where T : INumber<T> =>
        Interval<T>.Singleton(value);
}
