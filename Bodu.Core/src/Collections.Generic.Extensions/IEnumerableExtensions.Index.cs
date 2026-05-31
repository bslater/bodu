// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.Index.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

#if !NET9_0_OR_GREATER

namespace Bodu.Collections.Generic.Extensions;

public static partial class IEnumerableExtensions
{
    /// <summary>
    /// Pairs each element of <paramref name="source" /> with its zero-based position in the sequence.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <param name="source">The sequence to pair with positional indexes.</param>
    /// <returns>
    /// A sequence of tuples, each holding the zero-based index and the element at that position, in order.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Execution is deferred: <paramref name="source" /> is not enumerated until the returned sequence is iterated,
    /// while the <see langword="null" /> argument check runs eagerly.
    /// </para>
    /// <para>
    /// This helper is compiled only for targets earlier than .NET 9, where the runtime introduces an equivalent
    /// <c>Enumerable.Index</c> method on the standard LINQ surface.
    /// </para>
    /// </remarks>
    public static IEnumerable<(int Index, TSource Item)> Index<TSource>(this IEnumerable<TSource> source)
    {
        ThrowHelper.ThrowIfNull(source);

        return Iterator(source);

        static IEnumerable<(int Index, TSource Item)> Iterator(IEnumerable<TSource> sequence)
        {
            var index = 0;
            foreach (TSource item in sequence)
                yield return (index++, item);
        }
    }
}

#endif
