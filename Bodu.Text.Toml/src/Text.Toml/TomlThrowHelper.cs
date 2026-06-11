// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlThrowHelper.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace Bodu.Text.Toml;

/// <summary>
/// Provides <c>ThrowIf</c> stream-capability guards used by the TOML reader and writer that are not covered by
/// <see cref="Bodu.ThrowHelper" />.
/// </summary>
internal static class TomlThrowHelper
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
            throw new ArgumentException(TomlResourceStrings.Arg_Invalid_StreamNotReadable, paramName);
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
            throw new ArgumentException(TomlResourceStrings.Arg_Invalid_StreamNotWritable, paramName);
    }
}
