// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstNodeNotFoundException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst;

/// <summary>
/// Represents a lookup for a node that does not exist in the file.
/// </summary>
/// <remarks>
/// A missing node is a caller-visible condition distinct from structural corruption — the file is well-formed, the
/// identifier simply is not present — so it carries its own exception type (with
/// <see cref="PstFileException.Error" /> set to <see cref="PstFileError.NodeNotFound" />) rather than the base
/// <see cref="PstFileException" />. Callers that prefer flow control over exceptions use
/// <see cref="PstFile.TryGetNode" />.
/// </remarks>
public sealed class PstNodeNotFoundException
    : PstFileException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PstNodeNotFoundException" /> class.
    /// </summary>
    public PstNodeNotFoundException()
        : base(null, PstFileError.NodeNotFound)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PstNodeNotFoundException" /> class with a message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public PstNodeNotFoundException(string? message)
        : base(message, PstFileError.NodeNotFound)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PstNodeNotFoundException" /> class with a message and an inner
    /// exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused this error.</param>
    public PstNodeNotFoundException(string? message, Exception? innerException)
        : base(message, innerException, PstFileError.NodeNotFound)
    {
    }
}
