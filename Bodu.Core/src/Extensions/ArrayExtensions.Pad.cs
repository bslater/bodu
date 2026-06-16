// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ArrayExtensions.Pad.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class ArrayExtensions
{
    /// <summary>
    /// Returns a new array of length <paramref name="totalLength" /> whose trailing elements are the contents of
    /// <paramref name="array" /> and whose leading positions are filled with <paramref name="padValue" />.
    /// </summary>
    /// <typeparam name="T">The element type of the array.</typeparam>
    /// <param name="array">The source array to right-align in the result.</param>
    /// <param name="totalLength">The desired length of the result.</param>
    /// <param name="padValue">The value used to fill the leading positions.</param>
    /// <returns>
    /// A new array of length <paramref name="totalLength" />. When <paramref name="totalLength" /> equals
    /// <c>array.Length</c> the result is a fresh copy of <paramref name="array" /> with no padding.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="array" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="totalLength" /> is less than <c>array.Length</c>.
    /// </exception>
    public static T[] PadLeft<T>(this T[] array, int totalLength, T padValue)
    {
        ThrowHelper.ThrowIfNull(array);
        if (totalLength < array.Length) throw new ArgumentOutOfRangeException(nameof(totalLength));

        var result = new T[totalLength];
        int padCount = totalLength - array.Length;

        if (padCount > 0)
            Array.Fill(result, padValue, 0, padCount);

        Array.Copy(array, 0, result, padCount, array.Length);
        return result;
    }

    /// <summary>
    /// Returns a new array of length <paramref name="totalLength" /> whose leading elements are the contents of
    /// <paramref name="array" /> and whose trailing positions are filled with <paramref name="padValue" />.
    /// </summary>
    /// <typeparam name="T">The element type of the array.</typeparam>
    /// <param name="array">The source array to left-align in the result.</param>
    /// <param name="totalLength">The desired length of the result.</param>
    /// <param name="padValue">The value used to fill the trailing positions.</param>
    /// <returns>
    /// A new array of length <paramref name="totalLength" />. When <paramref name="totalLength" /> equals
    /// <c>array.Length</c> the result is a fresh copy of <paramref name="array" /> with no padding.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="array" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="totalLength" /> is less than <c>array.Length</c>.
    /// </exception>
    public static T[] PadRight<T>(this T[] array, int totalLength, T padValue)
    {
        ThrowHelper.ThrowIfNull(array);
        if (totalLength < array.Length) throw new ArgumentOutOfRangeException(nameof(totalLength));

        var result = new T[totalLength];
        Array.Copy(array, 0, result, 0, array.Length);

        int padCount = totalLength - array.Length;
        if (padCount > 0)
            Array.Fill(result, padValue, array.Length, padCount);

        return result;
    }
}
