// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelper.Collection-CallerExpression.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

#if !NETSTANDARD2_0_OR_GREATER
#pragma warning disable SA1117 // Parameters should be on same line or separate lines
#pragma warning disable IDE0011 // Add braces

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bodu;

public static partial class ThrowHelper
{
    /// <summary>
    /// Cached parsed format for <see cref="ResourceStrings.Arg_Invalid_CollectionTooSmall" />.
    /// </summary>
    private static readonly CompositeFormat s_argInvalidCollectionTooSmall =
        CompositeFormat.Parse(ResourceStrings.Arg_Invalid_CollectionTooSmall);

    /// <summary>
    /// Throws an <see cref="ArgumentException" /> if the collection has fewer than <paramref name="minCount" />
    /// elements.
    /// </summary>
    /// <typeparam name="T">The element type of the collection.</typeparam>
    /// <param name="collection">The collection to validate. Must not be <see langword="null" />.</param>
    /// <param name="minCount">The minimum number of required elements.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="collection" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <c>collection.Count &lt; minCount</c>.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfCollectionTooSmall<T>(
        ICollection<T> collection, int minCount,
        [CallerArgumentExpression(nameof(collection))] string? paramName = null)
    {
        ThrowIfNull(collection, paramName);
        if (collection.Count < minCount)
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, s_argInvalidCollectionTooSmall, minCount),
                paramName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException" /> if the collection is empty.
    /// </summary>
    /// <typeparam name="T">The element type of the collection.</typeparam>
    /// <param name="collection">The collection to validate. Must not be <see langword="null" />.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="collection" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <c>collection.Count == 0</c>.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfCollectionIsEmpty<T>(
        ICollection<T> collection,
        [CallerArgumentExpression(nameof(collection))] string? paramName = null)
    {
        ThrowIfNull(collection, paramName);
        if (collection.Count == 0)
            throw new ArgumentException(ResourceStrings.Arg_Invalid_CollectionIsEmpty, paramName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException" /> if <paramref name="value" /> is <see langword="null" /> when
    /// <paramref name="conditionalParam" /> equals <paramref name="conditionalValue" />.
    /// </summary>
    /// <typeparam name="TValue">The type of the parameter being validated.</typeparam>
    /// <typeparam name="TCondition">The type of the conditional parameter.</typeparam>
    /// <param name="value">The parameter value to validate for null.</param>
    /// <param name="conditionalParam">The current value of the conditional parameter.</param>
    /// <param name="conditionalValue">
    /// The value of <paramref name="conditionalParam" /> that makes <paramref name="value" /> mandatory.
    /// </param>
    /// <param name="paramName">The name of the value parameter. Supplied automatically by the compiler.</param>
    /// <param name="conditionalParamName">
    /// The name of the conditional parameter. Supplied automatically by the compiler.
    /// </param>
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
        TCondition conditionalValue,
        [CallerArgumentExpression(nameof(value))] string? paramName = null,
        [CallerArgumentExpression(nameof(conditionalParam))] string? conditionalParamName = null)
    {
        if (EqualityComparer<TCondition>.Default.Equals(conditionalParam, conditionalValue) && value is null)
        {
            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    s_argInvalidParameterRequiredIf,
                    paramName,
                    conditionalParamName,
                    conditionalValue),
                paramName);
        }
    }

    /// <summary>
    /// Throws an <see cref="ArgumentNullException" /> if <paramref name="collection" /> is <see langword="null" />, or
    /// an <see cref="ArgumentException" /> if it is read-only.
    /// </summary>
    /// <typeparam name="T">The element type of the collection.</typeparam>
    /// <param name="collection">The collection to validate. Must not be <see langword="null" />.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="collection" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <c>collection.IsReadOnly</c> is <see langword="true" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfReadOnly<T>(
        ICollection<T> collection,
        [CallerArgumentExpression(nameof(collection))] string? paramName = null)
    {
        ThrowIfNull(collection, paramName);
        if (collection.IsReadOnly)
            throw new ArgumentException(ResourceStrings.Arg_Invalid_CollectionReadOnly, paramName);
    }
}

#endif
