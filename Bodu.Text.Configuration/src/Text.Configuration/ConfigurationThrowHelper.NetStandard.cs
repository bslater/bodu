// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationThrowHelper.NetStandard.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

#if NETSTANDARD2_0_OR_GREATER
using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Bodu.Text.Configuration;

internal static partial class ConfigurationThrowHelper
{
    /// <summary>
    /// Throws an <see cref="ArgumentException" /> when <paramref name="rawKey" /> contains any control character.
    /// </summary>
    /// <param name="rawKey">The candidate configuration key to scan.</param>
    /// <param name="paramName">The parameter name reported in the exception.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="rawKey" /> contains a character for which <see cref="char.IsControl(char)" /> returns
    /// <see langword="true" />. The exception message includes the zero-based index of the first such character.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfConfigKeyContainsControlChar(string rawKey, string? paramName = null)
    {
        for (var i = 0; i < rawKey.Length; i++)
        {
            if (char.IsControl(rawKey[i]))
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ConfigurationResourceStrings.Arg_Invalid_ConfigKeyControlChar,
                        i),
                    paramName);
        }
    }
}

#endif
