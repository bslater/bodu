// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextThrowHelper.NetStandard.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

#if NETSTANDARD2_0_OR_GREATER
using System.Runtime.CompilerServices;

namespace Bodu.Text;

internal static partial class TextThrowHelper
{
    /// <summary>
    /// Throws an <see cref="ArgumentException" /> when <paramref name="stream" /> does not support reading.
    /// </summary>
    /// <param name="stream">The stream to test.</param>
    /// <param name="paramName">The parameter name reported in the exception.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="stream" /> cannot be read.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfStreamNotReadable(Stream stream, string? paramName = null)
    {
        if (!stream.CanRead)
            throw new ArgumentException(FormatsResourceStrings.Arg_Invalid_StreamNotReadable, paramName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException" /> when <paramref name="stream" /> does not support writing.
    /// </summary>
    /// <param name="stream">The stream to test.</param>
    /// <param name="paramName">The parameter name reported in the exception.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="stream" /> cannot be written.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfStreamNotWritable(Stream stream, string? paramName = null)
    {
        if (!stream.CanWrite)
            throw new ArgumentException(FormatsResourceStrings.Arg_Invalid_StreamNotWritable, paramName);
    }
}

#endif
