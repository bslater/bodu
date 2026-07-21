// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.CountOrDefault.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Extensions;

public static partial class IEnumerableExtensions
{
    /// <summary>
    /// Gets the number of elements in a non-generic <see cref="IEnumerable" /> sequence, using collection fast-paths
    /// where possible.
    /// </summary>
    /// <param name="source">The sequence to count. Must not be <see langword="null" />.</param>
    /// <returns>The number of elements in the sequence.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// When <paramref name="source" /> implements neither <see cref="ICollection" /> nor
    /// <see cref="IReadOnlyCollection{T}" />, the sequence is fully enumerated to produce the count — a one-shot
    /// source is consumed by the call.
    /// </remarks>
    public static int CountOrDefault(this IEnumerable source)
    {
        ThrowHelper.ThrowIfNull(source);

        if (source is ICollection col)
            return col.Count;

        if (source is IReadOnlyCollection<object> roGeneric)
            return roGeneric.Count;

        // Fallback: force enumeration through the non-generic enumerator. Current is never read, so value-type
        // elements are not boxed per step (unlike a Cast<object>().Count() pipeline).
        int count = 0;
        IEnumerator enumerator = source.GetEnumerator();
        try
        {
            while (enumerator.MoveNext())
                count++;
        }
        finally
        {
            (enumerator as IDisposable)?.Dispose();
        }

        return count;
    }
}