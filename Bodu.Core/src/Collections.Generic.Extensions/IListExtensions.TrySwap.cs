// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IListExtensions.TrySwap.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Extensions;

public static partial class IListExtensions
{
    /// <summary>
    /// Attempts to swap the elements at the specified indexes in the list.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="list">The list to modify. Must not be <see langword="null" />.</param>
    /// <param name="indexA">The zero-based index of the first element to swap.</param>
    /// <param name="indexB">The zero-based index of the second element to swap.</param>
    /// <returns>
    /// <see langword="true" /> if the swap was performed or both indexes refer to the same element;
    /// <see langword="false" /> if either <paramref name="indexA" /> or <paramref name="indexB" /> is negative or
    /// greater than or equal to <see cref="ICollection{T}.Count" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="list" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// When <paramref name="indexA" /> and <paramref name="indexB" /> are equal, the method returns
    /// <see langword="true" /> without modifying the list.
    /// </remarks>
    public static bool TrySwap<T>(this IList<T> list, int indexA, int indexB)
    {
        ThrowHelper.ThrowIfNull(list);

        if (indexA < 0 || indexB < 0 || indexA >= list.Count || indexB >= list.Count)
            return false;

        if (indexA != indexB)
        {
            T temp = list[indexA];
            list[indexA] = list[indexB];
            list[indexB] = temp;
        }

        return true;
    }
}
