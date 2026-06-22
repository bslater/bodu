// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundFileFormatException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

/// <summary>
/// The exception thrown when a stream cannot be interpreted as a well-formed OLE2 / Compound File Binary container.
/// </summary>
/// <remarks>
/// This exception reports structural failures of the container itself — an invalid signature, a malformed header, a
/// circular or out-of-range sector chain, or a corrupt directory — rather than any application-level format carried
/// inside a stream. The <see cref="Category" /> classifies the failure in a message-independent way. It derives from
/// <see cref="CompoundFileException" />, the common base for all compound-file failures.
/// </remarks>
public sealed class CompoundFileFormatException
    : CompoundFileException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundFileFormatException" /> class.
    /// </summary>
    public CompoundFileFormatException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundFileFormatException" /> class with the specified message.
    /// </summary>
    /// <param name="message">A message that describes the structural failure.</param>
    public CompoundFileFormatException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundFileFormatException" /> class with the specified message
    /// and a reference to the underlying cause.
    /// </summary>
    /// <param name="message">A message that describes the structural failure.</param>
    /// <param name="innerException">The exception that is the cause of this exception.</param>
    public CompoundFileFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundFileFormatException" /> class with the specified message
    /// and failure category.
    /// </summary>
    /// <param name="message">A message that describes the structural failure.</param>
    /// <param name="category">The category that classifies the failure.</param>
    public CompoundFileFormatException(string message, CompoundFileError category)
        : base(message)
    {
        Category = category;
    }

    /// <summary>
    /// Gets the category that classifies the structural failure.
    /// </summary>
    /// <returns>
    /// The <see cref="CompoundFileError" /> describing the failure; <see cref="CompoundFileError.Unknown" /> when
    /// unspecified.
    /// </returns>
    public CompoundFileError Category { get; }
}
