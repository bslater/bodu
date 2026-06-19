// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelper.Stream-CallerExpression.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

#if !NETSTANDARD2_0_OR_GREATER
#pragma warning disable SA1117 // Parameters should be on same line or separate lines
#pragma warning disable IDE0011 // Add braces

using System.Runtime.CompilerServices;

namespace Bodu;

public static partial class ThrowHelper
{
    /// <summary>
    /// Throws an <see cref="ArgumentNullException" /> if <paramref name="stream" /> is <see langword="null" />, or an
    /// <see cref="ArgumentException" /> if it does not support reading.
    /// </summary>
    /// <param name="stream">The stream to validate.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="stream" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="stream" />.<see cref="Stream.CanRead" /> is <see langword="false" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfStreamNotReadable(
        Stream stream,
        [CallerArgumentExpression(nameof(stream))] string? paramName = null)
    {
        if (stream is null)
            throw new ArgumentNullException(paramName);

        if (!stream.CanRead)
            throw new ArgumentException(ResourceStrings.Arg_Invalid_StreamNotReadable, paramName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentNullException" /> if <paramref name="stream" /> is <see langword="null" />, or an
    /// <see cref="ArgumentException" /> if it does not support writing.
    /// </summary>
    /// <param name="stream">The stream to validate.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="stream" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="stream" />.<see cref="Stream.CanWrite" /> is <see langword="false" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfStreamNotWritable(
        Stream stream,
        [CallerArgumentExpression(nameof(stream))] string? paramName = null)
    {
        if (stream is null)
            throw new ArgumentNullException(paramName);

        if (!stream.CanWrite)
            throw new ArgumentException(ResourceStrings.Arg_Invalid_StreamNotWritable, paramName);
    }
}

#endif
