// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelper.Stream-NetStandard.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

#if NETSTANDARD2_0_OR_GREATER
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Bodu;

public static partial class ThrowHelper
{
    /// <summary>
    /// Throws an <see cref="ArgumentNullException" /> if <paramref name="stream" /> is <see langword="null" />, or an
    /// <see cref="ArgumentException" /> if it does not support reading.
    /// </summary>
    /// <param name="stream">The stream to validate.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="stream" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="stream" />.<see cref="Stream.CanRead" /> is <see langword="false" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfStreamNotReadable(Stream stream)
    {
        if (stream is null)
            throw new ArgumentNullException(nameof(stream));

        if (!stream.CanRead)
            throw new ArgumentException(ResourceStrings.Arg_Invalid_StreamNotReadable, nameof(stream));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentNullException" /> if <paramref name="stream" /> is <see langword="null" />, or an
    /// <see cref="ArgumentException" /> if it does not support writing.
    /// </summary>
    /// <param name="stream">The stream to validate.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="stream" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="stream" />.<see cref="Stream.CanWrite" /> is <see langword="false" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfStreamNotWritable(Stream stream)
    {
        if (stream is null)
            throw new ArgumentNullException(nameof(stream));

        if (!stream.CanWrite)
            throw new ArgumentException(ResourceStrings.Arg_Invalid_StreamNotWritable, nameof(stream));
    }
}

#endif
