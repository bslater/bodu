// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundFileSerializationException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

/// <summary>
/// The exception thrown when a compound file cannot be authored or serialized — for example, when a storage contains a
/// duplicate or invalid child name, the directory nesting is too deep, or a value cannot be represented in the
/// container.
/// </summary>
/// <remarks>
/// This exception reports authoring and write-time failures of the mutable object model and builder, as distinct from
/// <see cref="CompoundFileFormatException" />, which reports structural failures encountered while reading. It derives
/// from <see cref="CompoundFileException" />, the common base for all compound-file failures.
/// </remarks>
public sealed class CompoundFileSerializationException
    : CompoundFileException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundFileSerializationException" /> class.
    /// </summary>
    public CompoundFileSerializationException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundFileSerializationException" /> class with the specified
    /// message.
    /// </summary>
    /// <param name="message">A message that describes the authoring or serialization failure.</param>
    public CompoundFileSerializationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundFileSerializationException" /> class with the specified
    /// message and a reference to the underlying cause.
    /// </summary>
    /// <param name="message">A message that describes the authoring or serialization failure.</param>
    /// <param name="innerException">The exception that is the cause of this exception.</param>
    public CompoundFileSerializationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
