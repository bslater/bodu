// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Memoizer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;

namespace Bodu.Functional;

/// <summary>
/// Provides factory methods that wrap a function with a thread-safe cache, so each distinct argument is computed at
/// most once and subsequent calls return the stored result.
/// </summary>
/// <remarks>
/// <para>
/// Results are cached in a <see cref="ConcurrentDictionary{TKey, TValue}" />. Only successful results are stored: if
/// the underlying function throws, nothing is cached and the next call retries. A <see langword="null" /> result is a
/// valid cached value. For sequential use the function runs exactly once per distinct argument; under concurrent first
/// access for the same argument the function may run more than once, but only one result is published.
/// </para>
/// <para>
/// Arguments are constrained to non-nullable types because they are used as dictionary keys. Memoizing a recursive
/// function requires the function to call the memoized delegate for its recursive step.
/// </para>
/// </remarks>
public static class Memoizer
{
    /// <summary>
    /// Wraps a single-argument function with a cache that uses the default equality comparer for its argument.
    /// </summary>
    /// <typeparam name="TArg">The argument type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="func">The function to memoize.</param>
    /// <returns>A memoizing wrapper around <paramref name="func" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="func" /> is <see langword="null" />.</exception>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// var square = Memoizer.Memoize<int, int>(n => ExpensiveSquare(n));
    ///
    /// int a = square(8); // runs ExpensiveSquare
    /// int b = square(8); // returns the cached result
    ///]]>
    /// </code>
    /// </example>
    public static Func<TArg, TResult> Memoize<TArg, TResult>(Func<TArg, TResult> func)
        where TArg : notnull =>
        Memoize(func, null);

    /// <summary>
    /// Wraps a single-argument function with a cache that uses the specified equality comparer for its argument.
    /// </summary>
    /// <typeparam name="TArg">The argument type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="func">The function to memoize.</param>
    /// <param name="comparer">
    /// The comparer used to compare arguments, or <see langword="null" /> to use the default comparer.
    /// </param>
    /// <returns>A memoizing wrapper around <paramref name="func" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="func" /> is <see langword="null" />.</exception>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// // Case-insensitive cache: "Hello" and "HELLO" resolve to one entry.
    /// var lengthOf = Memoizer.Memoize<string, int>(s => s.Length, StringComparer.OrdinalIgnoreCase);
    ///
    /// int x = lengthOf("Hello"); // computed
    /// int y = lengthOf("HELLO"); // cache hit
    ///]]>
    /// </code>
    /// </example>
    public static Func<TArg, TResult> Memoize<TArg, TResult>(Func<TArg, TResult> func, IEqualityComparer<TArg>? comparer)
        where TArg : notnull
    {
        ThrowHelper.ThrowIfNull(func);

        var cache = new ConcurrentDictionary<TArg, TResult>(comparer ?? EqualityComparer<TArg>.Default);
        return arg => cache.GetOrAdd(arg, func);
    }

    /// <summary>
    /// Wraps a two-argument function with a cache keyed on the pair of arguments.
    /// </summary>
    /// <typeparam name="T1">The first argument type.</typeparam>
    /// <typeparam name="T2">The second argument type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="func">The function to memoize.</param>
    /// <returns>A memoizing wrapper around <paramref name="func" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="func" /> is <see langword="null" />.</exception>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// // Cache keyed on the (x, y) argument pair.
    /// var add = Memoizer.Memoize<int, int, int>((x, y) => SlowAdd(x, y));
    ///
    /// int s1 = add(3, 4); // computed
    /// int s2 = add(3, 4); // cache hit
    ///]]>
    /// </code>
    /// </example>
    public static Func<T1, T2, TResult> Memoize<T1, T2, TResult>(Func<T1, T2, TResult> func)
        where T1 : notnull
        where T2 : notnull
    {
        ThrowHelper.ThrowIfNull(func);

        var cache = new ConcurrentDictionary<(T1, T2), TResult>();
        return (arg1, arg2) => cache.GetOrAdd((arg1, arg2), key => func(key.Item1, key.Item2));
    }
}
