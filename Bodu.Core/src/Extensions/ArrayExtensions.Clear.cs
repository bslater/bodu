// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ArrayExtensions.Clear.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class ArrayExtensions
{
    /// <summary>
    /// Sets all elements in a one-dimensional array to the default value of the element type.
    /// </summary>
    /// <typeparam name="T">The type of the elements of the array.</typeparam>
    /// <param name="array">The one-dimensional array to clear.</param>
    /// <exception cref="ArgumentNullException"><paramref name="array"/> is <see langword="null"/>.</exception>
    public static void Clear<T>(this T[] array)
    {
        ThrowHelper.ThrowIfNull(array);
        ClearInternal(array, 0, array.Length);
    }

    /// <summary>
    /// Sets all elements in a one-dimensional array to the default value of the element type, starting from the specified index to the
    /// end of the array.
    /// </summary>
    /// <typeparam name="T">The type of the elements of the array.</typeparam>
    /// <param name="array">The one-dimensional array to clear.</param>
    /// <param name="index">The zero-based index at which to begin clearing elements.</param>
    /// <exception cref="ArgumentNullException"><paramref name="array"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than 0 or greater than the length of <paramref name="array"/>.</exception>
    /// <remarks>
    /// This method clears elements starting from <paramref name="index"/> to the end of the array. The number of elements cleared is
    /// <c>array.Length - index</c>.
    /// </remarks>
    public static void Clear<T>(this T[] array, int index)
    {
        ThrowHelper.ThrowIfNull(array);
        ThrowHelper.ThrowIfIndexOutOfRange(index, array);
        ClearInternal(array, index, array.Length - index);
    }

    /// <summary>
    /// Sets a specified number of elements in a one-dimensional array to the default value of the element type, starting from a given index.
    /// </summary>
    /// <typeparam name="T">The type of the elements of the array.</typeparam>
    /// <param name="array">The one-dimensional array to clear.</param>
    /// <param name="index">The zero-based index at which to begin clearing elements.</param>
    /// <param name="count">The number of elements to clear.</param>
    /// <exception cref="ArgumentNullException"><paramref name="array"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="index"/> is less than 0 or greater than the length of <paramref name="array"/>. <br />
    /// -or- <br /><paramref name="count"/> is less than 0 or extends beyond the end of the array.
    /// </exception>
    /// <remarks>This method clears exactly <paramref name="count"/> elements starting at <paramref name="index"/>.</remarks>
    public static void Clear<T>(this T[] array, int index, int count)
    {
        ThrowHelper.ThrowIfNull(array);
        ThrowHelper.ThrowIfIndexOutOfRange(index, array);
        ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(array, index, count);
        ClearInternal(array, index, count);
    }

    /// <summary>
    /// Sets all elements in an <see cref="Array"/> to the default value of each element type.
    /// </summary>
    /// <param name="array">The array whose elements to clear.</param>
    /// <exception cref="ArgumentNullException"><paramref name="array"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="array"/> is not a single-dimensional array. <br />
    /// -or- <br /><paramref name="array"/> does not have a zero-based index.
    /// </exception>
    /// <remarks>This method supports only single-dimensional, zero-based arrays.</remarks>
    public static void Clear(this Array array)
    {
        ThrowHelper.ThrowIfNull(array);
        ThrowHelper.ThrowIfArrayMultidimensional(array);
        ThrowHelper.ThrowIfArrayIsNotZeroBased(array);
        ClearCore(array, 0, array.Length);
    }

    /// <summary>
    /// Sets all elements in an <see cref="Array"/> to the default value of each element type, starting from the specified index to the
    /// end of the array.
    /// </summary>
    /// <param name="array">The array whose elements to clear.</param>
    /// <param name="index">The zero-based index at which to begin clearing elements.</param>
    /// <exception cref="ArgumentNullException"><paramref name="array"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="array"/> is not a single-dimensional array. <br />
    /// -or- <br /><paramref name="array"/> does not have a zero-based index.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than 0 or greater than the length of <paramref name="array"/>.</exception>
    /// <remarks>Clears elements from <paramref name="index"/> to the end of the array.</remarks>
    public static void Clear(this Array array, int index)
    {
        ThrowHelper.ThrowIfNull(array);
        ThrowHelper.ThrowIfArrayMultidimensional(array);
        ThrowHelper.ThrowIfArrayIsNotZeroBased(array);
        ThrowHelper.ThrowIfIndexOutOfRange(index, array);
        ClearCore(array, index, array.Length - index);
    }

    /// <summary>
    /// Sets a specified number of elements in an <see cref="Array"/> to the default value of each element type, starting from a given index.
    /// </summary>
    /// <param name="array">The array whose elements to clear.</param>
    /// <param name="index">The zero-based index at which to begin clearing elements.</param>
    /// <param name="count">The number of elements to clear.</param>
    /// <exception cref="ArgumentNullException"><paramref name="array"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="array"/> is not a single-dimensional array. <br />
    /// -or- <br /><paramref name="array"/> does not have a zero-based index.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="index"/> is less than 0 or greater than the length of <paramref name="array"/>. <br />
    /// -or- <br /><paramref name="count"/> is less than 0 or extends beyond the end of the array.
    /// </exception>
    /// <remarks>Clears <paramref name="count"/> elements starting at <paramref name="index"/>.</remarks>
    public static void Clear(this Array array, int index, int count)
    {
        ThrowHelper.ThrowIfNull(array);
        ThrowHelper.ThrowIfArrayMultidimensional(array);
        ThrowHelper.ThrowIfArrayIsNotZeroBased(array);
        ThrowHelper.ThrowIfIndexOutOfRange(index, array);
        ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(array, index, count);
        ClearCore(array, index, count);
    }

    /// <summary>
    /// Clears a range of elements in a one-dimensional array without performing any validation on the parameters.
    /// </summary>
    /// <typeparam name="T">The type of the elements of the array.</typeparam>
    /// <param name="array">The one-dimensional array to clear. Must not be <see langword="null"/>.</param>
    /// <param name="index">The starting index. Must be in valid range.</param>
    /// <param name="count">The number of elements to clear. Must be non-negative and in range.</param>
    /// <remarks>
    /// This method assumes all parameters are valid and skips all validation checks. Intended for internal use where input validity is
    /// already ensured.
    /// </remarks>
    internal static void ClearInternal<T>(T[] array, int index, int count) =>
        Array.Clear(array, index, count);

    /// <summary>
    /// Clears a range of elements in a <see cref="System.Array"/> without performing any validation on the parameters.
    /// </summary>
    /// <param name="array">The array to clear. Must not be <see langword="null"/> and must be single-dimensional, zero-based.</param>
    /// <param name="index">The starting index. Must be in valid range.</param>
    /// <param name="count">The number of elements to clear. Must be non-negative and in range.</param>
    /// <remarks>
    /// This method assumes all parameters are valid and skips all validation checks. Intended for internal use where input validity is
    /// already ensured.
    /// </remarks>
    internal static void ClearCore(Array array, int index, int count) =>
        Array.Clear(array, index, count);
}
