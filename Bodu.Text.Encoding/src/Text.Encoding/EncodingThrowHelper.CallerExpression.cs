// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EncodingThrowHelper.CallerExpression.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

#if !NETSTANDARD2_0_OR_GREATER
using System.Runtime.CompilerServices;

namespace Bodu.Text.Encoding;

internal static partial class EncodingThrowHelper
{
    /// <summary>
    /// Throws a <see cref="FormatException" /> when the decoded byte count does not equal the 16 bytes required to
    /// populate a <see cref="System.Guid" /> value.
    /// </summary>
    /// <param name="decodedLength">The number of bytes produced by the decoder.</param>
    /// <exception cref="FormatException">Thrown when <paramref name="decodedLength" /> is not exactly 16.</exception>
    internal static void ThrowIfGuidLengthMismatch(int decodedLength)
    {
        if (decodedLength != 16)
            throw new FormatException(EncodingResourceStrings.Format_Invalid_GuidNotSixteenBytes);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException" /> when <paramref name="destinationLength" /> is less than
    /// <paramref name="requiredLength" />.
    /// </summary>
    /// <param name="destinationLength">The available length of the destination span.</param>
    /// <param name="requiredLength">The minimum length the destination must provide.</param>
    /// <param name="paramName">The parameter name reported in the exception; inferred from the call site.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="destinationLength" /> is less than <paramref name="requiredLength" />.
    /// </exception>
    internal static void ThrowIfDestinationTooSmallForEncoded(
        int destinationLength,
        int requiredLength,
        [CallerArgumentExpression(nameof(destinationLength))] string? paramName = null)
    {
        if (destinationLength < requiredLength)
            throw new ArgumentException(
                EncodingResourceStrings.Arg_Invalid_DestinationTooSmallForEncoded,
                paramName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException" /> when <paramref name="sourceLength" /> is not a multiple of four,
    /// which is required by Z85 encoding.
    /// </summary>
    /// <param name="sourceLength">The length of the input span.</param>
    /// <param name="paramName">The parameter name reported in the exception; inferred from the call site.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="sourceLength" /> is not a positive multiple of four.
    /// </exception>
    internal static void ThrowIfZ85InputLengthInvalid(
        int sourceLength,
        [CallerArgumentExpression(nameof(sourceLength))] string? paramName = null)
    {
        if (sourceLength % 4 != 0)
            throw new ArgumentException(
                EncodingResourceStrings.Arg_Invalid_Z85InputMultipleOfFour,
                paramName);
    }
}

#endif
