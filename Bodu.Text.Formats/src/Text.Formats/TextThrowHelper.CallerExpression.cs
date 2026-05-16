// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextThrowHelper.CallerExpression.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace Bodu.Text.Formats;

internal static partial class TextThrowHelper
{
    /// <summary>
    /// Throws an <see cref="ArgumentException" /> when <paramref name="stream" /> does not support reading.
    /// </summary>
    /// <param name="stream">The stream to test.</param>
    /// <param name="paramName">The parameter name reported in the exception; inferred from the call site.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="stream" /> cannot be read.</exception>
    internal static void ThrowIfStreamNotReadable(
        Stream stream,
        [CallerArgumentExpression(nameof(stream))] string? paramName = null)
    {
        if (!stream.CanRead)
            throw new ArgumentException(FormatsResourceStrings.ArgumentException_StreamNotReadable, paramName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException" /> when <paramref name="stream" /> does not support writing.
    /// </summary>
    /// <param name="stream">The stream to test.</param>
    /// <param name="paramName">The parameter name reported in the exception; inferred from the call site.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="stream" /> cannot be written.</exception>
    internal static void ThrowIfStreamNotWritable(
        Stream stream,
        [CallerArgumentExpression(nameof(stream))] string? paramName = null)
    {
        if (!stream.CanWrite)
            throw new ArgumentException(FormatsResourceStrings.ArgumentException_StreamNotWritable, paramName);
    }
}
