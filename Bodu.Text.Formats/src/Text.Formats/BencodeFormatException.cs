// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeFormatException.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Formats;

/// <summary>
/// Represents an error that occurs when bencoded data is malformed.
/// </summary>
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
