// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationHelpers.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Bodu.Text.Configuration;

/// <summary>
/// Provides general-purpose utility methods used by configuration components within <c>Bodu.Text.Configuration</c>,
/// including reusable exception factories for situations that do not fit the <c>ThrowIf</c>-style guard pattern.
/// </summary>
/// <remarks>
/// <para>
/// Members here unconditionally throw a configured exception, typically because the precondition was evaluated at the
/// call site and the failure path is shared by multiple members. For conditional <c>ThrowIf</c>-style validation, see
/// <see cref="ConfigurationThrowHelper" />.
/// </para>
/// </remarks>
[SuppressMessage(
    "Performance",
    "CA1822:Mark members as static",
    Justification = "Helper APIs are intentionally static factories.")]
internal static class ConfigurationHelpers
{
    /// <summary>
    /// Throws a <see cref="KeyNotFoundException" /> indicating that <paramref name="key" /> was not present in the
    /// resolved configuration view.
    /// </summary>
    /// <param name="key">The configuration key that was not found.</param>
    /// <exception cref="KeyNotFoundException">
    /// Always thrown. The exception message embeds <paramref name="key" />.
    /// </exception>
    [DoesNotReturn]
    internal static void ThrowConfigKeyNotPresent(string key) =>
        throw new KeyNotFoundException(
            string.Format(
                CultureInfo.InvariantCulture,
                ConfigurationResourceStrings.Op_Invalid_ConfigKeyNotPresent,
                key));

    /// <summary>
    /// Throws a <see cref="FormatException" /> indicating that the raw value for <paramref name="key" /> could not be
    /// converted to <paramref name="targetTypeName" />.
    /// </summary>
    /// <param name="key">The configuration key whose value failed to parse.</param>
    /// <param name="rawValue">The raw value that could not be converted.</param>
    /// <param name="targetTypeName">The display name of the target type.</param>
    /// <exception cref="FormatException">
    /// Always thrown. The exception message embeds <paramref name="key" />, <paramref name="rawValue" />, and
    /// <paramref name="targetTypeName" />.
    /// </exception>
    [DoesNotReturn]
    internal static void ThrowValueNotConvertible(string key, string? rawValue, string targetTypeName) =>
        throw new FormatException(
            string.Format(
                CultureInfo.InvariantCulture,
                ConfigurationResourceStrings.Format_Invalid_ValueNotConvertible,
                key,
                rawValue,
                targetTypeName));

    /// <summary>
    /// Throws an <see cref="InvalidOperationException" /> indicating that a configuration document parsed without a
    /// path root cannot be resolved.
    /// </summary>
    /// <exception cref="InvalidOperationException">Always thrown.</exception>
    [DoesNotReturn]
    internal static void ThrowResolveWithoutPathRoot() =>
        throw new InvalidOperationException(ConfigurationResourceStrings.Op_Invalid_ResolveWithoutPathRoot);
}
