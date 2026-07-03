// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.ChunkBy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Extensions;

public static partial class IEnumerableExtensions
{
    /// <summary>
    /// Groups <em>adjacent</em> elements of the source sequence that share the same key, starting a new group whenever
    /// the key changes, using the default equality comparer.
    /// </summary>
    /// <typeparam name="TSource">The type of elements in the source sequence.</typeparam>
    /// <typeparam name="TKey">The type of the key produced for each element.</typeparam>
    /// <param name="source">The source sequence to group.</param>
    /// <param name="keySelector">A function that produces the grouping key for each element.</param>
    /// <returns>
    /// A sequence of <c>(Key, Items)</c> tuples where <c>Items</c> is a stable snapshot of a maximal run of adjacent
    /// elements sharing <c>Key</c>. The result is empty when <paramref name="source" /> is empty.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> or <paramref name="keySelector" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// This method uses deferred execution and enumerates the source exactly once. Keys are compared with
    /// <see cref="EqualityComparer{T}.Default" />.
    /// </remarks>
    public static IEnumerable<(TKey Key, IReadOnlyList<TSource> Items)> ChunkBy<TSource, TKey>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector) =>
        source.ChunkBy(keySelector, null);

    /// <summary>
    /// Groups <em>adjacent</em> elements of the source sequence that share the same key, starting a new group whenever
    /// the key changes, using the specified equality comparer.
    /// </summary>
    /// <typeparam name="TSource">The type of elements in the source sequence.</typeparam>
    /// <typeparam name="TKey">The type of the key produced for each element.</typeparam>
    /// <param name="source">The source sequence to group.</param>
    /// <param name="keySelector">A function that produces the grouping key for each element.</param>
    /// <param name="comparer">
    /// The comparer used to decide whether adjacent keys are equal, or <see langword="null" /> to use
    /// <see cref="EqualityComparer{T}.Default" />.
    /// </param>
    /// <returns>
    /// A sequence of <c>(Key, Items)</c> tuples where <c>Items</c> is a stable snapshot of a maximal run of adjacent
    /// elements sharing <c>Key</c>. The result is empty when <paramref name="source" /> is empty.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> or <paramref name="keySelector" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method uses deferred execution and enumerates the source exactly once in a single forward pass, buffering
    /// only the current group. <paramref name="keySelector" /> is invoked exactly once per source element, and each
    /// yielded group is a stable snapshot that is not modified when the enumerator advances.
    /// </para>
    /// <para>
    /// This method differs from <see cref="System.Linq.Enumerable.GroupBy{TSource, TKey}(IEnumerable{TSource}, Func{TSource, TKey})" />:
    /// <c>GroupBy</c> gathers all elements sharing a key across the whole sequence, whereas this method uses adjacent
    /// equality only, so the same key value separated by a different key produces separate groups.
    /// </para>
    /// <para>
    /// When bounded over an infinite source, a group whose key never changes never yields; combine with
    /// <see cref="System.Linq.Enumerable.Take{TSource}(IEnumerable{TSource}, int)" /> only when keys are known to change.
    /// </para>
    /// </remarks>
    public static IEnumerable<(TKey Key, IReadOnlyList<TSource> Items)> ChunkBy<TSource, TKey>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        IEqualityComparer<TKey>? comparer)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(keySelector);

        comparer ??= EqualityComparer<TKey>.Default;

        return Iterator();

        IEnumerable<(TKey Key, IReadOnlyList<TSource> Items)> Iterator()
        {
            using IEnumerator<TSource> enumerator = source.GetEnumerator();

            if (!enumerator.MoveNext())
                yield break;

            TSource first = enumerator.Current;
            TKey currentKey = keySelector(first);
            var chunk = new List<TSource> { first };

            while (enumerator.MoveNext())
            {
                TSource item = enumerator.Current;
                TKey key = keySelector(item);

                if (!comparer.Equals(currentKey, key))
                {
                    yield return (currentKey, chunk.ToArray());
                    chunk.Clear();
                    currentKey = key;
                }

                chunk.Add(item);
            }

            yield return (currentKey, chunk.ToArray());
        }
    }
}
