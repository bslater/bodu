// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundFileException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

/// <summary>
/// The base class for exceptions raised by the compound-file reader, writer, and object model.
/// </summary>
/// <remarks>
/// Catch this type to handle any compound-file failure uniformly. The concrete subclasses narrow the cause:
/// <see cref="CompoundFileFormatException" /> reports structural failures encountered while reading,
/// <see cref="CompoundFileSerializationException" /> reports authoring and write-time failures, and
/// <see cref="CompoundStreamNotFoundException" /> reports a missing named stream or storage.
/// </remarks>
public class CompoundFileException
    : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundFileException" /> class.
    /// </summary>
    public CompoundFileException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundFileException" /> class with the specified message.
    /// </summary>
    /// <param name="message">A message that describes the failure.</param>
    public CompoundFileException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundFileException" /> class with the specified message and a
    /// reference to the underlying cause.
    /// </summary>
    /// <param name="message">A message that describes the failure.</param>
    /// <param name="innerException">The exception that is the cause of this exception.</param>
    public CompoundFileException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
