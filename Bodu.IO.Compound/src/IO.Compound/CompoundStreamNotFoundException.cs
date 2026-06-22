// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundStreamNotFoundException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.IO.Compound;

/// <summary>
/// The exception thrown when a named stream is requested from a compound file but no matching directory entry exists.
/// </summary>
/// <remarks>
/// Thrown by <see cref="CompoundStorage.OpenStream(string)" /> and <see cref="CompoundStorage.OpenStorage(string)" />.
/// Callers that prefer a non-throwing lookup should use
/// <see cref="CompoundStorage.TryOpenStream(string, out CompoundStream)" /> or
/// <see cref="CompoundStorage.TryOpenStorage(string, out CompoundStorage)" /> instead.
/// </remarks>
/// <example>
/// Catch the exception when a stream is expected to exist, or switch to the <c>Try</c> pattern when its absence is a
/// normal outcome:
/// <code language="csharp">
///<![CDATA[
/// try
/// {
///     CompoundStream entry = file.RootStorage.OpenStream("Workbook");
/// }
/// catch (CompoundStreamNotFoundException ex)
/// {
///     Console.WriteLine($"Missing stream: {ex.StreamName}");
/// }
///
/// // Non-throwing alternative:
/// if (file.RootStorage.TryOpenStream("Workbook", out CompoundStream? workbook))
/// {
///     // ...
/// }
///]]>
/// </code>
/// </example>
public sealed class CompoundStreamNotFoundException
    : KeyNotFoundException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundStreamNotFoundException" /> class.
    /// </summary>
    public CompoundStreamNotFoundException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundStreamNotFoundException" /> class with the specified
    /// message.
    /// </summary>
    /// <param name="message">A message that describes the lookup failure.</param>
    public CompoundStreamNotFoundException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundStreamNotFoundException" /> class with the specified
    /// message and a reference to the underlying cause.
    /// </summary>
    /// <param name="message">A message that describes the lookup failure.</param>
    /// <param name="innerException">The exception that is the cause of this exception.</param>
    public CompoundStreamNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundStreamNotFoundException" /> class for the named stream,
    /// composing a descriptive message and capturing the requested name.
    /// </summary>
    /// <param name="streamName">The name of the stream that was requested but not found.</param>
    /// <param name="sentinel">
    /// An unused discriminator that distinguishes this overload from the message constructor.
    /// </param>
    private CompoundStreamNotFoundException(string streamName, bool sentinel)
        : base(string.Format(CultureInfo.CurrentCulture, CompoundResourceStrings.IO_KeyNotFound_CompoundStream, streamName))
    {
        _ = sentinel;
        StreamName = streamName;
    }

    /// <summary>
    /// Gets the name of the stream that was requested but not found, when known.
    /// </summary>
    /// <returns>
    /// The requested stream name, or <see langword="null" /> when this instance was created without one.
    /// </returns>
    public string? StreamName { get; }

    /// <summary>
    /// Creates a <see cref="CompoundStreamNotFoundException" /> describing a missing stream by name.
    /// </summary>
    /// <param name="streamName">The name of the stream that was requested but not found.</param>
    /// <returns>
    /// A new exception whose message names <paramref name="streamName" /> and whose <see cref="StreamName" /> property
    /// is populated.
    /// </returns>
    internal static CompoundStreamNotFoundException ForName(string streamName) =>
        new(streamName, sentinel: true);
}
