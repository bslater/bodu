// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IComparableExtensions.IsEqualTo.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Bodu.Extensions;

public static partial class IComparableExtensions
{
    /// <summary>
    /// Determines whether two values are equal under the ordering defined by a custom <see cref="IComparer{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the values to compare.</typeparam>
    /// <param name="value">The first value.</param>
    /// <param name="other">The second value.</param>
    /// <param name="comparer">The comparer whose ordering is used to determine equality.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="comparer"/> compares <paramref name="value"/> and <paramref name="other"/> as
    /// equal (that is, <see cref="IComparer{T}.Compare(T, T)"/> returns zero); otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="comparer"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// Comparer-defined equality is distinct from <see cref="object.Equals(object)"/>. A case-insensitive
    /// <see cref="StringComparer"/>, for example, considers <c>"Hello"</c> and <c>"hello"</c> equal even though
    /// <see cref="string.Equals(string)"/> does not. For the default equality semantics of <typeparamref name="T"/>
    /// use <see cref="object.Equals(object)"/> or <see cref="IEquatable{T}.Equals(T)"/> directly.
    /// </remarks>
    public static bool IsEqualTo<T>(this T value, T other, IComparer<T> comparer) =>
        comparer is null
            ? throw new ArgumentNullException(nameof(comparer))
            : comparer.Compare(value, other) == 0;
}
