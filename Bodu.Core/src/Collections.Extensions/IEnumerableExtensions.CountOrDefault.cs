// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.CountOrDefault.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Extensions;

public static partial class IEnumerableExtensions
{
    /// <summary>
    /// Gets the number of elements in a non-generic <see cref="IEnumerable"/> sequence, using collection fast-paths where possible.
    /// </summary>
    /// <param name="source">The sequence to count.</param>
    /// <returns>The number of elements in the sequence.</returns>
    public static int CountOrDefault(this IEnumerable source)
    {
        if (source is ICollection col)
            return col.Count;

        if (source is IReadOnlyCollection<object> roGeneric)
            return roGeneric.Count;

        return source.Cast<object>().Count(); // fallback: force enumeration
    }
}