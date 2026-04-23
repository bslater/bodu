// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.Aggregate.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Bodu.Collections.Generic.Extensions;

public static partial class IEnumerableExtensions
{
    /// <summary>
    /// Applies an accumulator function over a sequence, incorporating the element's index.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements contained in the sequence.</typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}"/> to aggregate over.</param>
    /// <param name="func">
    /// An accumulator function invoked on each element. The first argument is the running aggregate, the second is the current element,
    /// and the third is the zero-based index of the current element.
    /// </param>
    /// <returns>The final accumulator value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="func"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="source"/> contains no elements.</exception>
    /// <remarks>
    /// <para>
    /// This overload uses <see langword="default"/>(<typeparamref name="TSource"/>) as the initial aggregate value and throws if the
    /// sequence is empty, matching the shape of <see cref="System.Linq.Enumerable.Aggregate{TSource}(IEnumerable{TSource}, Func{TSource, TSource, TSource})"/>
    /// but exposing the element's index as an additional argument.
    /// </para>
    /// <para>
    /// The index starts at zero and increments by one for each element visited. For very long sequences the counter is incremented
    /// using a checked operation so overflow throws rather than wraps.
    /// </para>
    /// </remarks>
    public static TSource Aggregate<TSource>(
        this IEnumerable<TSource> source,
        Func<TSource, TSource, int, TSource> func)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(func);

        using IEnumerator<TSource> enumerator = source.GetEnumerator();
        if (!enumerator.MoveNext())
            throw new InvalidOperationException(ResourceStrings.InvalidOperation_EmptySequence);

        int index = -1;
        TSource value = default!;
        do
        {
            index = checked(index + 1);
            value = func(value, enumerator.Current, index);
        }
        while (enumerator.MoveNext());

        return value;
    }

    /// <summary>
    /// Applies an accumulator function over a sequence using the specified seed as the initial accumulator value, incorporating the
    /// element's index.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements contained in the sequence.</typeparam>
    /// <typeparam name="TAccumulate">The type of the accumulator value.</typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}"/> to aggregate over.</param>
    /// <param name="seed">The initial accumulator value.</param>
    /// <param name="func">
    /// An accumulator function invoked on each element. The first argument is the running aggregate, the second is the current element,
    /// and the third is the zero-based index of the current element.
    /// </param>
    /// <returns>The final accumulator value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="func"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// If <paramref name="source"/> is empty, <paramref name="seed"/> is returned unchanged and <paramref name="func"/> is not invoked.
    /// </para>
    /// <para>
    /// The index starts at zero and increments by one for each element visited. The counter is incremented using a checked operation so
    /// overflow throws rather than wraps.
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
    /// Applies an accumulator function over a sequence using the specified seed as the initial accumulator value, incorporating the
    /// element's index, and projects the final accumulator value through the supplied result selector.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements contained in the sequence.</typeparam>
    /// <typeparam name="TAccumulate">The type of the accumulator value.</typeparam>
    /// <typeparam name="TResult">The type of the resulting value.</typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}"/> to aggregate over.</param>
    /// <param name="seed">The initial accumulator value.</param>
    /// <param name="func">
    /// An accumulator function invoked on each element. The first argument is the running aggregate, the second is the current element,
    /// and the third is the zero-based index of the current element.
    /// </param>
    /// <param name="resultSelector">A function that transforms the final accumulator value into the returned result.</param>
    /// <returns>The value produced by applying <paramref name="resultSelector"/> to the final accumulator value.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source"/>, <paramref name="func"/>, or <paramref name="resultSelector"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// If <paramref name="source"/> is empty, <paramref name="resultSelector"/> is applied to <paramref name="seed"/>.
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
}
