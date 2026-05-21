// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationThrowHelper.CallerExpression.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

#if !NETSTANDARD2_0_OR_GREATER
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Bodu.Text.Configuration;

internal static partial class ConfigurationThrowHelper
{
    /// <summary>
    /// Throws an <see cref="ArgumentException" /> when <paramref name="stream" /> does not support reading.
    /// </summary>
    /// <param name="stream">The stream to validate.</param>
    /// <param name="paramName">The parameter name reported in the exception; inferred from the call site.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="stream" />.<see cref="Stream.CanRead" /> is <see langword="false" />.
    /// </exception>
    internal static void ThrowIfStreamNotReadable(
        Stream stream,
        [CallerArgumentExpression(nameof(stream))] string? paramName = null)
    {
        if (!stream.CanRead)
            throw new ArgumentException(ConfigurationResourceStrings.Arg_Invalid_StreamNotReadable, paramName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException" /> when <paramref name="stream" /> does not support writing.
    /// </summary>
    /// <param name="stream">The stream to validate.</param>
    /// <param name="paramName">The parameter name reported in the exception; inferred from the call site.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="stream" />.<see cref="Stream.CanWrite" /> is <see langword="false" />.
    /// </exception>
    internal static void ThrowIfStreamNotWritable(
        Stream stream,
        [CallerArgumentExpression(nameof(stream))] string? paramName = null)
    {
        if (!stream.CanWrite)
            throw new ArgumentException(ConfigurationResourceStrings.Arg_Invalid_StreamNotWritable, paramName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException" /> when <paramref name="rawKey" /> contains any control character.
    /// </summary>
    /// <param name="rawKey">The candidate configuration key to scan.</param>
    /// <param name="paramName">The parameter name reported in the exception; inferred from the call site.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="rawKey" /> contains a character for which <see cref="char.IsControl(char)" /> returns
    /// <see langword="true" />. The exception message includes the zero-based index of the first such character.
    /// </exception>
    internal static void ThrowIfConfigKeyContainsControlChar(
        string rawKey,
        [CallerArgumentExpression(nameof(rawKey))] string? paramName = null)
    {
        for (var i = 0; i < rawKey.Length; i++)
        {
            if (char.IsControl(rawKey[i]))
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        ConfigurationResourceStrings.Arg_Invalid_ConfigKeyControlChar,
                        i),
                    paramName);
        }
    }
}

#endif
