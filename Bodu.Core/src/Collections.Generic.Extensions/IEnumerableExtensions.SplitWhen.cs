// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.SplitWhen.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Extensions;

public static partial class IEnumerableExtensions
{
    /// <summary>
    /// Splits the source sequence into consecutive chunks, starting a new chunk whenever a boundary predicate over an
    /// adjacent pair of elements returns <see langword="true" />.
    /// </summary>
    /// <typeparam name="TSource">The type of elements in the source sequence.</typeparam>
    /// <param name="source">The source sequence to split.</param>
    /// <param name="shouldSplit">
    /// A predicate applied to each adjacent pair, receiving the previous and current elements. When it returns
    /// <see langword="true" />, the current element starts a new chunk.
    /// </param>
    /// <returns>
    /// A sequence of chunks, each a stable snapshot, that together preserve every source element in order. The result is
    /// empty when <paramref name="source" /> is empty.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> or <paramref name="shouldSplit" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method uses deferred execution and enumerates the source exactly once in a single forward pass, buffering
    /// only the current chunk. This detects transitions between adjacent elements, not separator values.
    /// </para>
    /// <para>
    /// <paramref name="shouldSplit" /> is invoked exactly once per adjacent pair — that is,
    /// <c>count - 1</c> times. Each yielded chunk is a stable snapshot and is not modified when the enumerator advances.
    /// </para>
    /// <para>
    /// When bounded over an infinite source, a chunk whose boundary never occurs never yields; combine with
    /// <see cref="System.Linq.Enumerable.Take{TSource}(IEnumerable{TSource}, int)" /> only when splits are known to occur.
    /// </para>
    /// </remarks>
    public static IEnumerable<IReadOnlyList<TSource>> SplitWhen<TSource>(
        this IEnumerable<TSource> source,
        Func<TSource, TSource, bool> shouldSplit)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(shouldSplit);

        return Iterator();

        IEnumerable<IReadOnlyList<TSource>> Iterator()
        {
            using IEnumerator<TSource> enumerator = source.GetEnumerator();

            if (!enumerator.MoveNext())
                yield break;

            var chunk = new List<TSource>();
            TSource previous = enumerator.Current;
            chunk.Add(previous);

            while (enumerator.MoveNext())
            {
                TSource current = enumerator.Current;

                if (shouldSplit(previous, current))
                {
                    yield return chunk.ToArray();
                    chunk.Clear();
                }

                chunk.Add(current);
                previous = current;
            }

            yield return chunk.ToArray();
        }
    }
}
