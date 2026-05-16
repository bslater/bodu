// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelper.Null.CallerExpression.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

#if !NETSTANDARD2_0_OR_GREATER
#pragma warning disable SA1117 // Parameters should be on same line or separate lines
#pragma warning disable IDE0011 // Add braces

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Bodu;

public static partial class ThrowHelper
{
    /// <summary>
    /// Throws an <see cref="ArgumentNullException" /> if <paramref name="value" /> is <see langword="null" />.
    /// </summary>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <param name="value">The value to check. Must not be <see langword="null" />.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNull<T>(
        [NotNull] T value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value is null)
            throw new ArgumentNullException(paramName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentNullException" /> if <paramref name="value" /> is <see langword="null" />,
    /// using <paramref name="message" /> as the exception message.
    /// </summary>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <param name="value">The value to check. Must not be <see langword="null" />.</param>
    /// <param name="message">The message to include in the exception.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNull<T>(
        [NotNull] T value, string message,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value is null)
            throw new ArgumentNullException(paramName, message);
    }

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException" /> if <paramref name="disposed" /> is <see langword="true" />.
    /// </summary>
    /// <param name="disposed">The disposal flag to evaluate.</param>
    /// <param name="objectName">
    /// The name of the disposed object included in the exception message.
    /// Supplied automatically by the compiler via <see cref="CallerArgumentExpressionAttribute" />.
    /// </param>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when <paramref name="disposed" /> is <see langword="true" />.
    /// </exception>
    [SuppressMessage(
        "Microsoft.CodeAnalysis.NetAnalyzers",
        "CA1513:Use ObjectDisposedException throw helper",
        Justification = "ObjectDisposedException.ThrowIf requires an object or Type instance; this helper accepts a string objectName captured via CallerArgumentExpressionAttribute, which the BCL helper does not support.")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfDisposed(
        bool disposed,
        [CallerArgumentExpression(nameof(disposed))] string? objectName = null)
    {
        if (disposed)
            throw new ObjectDisposedException(objectName);
    }
}

#endif
