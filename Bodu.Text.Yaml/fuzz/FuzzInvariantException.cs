// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FuzzInvariantException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml.Fuzzing;

/// <summary>
/// Thrown by the fuzz harness when a parser property invariant is violated (for example a parsed document that does not
/// round-trip). It is intentionally not one of the library's contracted exceptions, so libFuzzer records it as a
/// finding.
/// </summary>
internal sealed class FuzzInvariantException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FuzzInvariantException" /> class with the specified message.
    /// </summary>
    /// <param name="message">The message describing the violated invariant.</param>
    public FuzzInvariantException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FuzzInvariantException" /> class with the specified message and
    /// the underlying exception.
    /// </summary>
    /// <param name="message">The message describing the violated invariant.</param>
    /// <param name="innerException">The exception that triggered the invariant failure.</param>
    public FuzzInvariantException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
