// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Interval{T}.Helpers.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Numerics;

/// <summary>
/// Provides type-inferring factory methods for <see cref="Interval{T}" />.
/// </summary>
/// <remarks>
/// <para>
/// The methods on this class mirror the static factories declared on <see cref="Interval{T}" /> but accept the endpoint
/// values directly, letting the compiler infer the endpoint type from the arguments. This avoids the need to repeat the
/// generic parameter at the call site: <c>Interval.Closed(1, 5)</c> compiles to a <see cref="Interval{T}" /> over
/// <see cref="int" /> without an explicit type argument, and <c>Interval.ClosedOpen(0m, 100m)</c> picks
/// <see cref="decimal" /> from the literal suffixes.
/// </para>
/// <para>
/// Prefer the non-generic helpers when the endpoint type is obvious from the arguments — typical for literals, local
/// variables, and expressions that already carry the type — and use <see cref="Interval{T}" />'s own static factories
/// (e.g. <see cref="Interval{T}.Closed(T, T)" />) when the call site needs an explicit type to disambiguate, when the
/// endpoint type comes from a generic context, or when the factory is being held as a method group.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.Numerics;
///
/// // Type inferred from the int literals.
/// var ints = Interval.ClosedOpen(0, 100);           // Interval<int>
///
/// // Type inferred from the decimal-suffixed literals.
/// var prices = Interval.OpenClosed(1000m, 10_000m); // Interval<decimal>
///
/// // Type inferred from the local variable.
/// double low = 1.5, high = 2.5;
/// var span = Interval.Closed(low, high);            // Interval<double>
///]]>
/// </code>
/// </example>
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
    /// Creates an open-open interval <c>(lower, upper)</c> with <typeparamref name="T" /> inferred from the arguments.
    /// </summary>
    /// <typeparam name="T">The endpoint type, inferred at the call site.</typeparam>
    /// <param name="lower">The lower endpoint.</param>
    /// <param name="upper">The upper endpoint.</param>
    /// <returns>An open-open interval over the supplied bounds.</returns>
    public static Interval<T> Open<T>(T lower, T upper)
        where T : INumber<T> =>
        Interval<T>.Open(lower, upper);

    /// <summary>
    /// Creates a closed-open interval <c>[lower, upper)</c> with <typeparamref name="T" /> inferred from the arguments.
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
    /// Creates a degenerate single-point interval <c>[value, value]</c> with <typeparamref name="T" /> inferred from
    /// the argument.
    /// </summary>
    /// <typeparam name="T">The endpoint type, inferred at the call site.</typeparam>
    /// <param name="value">The single value the interval contains.</param>
    /// <returns>A degenerate interval over the single value.</returns>
    public static Interval<T> Singleton<T>(T value)
        where T : INumber<T> =>
        Interval<T>.Singleton(value);

    /// <summary>
    /// Creates the lower-bounded interval <c>[lower, +&#x221E;)</c> with <typeparamref name="T" /> inferred from the
    /// argument.
    /// </summary>
    /// <typeparam name="T">The endpoint type, inferred at the call site.</typeparam>
    /// <param name="lower">The inclusive lower endpoint.</param>
    /// <returns>A closed-below, upper-unbounded interval.</returns>
    public static Interval<T> AtLeast<T>(T lower)
        where T : INumber<T> =>
        Interval<T>.AtLeast(lower);

    /// <summary>
    /// Creates the lower-bounded interval <c>(lower, +&#x221E;)</c> with <typeparamref name="T" /> inferred from the
    /// argument.
    /// </summary>
    /// <typeparam name="T">The endpoint type, inferred at the call site.</typeparam>
    /// <param name="lower">The exclusive lower endpoint.</param>
    /// <returns>An open-below, upper-unbounded interval.</returns>
    public static Interval<T> GreaterThan<T>(T lower)
        where T : INumber<T> =>
        Interval<T>.GreaterThan(lower);

    /// <summary>
    /// Creates the upper-bounded interval <c>(-&#x221E;, upper]</c> with <typeparamref name="T" /> inferred from the
    /// argument.
    /// </summary>
    /// <typeparam name="T">The endpoint type, inferred at the call site.</typeparam>
    /// <param name="upper">The inclusive upper endpoint.</param>
    /// <returns>A lower-unbounded, closed-above interval.</returns>
    public static Interval<T> AtMost<T>(T upper)
        where T : INumber<T> =>
        Interval<T>.AtMost(upper);

    /// <summary>
    /// Creates the upper-bounded interval <c>(-&#x221E;, upper)</c> with <typeparamref name="T" /> inferred from the
    /// argument.
    /// </summary>
    /// <typeparam name="T">The endpoint type, inferred at the call site.</typeparam>
    /// <param name="upper">The exclusive upper endpoint.</param>
    /// <returns>A lower-unbounded, open-above interval.</returns>
    public static Interval<T> LessThan<T>(T upper)
        where T : INumber<T> =>
        Interval<T>.LessThan(upper);
}
