// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.Aggregate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Extensions;

public static partial class IEnumerableExtensions
{
    /// <summary>
    /// Applies an accumulator function over a sequence using the specified seed as the initial accumulator value,
    /// incorporating the element's index.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements contained in the sequence.</typeparam>
    /// <typeparam name="TAccumulate">The type of the accumulator value.</typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}" /> to aggregate over.</param>
    /// <param name="seed">The initial accumulator value.</param>
    /// <param name="func">
    /// An accumulator function invoked on each element. The first argument is the running aggregate, the second is the
    /// current element, and the third is the zero-based index of the current element.
    /// </param>
    /// <returns>The final accumulator value.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> or <paramref name="func" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// If <paramref name="source" /> is empty, <paramref name="seed" /> is returned unchanged and
    /// <paramref name="func" /> is not invoked.
    /// </para>
    /// <para>
    /// The index starts at zero and increments by one for each element visited. The counter is incremented using a
    /// checked operation so overflow throws rather than wraps.
    /// </para>
    /// </remarks>
    public static TAccumulate Aggregate<TSource, TAccumulate>(
        this IEnumerable<TSource> source,
        TAccumulate seed,
        Func<TAccumulate, TSource, int, TAccumulate> func)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(func);

        int index = -1;
        TAccumulate value = seed;
        foreach (TSource item in source)
        {
            index = checked(index + 1);
            value = func(value, item, index);
        }

        return value;
    }

    /// <summary>
    /// Applies an accumulator function over a sequence using the specified seed as the initial accumulator value,
    /// incorporating the element's index, and projects the final accumulator value through the supplied result
    /// selector.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements contained in the sequence.</typeparam>
    /// <typeparam name="TAccumulate">The type of the accumulator value.</typeparam>
    /// <typeparam name="TResult">The type of the resulting value.</typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}" /> to aggregate over.</param>
    /// <param name="seed">The initial accumulator value.</param>
    /// <param name="func">
    /// An accumulator function invoked on each element. The first argument is the running aggregate, the second is the
    /// current element, and the third is the zero-based index of the current element.
    /// </param>
    /// <param name="resultSelector">
    /// A function that transforms the final accumulator value into the returned result.
    /// </param>
    /// <returns>
    /// The value produced by applying <paramref name="resultSelector" /> to the final accumulator value.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" />, <paramref name="func" />, or <paramref name="resultSelector" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// If <paramref name="source" /> is empty, <paramref name="resultSelector" /> is applied to
    /// <paramref name="seed" />.
    /// </para>
    /// </remarks>
    public static TResult Aggregate<TSource, TAccumulate, TResult>(
        this IEnumerable<TSource> source,
        TAccumulate seed,
        Func<TAccumulate, TSource, int, TAccumulate> func,
        Func<TAccumulate, TResult> resultSelector)
    {
        ThrowHelper.ThrowIfNull(resultSelector);

        return resultSelector(source.Aggregate(seed, func));
    }

    /// <summary>
    /// Applies two accumulator functions over a sequence in a single pass and returns the final accumulator values as a
    /// tuple.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements contained in the sequence.</typeparam>
    /// <typeparam name="T1">The type of the first accumulator value.</typeparam>
    /// <typeparam name="T2">The type of the second accumulator value.</typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}" /> to aggregate over.</param>
    /// <param name="seed1">The initial value of the first accumulator.</param>
    /// <param name="seed2">The initial value of the second accumulator.</param>
    /// <param name="func1">An accumulator function invoked on each element for the first accumulator.</param>
    /// <param name="func2">An accumulator function invoked on each element for the second accumulator.</param>
    /// <returns>A tuple containing the final values of both accumulators.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" />, <paramref name="func1" />, or <paramref name="func2" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The sequence is enumerated exactly once. For each element, <paramref name="func1" /> is invoked before
    /// <paramref name="func2" />. If <paramref name="source" /> is empty, the seeds are returned unchanged.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// var (min, max) = new[] { 3, 1, 4, 1, 5, 9, 2, 6 }.Aggregate(
    ///     seed1: int.MaxValue,
    ///     seed2: int.MinValue,
    ///     func1: (acc, x) => Math.Min(acc, x),
    ///     func2: (acc, x) => Math.Max(acc, x)); // min == 1, max == 9
    ///]]>
    /// </code>
    /// </example>
    public static (T1, T2) Aggregate<TSource, T1, T2>(
        this IEnumerable<TSource> source,
        T1 seed1,
        T2 seed2,
        Func<T1, TSource, T1> func1,
        Func<T2, TSource, T2> func2)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(func1);
        ThrowHelper.ThrowIfNull(func2);

        T1 value1 = seed1;
        T2 value2 = seed2;
        foreach (TSource item in source)
        {
            value1 = func1(value1, item);
            value2 = func2(value2, item);
        }

        return (value1, value2);
    }

    /// <summary>
    /// Applies two accumulator functions over a sequence in a single pass, incorporating the element's index, and
    /// returns the final accumulator values as a tuple.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements contained in the sequence.</typeparam>
    /// <typeparam name="T1">The type of the first accumulator value.</typeparam>
    /// <typeparam name="T2">The type of the second accumulator value.</typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}" /> to aggregate over.</param>
    /// <param name="seed1">The initial value of the first accumulator.</param>
    /// <param name="seed2">The initial value of the second accumulator.</param>
    /// <param name="func1">
    /// An accumulator function invoked on each element for the first accumulator; receives the element's index as its
    /// third argument.
    /// </param>
    /// <param name="func2">
    /// An accumulator function invoked on each element for the second accumulator; receives the element's index as its
    /// third argument.
    /// </param>
    /// <returns>A tuple containing the final values of both accumulators.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" />, <paramref name="func1" />, or <paramref name="func2" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The sequence is enumerated exactly once. Both functions receive the same zero-based index for a given element
    /// and <paramref name="func1" /> is invoked before <paramref name="func2" />. The counter is incremented using a
    /// checked operation so overflow throws rather than wraps.
    /// </para>
    /// </remarks>
    public static (T1, T2) Aggregate<TSource, T1, T2>(
        this IEnumerable<TSource> source,
        T1 seed1,
        T2 seed2,
        Func<T1, TSource, int, T1> func1,
        Func<T2, TSource, int, T2> func2)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(func1);
        ThrowHelper.ThrowIfNull(func2);

        int index = -1;
        T1 value1 = seed1;
        T2 value2 = seed2;
        foreach (TSource item in source)
        {
            index = checked(index + 1);
            value1 = func1(value1, item, index);
            value2 = func2(value2, item, index);
        }

        return (value1, value2);
    }

    /// <summary>
    /// Applies two accumulator functions over a sequence in a single pass and projects the final accumulator values
    /// through the supplied result selector.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements contained in the sequence.</typeparam>
    /// <typeparam name="T1">The type of the first accumulator value.</typeparam>
    /// <typeparam name="T2">The type of the second accumulator value.</typeparam>
    /// <typeparam name="TResult">The type of the resulting value.</typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}" /> to aggregate over.</param>
    /// <param name="seed1">The initial value of the first accumulator.</param>
    /// <param name="seed2">The initial value of the second accumulator.</param>
    /// <param name="func1">An accumulator function invoked on each element for the first accumulator.</param>
    /// <param name="func2">An accumulator function invoked on each element for the second accumulator.</param>
    /// <param name="resultSelector">
    /// A function that transforms the final accumulator values into the returned result.
    /// </param>
    /// <returns>
    /// The value produced by applying <paramref name="resultSelector" /> to the final accumulator values.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" />, <paramref name="func1" />, <paramref name="func2" />, or
    /// <paramref name="resultSelector" /> is <see langword="null" />.
    /// </exception>
    public static TResult Aggregate<TSource, T1, T2, TResult>(
        this IEnumerable<TSource> source,
        T1 seed1,
        T2 seed2,
        Func<T1, TSource, T1> func1,
        Func<T2, TSource, T2> func2,
        Func<T1, T2, TResult> resultSelector)
    {
        ThrowHelper.ThrowIfNull(resultSelector);

        (T1 v1, T2 v2) = source.Aggregate(seed1, seed2, func1, func2);
        return resultSelector(v1, v2);
    }

    /// <summary>
    /// Applies two accumulator functions over a sequence in a single pass, incorporating the element's index, and
    /// projects the final accumulator values through the supplied result selector.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements contained in the sequence.</typeparam>
    /// <typeparam name="T1">The type of the first accumulator value.</typeparam>
    /// <typeparam name="T2">The type of the second accumulator value.</typeparam>
    /// <typeparam name="TResult">The type of the resulting value.</typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}" /> to aggregate over.</param>
    /// <param name="seed1">The initial value of the first accumulator.</param>
    /// <param name="seed2">The initial value of the second accumulator.</param>
    /// <param name="func1">
    /// An accumulator function invoked on each element for the first accumulator; receives the element's index as its
    /// third argument.
    /// </param>
    /// <param name="func2">
    /// An accumulator function invoked on each element for the second accumulator; receives the element's index as its
    /// third argument.
    /// </param>
    /// <param name="resultSelector">
    /// A function that transforms the final accumulator values into the returned result.
    /// </param>
    /// <returns>
    /// The value produced by applying <paramref name="resultSelector" /> to the final accumulator values.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" />, <paramref name="func1" />, <paramref name="func2" />, or
    /// <paramref name="resultSelector" /> is <see langword="null" />.
    /// </exception>
    public static TResult Aggregate<TSource, T1, T2, TResult>(
        this IEnumerable<TSource> source,
        T1 seed1,
        T2 seed2,
        Func<T1, TSource, int, T1> func1,
        Func<T2, TSource, int, T2> func2,
        Func<T1, T2, TResult> resultSelector)
    {
        ThrowHelper.ThrowIfNull(resultSelector);

        (T1 v1, T2 v2) = source.Aggregate(seed1, seed2, func1, func2);
        return resultSelector(v1, v2);
    }

    /// <summary>
    /// Applies three accumulator functions over a sequence in a single pass and returns the final accumulator values as
    /// a tuple.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements contained in the sequence.</typeparam>
    /// <typeparam name="T1">The type of the first accumulator value.</typeparam>
    /// <typeparam name="T2">The type of the second accumulator value.</typeparam>
    /// <typeparam name="T3">The type of the third accumulator value.</typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}" /> to aggregate over.</param>
    /// <param name="seed1">The initial value of the first accumulator.</param>
    /// <param name="seed2">The initial value of the second accumulator.</param>
    /// <param name="seed3">The initial value of the third accumulator.</param>
    /// <param name="func1">An accumulator function invoked on each element for the first accumulator.</param>
    /// <param name="func2">An accumulator function invoked on each element for the second accumulator.</param>
    /// <param name="func3">An accumulator function invoked on each element for the third accumulator.</param>
    /// <returns>A tuple containing the final values of the three accumulators.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" />, <paramref name="func1" />, <paramref name="func2" />, or
    /// <paramref name="func3" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The sequence is enumerated exactly once. For each element, <paramref name="func1" />, <paramref name="func2" />,
    /// and <paramref name="func3" /> are invoked in declaration order. If <paramref name="source" /> is empty, the
    /// seeds are returned unchanged.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// var (sum, count, max) = new[] { 3, 1, 4, 1, 5, 9, 2, 6 }.Aggregate(
    ///     seed1: 0,
    ///     seed2: 0,
    ///     seed3: int.MinValue,
    ///     func1: (acc, x) => acc + x,
    ///     func2: (acc, _) => acc + 1,
    ///     func3: (acc, x) => Math.Max(acc, x)); // sum == 31, count == 8, max == 9
    ///]]>
    /// </code>
    /// </example>
    public static (T1, T2, T3) Aggregate<TSource, T1, T2, T3>(
        this IEnumerable<TSource> source,
        T1 seed1,
        T2 seed2,
        T3 seed3,
        Func<T1, TSource, T1> func1,
        Func<T2, TSource, T2> func2,
        Func<T3, TSource, T3> func3)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(func1);
        ThrowHelper.ThrowIfNull(func2);
        ThrowHelper.ThrowIfNull(func3);

        T1 value1 = seed1;
        T2 value2 = seed2;
        T3 value3 = seed3;
        foreach (TSource item in source)
        {
            value1 = func1(value1, item);
            value2 = func2(value2, item);
            value3 = func3(value3, item);
        }

        return (value1, value2, value3);
    }

    /// <summary>
    /// Applies three accumulator functions over a sequence in a single pass, incorporating the element's index, and
    /// returns the final accumulator values as a tuple.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements contained in the sequence.</typeparam>
    /// <typeparam name="T1">The type of the first accumulator value.</typeparam>
    /// <typeparam name="T2">The type of the second accumulator value.</typeparam>
    /// <typeparam name="T3">The type of the third accumulator value.</typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}" /> to aggregate over.</param>
    /// <param name="seed1">The initial value of the first accumulator.</param>
    /// <param name="seed2">The initial value of the second accumulator.</param>
    /// <param name="seed3">The initial value of the third accumulator.</param>
    /// <param name="func1">
    /// An accumulator function invoked on each element for the first accumulator; receives the element's index as its
    /// third argument.
    /// </param>
    /// <param name="func2">
    /// An accumulator function invoked on each element for the second accumulator; receives the element's index as its
    /// third argument.
    /// </param>
    /// <param name="func3">
    /// An accumulator function invoked on each element for the third accumulator; receives the element's index as its
    /// third argument.
    /// </param>
    /// <returns>A tuple containing the final values of the three accumulators.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" />, <paramref name="func1" />, <paramref name="func2" />, or
    /// <paramref name="func3" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The sequence is enumerated exactly once. All three functions receive the same zero-based index for a given
    /// element and are invoked in declaration order. The counter is incremented using a checked operation so overflow
    /// throws rather than wraps.
    /// </para>
    /// </remarks>
    public static (T1, T2, T3) Aggregate<TSource, T1, T2, T3>(
        this IEnumerable<TSource> source,
        T1 seed1,
        T2 seed2,
        T3 seed3,
        Func<T1, TSource, int, T1> func1,
        Func<T2, TSource, int, T2> func2,
        Func<T3, TSource, int, T3> func3)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(func1);
        ThrowHelper.ThrowIfNull(func2);
        ThrowHelper.ThrowIfNull(func3);

        int index = -1;
        T1 value1 = seed1;
        T2 value2 = seed2;
        T3 value3 = seed3;
        foreach (TSource item in source)
        {
            index = checked(index + 1);
            value1 = func1(value1, item, index);
            value2 = func2(value2, item, index);
            value3 = func3(value3, item, index);
        }

        return (value1, value2, value3);
    }

    /// <summary>
    /// Applies three accumulator functions over a sequence in a single pass and projects the final accumulator values
    /// through the supplied result selector.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements contained in the sequence.</typeparam>
    /// <typeparam name="T1">The type of the first accumulator value.</typeparam>
    /// <typeparam name="T2">The type of the second accumulator value.</typeparam>
    /// <typeparam name="T3">The type of the third accumulator value.</typeparam>
    /// <typeparam name="TResult">The type of the resulting value.</typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}" /> to aggregate over.</param>
    /// <param name="seed1">The initial value of the first accumulator.</param>
    /// <param name="seed2">The initial value of the second accumulator.</param>
    /// <param name="seed3">The initial value of the third accumulator.</param>
    /// <param name="func1">An accumulator function invoked on each element for the first accumulator.</param>
    /// <param name="func2">An accumulator function invoked on each element for the second accumulator.</param>
    /// <param name="func3">An accumulator function invoked on each element for the third accumulator.</param>
    /// <param name="resultSelector">
    /// A function that transforms the final accumulator values into the returned result.
    /// </param>
    /// <returns>
    /// The value produced by applying <paramref name="resultSelector" /> to the final accumulator values.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" />, <paramref name="func1" />, <paramref name="func2" />,
    /// <paramref name="func3" />, or <paramref name="resultSelector" /> is <see langword="null" />.
    /// </exception>
    public static TResult Aggregate<TSource, T1, T2, T3, TResult>(
        this IEnumerable<TSource> source,
        T1 seed1,
        T2 seed2,
        T3 seed3,
        Func<T1, TSource, T1> func1,
        Func<T2, TSource, T2> func2,
        Func<T3, TSource, T3> func3,
        Func<T1, T2, T3, TResult> resultSelector)
    {
        ThrowHelper.ThrowIfNull(resultSelector);

        (T1 v1, T2 v2, T3 v3) = source.Aggregate(seed1, seed2, seed3, func1, func2, func3);
        return resultSelector(v1, v2, v3);
    }

    /// <summary>
    /// Applies three accumulator functions over a sequence in a single pass, incorporating the element's index, and
    /// projects the final accumulator values through the supplied result selector.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements contained in the sequence.</typeparam>
    /// <typeparam name="T1">The type of the first accumulator value.</typeparam>
    /// <typeparam name="T2">The type of the second accumulator value.</typeparam>
    /// <typeparam name="T3">The type of the third accumulator value.</typeparam>
    /// <typeparam name="TResult">The type of the resulting value.</typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}" /> to aggregate over.</param>
    /// <param name="seed1">The initial value of the first accumulator.</param>
    /// <param name="seed2">The initial value of the second accumulator.</param>
    /// <param name="seed3">The initial value of the third accumulator.</param>
    /// <param name="func1">
    /// An accumulator function invoked on each element for the first accumulator; receives the element's index as its
    /// third argument.
    /// </param>
    /// <param name="func2">
    /// An accumulator function invoked on each element for the second accumulator; receives the element's index as its
    /// third argument.
    /// </param>
    /// <param name="func3">
    /// An accumulator function invoked on each element for the third accumulator; receives the element's index as its
    /// third argument.
    /// </param>
    /// <param name="resultSelector">
    /// A function that transforms the final accumulator values into the returned result.
    /// </param>
    /// <returns>
    /// The value produced by applying <paramref name="resultSelector" /> to the final accumulator values.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" />, <paramref name="func1" />, <paramref name="func2" />,
    /// <paramref name="func3" />, or <paramref name="resultSelector" /> is <see langword="null" />.
    /// </exception>
    public static TResult Aggregate<TSource, T1, T2, T3, TResult>(
        this IEnumerable<TSource> source,
        T1 seed1,
        T2 seed2,
        T3 seed3,
        Func<T1, TSource, int, T1> func1,
        Func<T2, TSource, int, T2> func2,
        Func<T3, TSource, int, T3> func3,
        Func<T1, T2, T3, TResult> resultSelector)
    {
        ThrowHelper.ThrowIfNull(resultSelector);

        (T1 v1, T2 v2, T3 v3) = source.Aggregate(seed1, seed2, seed3, func1, func2, func3);
        return resultSelector(v1, v2, v3);
    }
}
