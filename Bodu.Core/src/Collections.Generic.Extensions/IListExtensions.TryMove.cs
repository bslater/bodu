// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IListExtensions.TryMove.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Extensions;

public static partial class IListExtensions
{
    /// <summary>
    /// Attempts to move an item from one index to another within the list.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="list">The list to modify. Must not be <see langword="null" />.</param>
    /// <param name="oldIndex">The zero-based index of the item to move.</param>
    /// <param name="newIndex">
    /// The zero-based insertion point in the original list before which the item will be placed. A value of <c>0</c>
    /// moves the item to the front. A value equal to <see cref="ICollection{T}.Count" /> moves the item to the end of
    /// the list.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if the move was performed or the item was already in the correct position;
    /// <see langword="false" /> if <paramref name="oldIndex" /> or <paramref name="newIndex" /> is out of range.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="list" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The <paramref name="newIndex" /> parameter represents an <em>insertion point</em> in the original list, not a
    /// final target index. The item is conceptually placed before the element currently at <paramref name="newIndex" />
    /// , after which all subsequent elements shift to accommodate it. This means the item's final index in the result
    /// list will be <paramref name="newIndex" /> when moving left (or to the same relative position), but
    /// <paramref name="newIndex" /> minus one when moving right.
    /// </para>
    /// <para>
    /// The method returns <see langword="true" /> without modifying the list when the move would produce no observable
    /// change — specifically when <paramref name="oldIndex" /> equals <paramref name="newIndex" /> (same position), or
    /// when <paramref name="oldIndex" /> equals <paramref name="newIndex" /> minus one (the item is already directly
    /// before the insertion point and shifting it right would leave it in the same position after the index adjustment
    /// for removal).
    /// </para>
    /// <para>
    /// Valid values for <paramref name="oldIndex" /> are in the range <c>[0, Count)</c>. Valid values for
    /// <paramref name="newIndex" /> are in the range <c>[0, Count]</c>, where a value of <c>Count</c> specifies
    /// insertion at the end of the list.
    /// </para>
    /// </remarks>
    public static bool TryMove<T>(this IList<T> list, int oldIndex, int newIndex)
    {
        ThrowHelper.ThrowIfNull(list);

        if (oldIndex < 0 || newIndex < 0 || oldIndex >= list.Count || newIndex > list.Count)
            return false;

        // No observable change: same position, or item is already directly before the insertion point.
        if (oldIndex == newIndex || oldIndex == newIndex - 1)
            return true;

        T item = list[oldIndex];
        list.RemoveAt(oldIndex);

        // Removal shifts all elements after oldIndex left by one; adjust the insertion point accordingly.
        if (newIndex > oldIndex)
            newIndex--;

        list.Insert(newIndex, item);
        return true;
    }
}
