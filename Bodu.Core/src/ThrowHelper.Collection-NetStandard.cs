// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelper.Collection.NetStandard.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

#if NETSTANDARD2_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Bodu;

public static partial class ThrowHelper
{
    /// <summary>
    /// Throws an <see cref="ArgumentException" /> if the collection has fewer than
    /// <paramref name="minCount" /> elements.
    /// </summary>
    /// <typeparam name="T">The element type of the collection.</typeparam>
    /// <param name="collection">The collection to validate. Must not be <see langword="null" />.</param>
    /// <param name="minCount">The minimum number of required elements.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="collection" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <c>collection.Count &lt; minCount</c>.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfCollectionTooSmall<T>(ICollection<T> collection, int minCount)
    {
        if (collection is null)
            throw new ArgumentNullException(nameof(collection));

        if (collection.Count < minCount)
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_Invalid_CollectionTooSmall, minCount),
                nameof(collection));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException" /> if the collection is empty.
    /// </summary>
    /// <typeparam name="T">The element type of the collection.</typeparam>
    /// <param name="collection">The collection to validate. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="collection" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <c>collection.Count == 0</c>.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfCollectionIsEmpty<T>(ICollection<T> collection)
    {
        if (collection is null)
            throw new ArgumentNullException(nameof(collection));

        if (collection.Count == 0)
            throw new ArgumentException(ResourceStrings.Arg_Invalid_CollectionIsEmpty, nameof(collection));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException" /> if <paramref name="value" /> is <see langword="null" /> when
    /// <paramref name="conditionalParam" /> equals <paramref name="conditionalValue" />.
    /// </summary>
    /// <typeparam name="TValue">The type of the parameter being validated.</typeparam>
    /// <typeparam name="TCondition">The type of the conditional parameter.</typeparam>
    /// <param name="value">The parameter value to validate for null.</param>
    /// <param name="conditionalParam">The current value of the conditional parameter.</param>
    /// <param name="conditionalValue">The value of <paramref name="conditionalParam" /> that makes
    /// <paramref name="value" /> mandatory.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="conditionalParam" /> equals <paramref name="conditionalValue" /> and
    /// <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// Use this method when a parameter becomes mandatory depending on the value of another parameter.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfConditionallyRequiredParameterIsNull<TValue, TCondition>(
        TValue? value,
        TCondition conditionalParam,
        TCondition conditionalValue)
    {
        if (EqualityComparer<TCondition>.Default.Equals(conditionalParam, conditionalValue) && value is null)
        {
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_Invalid_ParameterRequiredIf,
                    nameof(value), nameof(conditionalParam), nameof(conditionalValue)),
                nameof(value));
        }
    }

    /// <summary>
    /// Throws an <see cref="ArgumentNullException" /> if <paramref name="collection" /> is <see langword="null" />,
    /// or an <see cref="ArgumentException" /> if it is read-only.
    /// </summary>
    /// <typeparam name="T">The element type of the collection.</typeparam>
    /// <param name="collection">The collection to validate. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="collection" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <c>collection.IsReadOnly</c> is <see langword="true" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfReadOnly<T>(ICollection<T> collection)
    {
        if (collection is null)
            throw new ArgumentNullException(nameof(collection));

        if (collection.IsReadOnly)
            throw new ArgumentException(ResourceStrings.Arg_Invalid_CollectionReadOnly, nameof(collection));
    }
}

#endif
