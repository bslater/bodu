// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DiscreteInterval{T}.Helpers.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Numerics;

/// <summary>
/// Provides type-inferring factory methods for <see cref="DiscreteInterval{T}" />, mirroring the static factories on
/// the type but inferring the endpoint type from the arguments.
/// </summary>
public static class DiscreteInterval
{
    /// <summary>
    /// Creates a closed interval <c>[lower, upper]</c> with <typeparamref name="T" /> inferred from the arguments.
    /// </summary>
    /// <typeparam name="T">The integer endpoint type, inferred at the call site.</typeparam>
    /// <param name="lower">The inclusive lower bound.</param>
    /// <param name="upper">The inclusive upper bound.</param>
    /// <returns>The interval.</returns>
    public static DiscreteInterval<T> Closed<T>(T lower, T upper)
        where T : IBinaryInteger<T> =>
        DiscreteInterval<T>.Closed(lower, upper);

    /// <summary>
    /// Creates an open interval <c>(lower, upper)</c> with <typeparamref name="T" /> inferred from the arguments.
    /// </summary>
    /// <typeparam name="T">The integer endpoint type, inferred at the call site.</typeparam>
    /// <param name="lower">The exclusive lower bound.</param>
    /// <param name="upper">The exclusive upper bound.</param>
    /// <returns>The canonicalized interval.</returns>
    public static DiscreteInterval<T> Open<T>(T lower, T upper)
        where T : IBinaryInteger<T> =>
        DiscreteInterval<T>.Open(lower, upper);

    /// <summary>
    /// Creates a closed-open interval <c>[lower, upper)</c> with <typeparamref name="T" /> inferred from the arguments.
    /// </summary>
    /// <typeparam name="T">The integer endpoint type, inferred at the call site.</typeparam>
    /// <param name="lower">The inclusive lower bound.</param>
    /// <param name="upper">The exclusive upper bound.</param>
    /// <returns>The canonicalized interval.</returns>
    public static DiscreteInterval<T> ClosedOpen<T>(T lower, T upper)
        where T : IBinaryInteger<T> =>
        DiscreteInterval<T>.ClosedOpen(lower, upper);

    /// <summary>
    /// Creates an open-closed interval <c>(lower, upper]</c> with <typeparamref name="T" /> inferred from the
    /// arguments.
    /// </summary>
    /// <typeparam name="T">The integer endpoint type, inferred at the call site.</typeparam>
    /// <param name="lower">The exclusive lower bound.</param>
    /// <param name="upper">The inclusive upper bound.</param>
    /// <returns>The canonicalized interval.</returns>
    public static DiscreteInterval<T> OpenClosed<T>(T lower, T upper)
        where T : IBinaryInteger<T> =>
        DiscreteInterval<T>.OpenClosed(lower, upper);

    /// <summary>
    /// Creates a single-integer interval <c>[value, value]</c> with <typeparamref name="T" /> inferred from the
    /// argument.
    /// </summary>
    /// <typeparam name="T">The integer endpoint type, inferred at the call site.</typeparam>
    /// <param name="value">The single integer.</param>
    /// <returns>A one-element interval.</returns>
    public static DiscreteInterval<T> Singleton<T>(T value)
        where T : IBinaryInteger<T> =>
        DiscreteInterval<T>.Singleton(value);

    /// <summary>
    /// Creates a lower-bounded interval <c>[lower, +&#x221E;)</c> with <typeparamref name="T" /> inferred from the
    /// argument.
    /// </summary>
    /// <typeparam name="T">The integer endpoint type, inferred at the call site.</typeparam>
    /// <param name="lower">The inclusive lower bound.</param>
    /// <returns>An upper-unbounded interval.</returns>
    public static DiscreteInterval<T> AtLeast<T>(T lower)
        where T : IBinaryInteger<T> =>
        DiscreteInterval<T>.AtLeast(lower);

    /// <summary>
    /// Creates a lower-bounded interval <c>(lower, +&#x221E;)</c> with <typeparamref name="T" /> inferred from the
    /// argument.
    /// </summary>
    /// <typeparam name="T">The integer endpoint type, inferred at the call site.</typeparam>
    /// <param name="lower">The exclusive lower bound.</param>
    /// <returns>An upper-unbounded interval.</returns>
    public static DiscreteInterval<T> GreaterThan<T>(T lower)
        where T : IBinaryInteger<T> =>
        DiscreteInterval<T>.GreaterThan(lower);

    /// <summary>
    /// Creates an upper-bounded interval <c>(-&#x221E;, upper]</c> with <typeparamref name="T" /> inferred from the
    /// argument.
    /// </summary>
    /// <typeparam name="T">The integer endpoint type, inferred at the call site.</typeparam>
    /// <param name="upper">The inclusive upper bound.</param>
    /// <returns>A lower-unbounded interval.</returns>
    public static DiscreteInterval<T> AtMost<T>(T upper)
        where T : IBinaryInteger<T> =>
        DiscreteInterval<T>.AtMost(upper);

    /// <summary>
    /// Creates an upper-bounded interval <c>(-&#x221E;, upper)</c> with <typeparamref name="T" /> inferred from the
    /// argument.
    /// </summary>
    /// <typeparam name="T">The integer endpoint type, inferred at the call site.</typeparam>
    /// <param name="upper">The exclusive upper bound.</param>
    /// <returns>A lower-unbounded interval.</returns>
    public static DiscreteInterval<T> LessThan<T>(T upper)
        where T : IBinaryInteger<T> =>
        DiscreteInterval<T>.LessThan(upper);
}
