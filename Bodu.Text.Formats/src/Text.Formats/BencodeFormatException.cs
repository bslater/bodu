// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeFormatException.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Formats;

/// <summary>
/// Represents an error that occurs when bencoded data is malformed.
/// </summary>
/// <remarks>
/// <para>
/// Raised by <see cref="Bencode.Decode(ReadOnlySpan{byte})" /> when the input cannot be interpreted as a valid bencoded
/// document — for example, when an unexpected prefix byte is encountered, an integer is missing its terminating
/// <c>e</c>, a string's length prefix does not fit the remaining bytes, a dictionary's keys are not in lexicographic
/// order, or the document contains trailing bytes after a complete value. The error is signalled through the
/// <see cref="FormatException" /> hierarchy so callers can catch it alongside other parse failures.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// try
/// {
///     BencodedValue root = Bencode.Decode(payload);
/// }
/// catch (BencodeFormatException ex)
/// {
///     Console.Error.WriteLine($"Malformed bencode: {ex.Message}");
/// }
///]]>
/// </example>
public sealed class BencodeFormatException
    : FormatException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeFormatException" /> class.
    /// </summary>
    public BencodeFormatException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeFormatException" /> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public BencodeFormatException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeFormatException" /> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public BencodeFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
