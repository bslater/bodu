// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RangeArrayCore.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Internal;

/// <summary>
/// Provides the sorted parallel-array machinery shared by <see cref="RangeSet{T}" /> (two arrays: starts and ends) and
/// <see cref="RangeDictionary{TKey, TValue}" /> (three arrays: starts, ends, and values): containing-range lookup,
/// insertion-point shifting, removal compaction, and lock-step resizing.
/// </summary>
/// <remarks>
/// <para>
/// Each operation is implemented once over a single array (<see cref="InsertInto{T}" />, <see cref="RemoveFrom{T}" />,
/// <see cref="RemoveRangeFrom{T}" />) and composed per array count, so the two-array and three-array shapes cannot
/// drift apart.
/// </para>
/// <para>
/// The consumer retains ownership of validation, growth policy, version stamping, and the live count; mutating methods
/// take the count by reference and update it, while capacity must already accommodate an insertion.
/// </para>
/// </remarks>
internal static class RangeArrayCore
{
    /// <summary>
    /// Locates the index of the range that contains <paramref name="value" />, if any.
    /// </summary>
    /// <typeparam name="T">The endpoint type.</typeparam>
    /// <param name="starts">The sorted inclusive start endpoints.</param>
    /// <param name="ends">The exclusive end endpoints, parallel to <paramref name="starts" />.</param>
    /// <param name="count">The number of live entries.</param>
    /// <param name="value">The value to locate.</param>
    /// <param name="comparer">The comparer that orders endpoints.</param>
    /// <returns>The index of the containing range, or <c>-1</c> if no range contains the value.</returns>
    internal static int FindContainingIndex<T>(T[] starts, T[] ends, int count, T value, IComparer<T> comparer)
    {
        int index = SortedRangeSearch.UpperBound(starts, count, value, comparer) - 1;

        return index < 0 ? -1 : comparer.Compare(value, ends[index]) < 0 ? index : -1;
    }

    /// <summary>
    /// Inserts a range at the specified position across the two parallel arrays, shifting trailing entries right and
    /// incrementing <paramref name="count" />. Capacity must already be sufficient.
    /// </summary>
    /// <typeparam name="T">The endpoint type.</typeparam>
    /// <param name="starts">The sorted inclusive start endpoints.</param>
    /// <param name="ends">The exclusive end endpoints, parallel to <paramref name="starts" />.</param>
    /// <param name="count">The live entry count, incremented on return.</param>
    /// <param name="index">The insertion index.</param>
    /// <param name="start">The inclusive start to insert.</param>
    /// <param name="end">The exclusive end to insert.</param>
    internal static void InsertAt<T>(T[] starts, T[] ends, ref int count, int index, T start, T end)
    {
        InsertInto(starts, count, index, start);
        InsertInto(ends, count, index, end);
        count++;
    }

    /// <summary>
    /// Inserts a range entry at the specified position across the three parallel arrays, shifting trailing entries
    /// right and incrementing <paramref name="count" />. Capacity must already be sufficient.
    /// </summary>
    /// <typeparam name="T">The endpoint type.</typeparam>
    /// <typeparam name="TValue">The associated value type.</typeparam>
    /// <param name="starts">The sorted inclusive start endpoints.</param>
    /// <param name="ends">The exclusive end endpoints, parallel to <paramref name="starts" />.</param>
    /// <param name="values">The associated values, parallel to <paramref name="starts" />.</param>
    /// <param name="count">The live entry count, incremented on return.</param>
    /// <param name="index">The insertion index.</param>
    /// <param name="start">The inclusive start to insert.</param>
    /// <param name="end">The exclusive end to insert.</param>
    /// <param name="value">The value to insert.</param>
    internal static void InsertAt<T, TValue>(T[] starts, T[] ends, TValue[] values, ref int count, int index, T start, T end, TValue value)
    {
        InsertInto(starts, count, index, start);
        InsertInto(ends, count, index, end);
        InsertInto(values, count, index, value);
        count++;
    }

    /// <summary>
    /// Removes the entry at the specified index from the two parallel arrays, shifting trailing entries left, clearing
    /// the vacated slots, and decrementing <paramref name="count" />.
    /// </summary>
    /// <typeparam name="T">The endpoint type.</typeparam>
    /// <param name="starts">The sorted inclusive start endpoints.</param>
    /// <param name="ends">The exclusive end endpoints, parallel to <paramref name="starts" />.</param>
    /// <param name="count">The live entry count, decremented on return.</param>
    /// <param name="index">The index of the entry to remove.</param>
    internal static void RemoveAt<T>(T[] starts, T[] ends, ref int count, int index)
    {
        count--;
        RemoveFrom(starts, count, index);
        RemoveFrom(ends, count, index);
    }

    /// <summary>
    /// Removes the entry at the specified index from the three parallel arrays, shifting trailing entries left,
    /// clearing the vacated slots, and decrementing <paramref name="count" />.
    /// </summary>
    /// <typeparam name="T">The endpoint type.</typeparam>
    /// <typeparam name="TValue">The associated value type.</typeparam>
    /// <param name="starts">The sorted inclusive start endpoints.</param>
    /// <param name="ends">The exclusive end endpoints, parallel to <paramref name="starts" />.</param>
    /// <param name="values">The associated values, parallel to <paramref name="starts" />.</param>
    /// <param name="count">The live entry count, decremented on return.</param>
    /// <param name="index">The index of the entry to remove.</param>
    internal static void RemoveAt<T, TValue>(T[] starts, T[] ends, TValue[] values, ref int count, int index)
    {
        count--;
        RemoveFrom(starts, count, index);
        RemoveFrom(ends, count, index);
        RemoveFrom(values, count, index);
    }

    /// <summary>
    /// Removes <paramref name="removeCount" /> consecutive entries starting at <paramref name="index" /> from the two
    /// parallel arrays, shifting any trailing entries left, clearing the vacated tail slots, and reducing
    /// <paramref name="count" />.
    /// </summary>
    /// <typeparam name="T">The endpoint type.</typeparam>
    /// <param name="starts">The sorted inclusive start endpoints.</param>
    /// <param name="ends">The exclusive end endpoints, parallel to <paramref name="starts" />.</param>
    /// <param name="count">The live entry count, reduced by <paramref name="removeCount" /> on return.</param>
    /// <param name="index">The index of the first entry to remove.</param>
    /// <param name="removeCount">The number of entries to remove. Must be greater than zero.</param>
    internal static void RemoveRange<T>(T[] starts, T[] ends, ref int count, int index, int removeCount)
    {
        RemoveRangeFrom(starts, count, index, removeCount);
        RemoveRangeFrom(ends, count, index, removeCount);
        count -= removeCount;
    }

    /// <summary>
    /// Reallocates the two parallel arrays to the specified capacity.
    /// </summary>
    /// <typeparam name="T">The endpoint type.</typeparam>
    /// <param name="starts">The start-endpoint array reference to resize.</param>
    /// <param name="ends">The end-endpoint array reference to resize.</param>
    /// <param name="capacity">The new capacity.</param>
    internal static void Resize<T>(ref T[] starts, ref T[] ends, int capacity)
    {
        Array.Resize(ref starts, capacity);
        Array.Resize(ref ends, capacity);
    }

    /// <summary>
    /// Reallocates the three parallel arrays to the specified capacity.
    /// </summary>
    /// <typeparam name="T">The endpoint type.</typeparam>
    /// <typeparam name="TValue">The associated value type.</typeparam>
    /// <param name="starts">The start-endpoint array reference to resize.</param>
    /// <param name="ends">The end-endpoint array reference to resize.</param>
    /// <param name="values">The value array reference to resize.</param>
    /// <param name="capacity">The new capacity.</param>
    internal static void Resize<T, TValue>(ref T[] starts, ref T[] ends, ref TValue[] values, int capacity)
    {
        Array.Resize(ref starts, capacity);
        Array.Resize(ref ends, capacity);
        Array.Resize(ref values, capacity);
    }

    /// <summary>
    /// Inserts <paramref name="item" /> at <paramref name="index" /> in a single array holding
    /// <paramref name="count" /> live entries, shifting the trailing entries right by one.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="array">The array to insert into. Capacity must exceed <paramref name="count" />.</param>
    /// <param name="count">The number of live entries before the insertion.</param>
    /// <param name="index">The insertion index.</param>
    /// <param name="item">The element to insert.</param>
    private static void InsertInto<T>(T[] array, int count, int index, T item)
    {
        if (index < count)
            Array.Copy(array, index, array, index + 1, count - index);

        array[index] = item;
    }

    /// <summary>
    /// Removes the entry at <paramref name="index" /> from a single array whose live count has already been reduced to
    /// <paramref name="newCount" />, shifting the trailing entries left and clearing the vacated tail slot.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="array">The array to compact.</param>
    /// <param name="newCount">The live entry count after the removal.</param>
    /// <param name="index">The index of the entry to remove.</param>
    private static void RemoveFrom<T>(T[] array, int newCount, int index)
    {
        int moveCount = newCount - index;

        if (moveCount > 0)
            Array.Copy(array, index + 1, array, index, moveCount);

        array[newCount] = default!;
    }

    /// <summary>
    /// Removes <paramref name="removeCount" /> consecutive entries starting at <paramref name="index" /> from a single
    /// array holding <paramref name="count" /> live entries, shifting any trailing entries left and clearing the
    /// vacated tail slots.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="array">The array to compact.</param>
    /// <param name="count">The number of live entries before the removal.</param>
    /// <param name="index">The index of the first entry to remove.</param>
    /// <param name="removeCount">The number of entries to remove. Must be greater than zero.</param>
    private static void RemoveRangeFrom<T>(T[] array, int count, int index, int removeCount)
    {
        int moveCount = count - index - removeCount;

        if (moveCount > 0)
            Array.Copy(array, index + removeCount, array, index, moveCount);

        Array.Clear(array, count - removeCount, removeCount);
    }
}
