// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ArrayExtensions.Copy.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class ArrayExtensions
{
    /// <summary>
    /// Creates a shallow copy of the specified array of value types. Returns <see langword="null" /> if the source
    /// array is <see langword="null" />.
    /// </summary>
    /// <typeparam name="T">The value type of the elements in the array.</typeparam>
    /// <param name="array">The array to copy.</param>
    /// <returns>
    /// A new array containing the same elements as the source, or <see langword="null" /> if the source is
    /// <see langword="null" />.
    /// </returns>
#pragma warning disable S1168

    [return: System.Diagnostics.CodeAnalysis.MaybeNull]
    public static T[] Copy<T>(this T[] array)
        where T : struct => array is null ? null : (T[])array.Clone();

#pragma warning restore S1168
}
