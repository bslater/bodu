// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.Interleave.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Extensions;

public static partial class IEnumerableExtensions
{
    /// <summary>
    /// Interleaves the source sequence with one or more additional sequences, taking one element from each sequence in
    /// turn and skipping sequences as they are exhausted.
    /// </summary>
    /// <typeparam name="TSource">The type of elements in the sequences.</typeparam>
    /// <param name="first">The first sequence, which contributes the first element of each round.</param>
    /// <param name="others">
    /// The additional sequences to interleave after <paramref name="first" />. Must not contain <see langword="null" />
    /// elements.
    /// </param>
    /// <returns>
    /// A sequence containing every element of every input, ordered in rounds: each round takes one element from each
    /// not-yet-exhausted input, starting with <paramref name="first" /> and continuing through
    /// <paramref name="others" /> in order. The result length is the sum of the input lengths.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="first" /> or <paramref name="others" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="others" /> contains a <see langword="null" /> element.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method uses deferred execution for the elements, but validates its arguments eagerly: a
    /// <see langword="null" /> entry in <paramref name="others" /> throws at the call site, not during iteration. Each
    /// input is enumerated exactly once and its enumerator is disposed, including when the result is abandoned early.
    /// </para>
    /// <para>
    /// Inputs of unequal length do not truncate the result and do not throw: once an input is exhausted it is skipped
    /// and the remaining inputs continue round-robin until all are exhausted. This is the round-robin scheduling shape
    /// — fair rotation over the surviving inputs — so no separate <c>RoundRobin</c> operator is provided.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// // Rounds continue with the surviving inputs once shorter ones are exhausted.
    /// int[] result = new[] { 1, 2, 3 }.Interleave(new[] { 10, 20 }, new[] { 100 }).ToArray();
    /// // => result == [ 1, 10, 100, 2, 20, 3 ]
    ///]]>
    /// </code>
    /// </example>
    public static IEnumerable<TSource> Interleave<TSource>(
        this IEnumerable<TSource> first,
        params IEnumerable<TSource>[] others)
    {
        ThrowHelper.ThrowIfNull(first);
        ThrowHelper.ThrowIfNull(others);
        if (Array.IndexOf(others, null) >= 0) throw new ArgumentException(ResourceStrings.Arg_Invalid_ParamsContainsNullValue, nameof(others));

        return Iterator();

        IEnumerable<TSource> Iterator()
        {
            var enumerators = new List<IEnumerator<TSource>>(others.Length + 1);

            try
            {
                enumerators.Add(first.GetEnumerator());
                foreach (IEnumerable<TSource> other in others)
                    enumerators.Add(other.GetEnumerator());

                // Round-robin over the surviving enumerators: an exhausted input is disposed and removed so later
                // rounds rotate only over the inputs that still have elements.
                while (enumerators.Count > 0)
                {
                    int index = 0;

                    while (index < enumerators.Count)
                    {
                        if (enumerators[index].MoveNext())
                        {
                            yield return enumerators[index].Current;
                            index++;
                        }
                        else
                        {
                            enumerators[index].Dispose();
                            enumerators.RemoveAt(index);
                        }
                    }
                }
            }
            finally
            {
                foreach (IEnumerator<TSource> enumerator in enumerators)
                    enumerator.Dispose();
            }
        }
    }
}
