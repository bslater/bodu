// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ArrayExtensions.ToMatrix.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class ArrayExtensions
{
    /// <summary>
    /// Copies the elements of a jagged array into a newly allocated two-dimensional array.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">
    /// The jagged source array. All inner arrays must be non-<see langword="null" /> and have the same length.
    /// </param>
    /// <param name="transpose">
    /// When <see langword="false" />, the result has shape <c>[rows, cols]</c> matching <paramref name="source" />;
    /// when <see langword="true" />, rows and columns are swapped to produce a shape of <c>[cols, rows]</c>.
    /// </param>
    /// <returns>A new rectangular array containing the values from <paramref name="source" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source" /> is <see langword="null" />, or any inner array is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="source" /> contains no rows, or the inner arrays do not all share the same length.
    /// </exception>
    public static T[,] ToMatrix<T>(this T[][] source, bool transpose)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfArrayLengthIsZero(source);

        T[] firstRow = source[0] ?? throw new ArgumentNullException(nameof(source));
        var rows = source.Length;
        var cols = firstRow.Length;

        for (var i = 1; i < rows; i++)
        {
            T[] row = source[i] ?? throw new ArgumentNullException(nameof(source));
            if (row.Length != cols) throw new ArgumentException(ResourceStrings.Arg_Invalid_JaggedArrayInnerLength, nameof(source));
        }

        T[,] matrix = transpose ? new T[cols, rows] : new T[rows, cols];
        for (var i = 0; i < rows; i++)
        {
            T[] row = source[i];
            for (var j = 0; j < cols; j++)
            {
                if (transpose)
                    matrix[j, i] = row[j];
                else
                    matrix[i, j] = row[j];
            }
        }

        return matrix;
    }
}
