// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ArrayExtensions.Slice.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class ArrayExtensions
{
    /// <summary>
    /// Takes a slice of the specified array starting from a given index and extending to the end of the array.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the array.</typeparam>
    /// <param name="array">The source array containing the elements to slice.</param>
    /// <param name="index">The starting index in the source array from which the slice begins.</param>
    /// <returns>
    /// A new array containing elements from the <paramref name="array" /> starting from <paramref name="index" /> to the end.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="array" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index" /> is out of bounds.</exception>
    public static T[] Slice<T>(this T[] array, int index) => array.Slice(index, array?.Length - index ?? 0);

    /// <summary>
    /// Takes a slice of the specified array starting from a given index and extending for a specified number of elements.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the array.</typeparam>
    /// <param name="array">The source array containing the elements to slice.</param>
    /// <param name="index">The starting index in the source array from which the slice begins.</param>
    /// <param name="count">The number of elements to include in the slice.</param>
    /// <returns>A new array containing the sliced elements from the <paramref name="array" /> starting at <paramref name="index" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="array" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="index" /> or <paramref name="count" /> is less than zero.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="index" /> and <paramref name="count" /> describe an invalid range within <paramref name="array" />.
    /// </exception>
    public static T[] Slice<T>(this T[] array, int index, int count)
    {
        ThrowHelper.ThrowIfNull(array);
        ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(array, index, count);

        return array.SliceInternal(index, count);
    }

    /// <summary>
    /// Takes a slice of the specified array starting from a given index and extending for a specified number of elements. This method
    /// does not perform any validation checks (use with caution).
    /// </summary>
    /// <typeparam name="T">The type of the elements in the array.</typeparam>
    /// <param name="array">The one-dimensional array that contains the values to copy.</param>
    /// <param name="index">The starting index in the <paramref name="array" /> from which copying begins.</param>
    /// <param name="count">The number of elements to copy from the <paramref name="array" />.</param>
    /// <returns>A new array containing the copied elements from the <paramref name="array" /> starting from <paramref name="index" />.</returns>
    /// <remarks>This method is optimized and does not perform validation. Ensure the inputs are valid.</remarks>
    public static T[] SliceInternal<T>(this T[] array, int index, int count)
    {
        ThrowHelper.ThrowIfNull(array);
        ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(array, index, count);

        T[] result = new T[count];
        Array.Copy(array, index, result, 0, count);
        return result;
    }
}