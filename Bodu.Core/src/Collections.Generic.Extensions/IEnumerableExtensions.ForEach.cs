// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.ForEach.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Extensions;

public static partial class IEnumerableExtensions
{
    /// <summary>
    /// Invokes <paramref name="action" /> once for each element of <paramref name="source" />, in sequence order.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <param name="source">The sequence whose elements are passed to <paramref name="action" />.</param>
    /// <param name="action">The delegate invoked for each element.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> or <paramref name="action" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// Execution is eager: <paramref name="source" /> is enumerated to completion before the method returns, and is
    /// enumerated exactly once.
    /// </remarks>
    public static void ForEach<TSource>(this IEnumerable<TSource> source, Action<TSource> action)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(action);

        foreach (TSource item in source)
            action(item);
    }
}
