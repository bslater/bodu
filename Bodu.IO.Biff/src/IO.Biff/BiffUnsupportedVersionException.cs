// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffUnsupportedVersionException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

/// <summary>
/// The exception thrown when a stream is well-formed BIFF but encoded in a version the codec does not process: a BIFF2,
/// BIFF3, or BIFF4 beginning-of-file record, or a <c>BOF</c> whose version field is neither BIFF5 nor BIFF8.
/// </summary>
/// <remarks>
/// The distinction from <see cref="BiffFormatException" /> is deliberate: the data is not corrupt, it is simply older
/// (or newer) than the two versions the codec implements.
/// </remarks>
public sealed class BiffUnsupportedVersionException
    : NotSupportedException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BiffUnsupportedVersionException" /> class.
    /// </summary>
    public BiffUnsupportedVersionException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BiffUnsupportedVersionException" /> class with the specified
    /// message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public BiffUnsupportedVersionException(string? message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BiffUnsupportedVersionException" /> class with the specified
    /// message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public BiffUnsupportedVersionException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BiffUnsupportedVersionException" /> class with the specified
    /// message and the raw version marker that was encountered.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="rawVersion">
    /// The version value that was rejected: the <c>vers</c> field of a <c>BOF</c> record, or the record identifier of a
    /// legacy beginning-of-file record.
    /// </param>
    public BiffUnsupportedVersionException(string? message, ushort rawVersion)
        : base(message)
    {
        RawVersion = rawVersion;
    }

    /// <summary>
    /// Gets the raw version marker that was rejected.
    /// </summary>
    /// <value>
    /// The <c>vers</c> field of the <c>BOF</c> record, or the identifier of the legacy beginning-of-file record;
    /// <see langword="null" /> when the exception was not raised for a specific marker.
    /// </value>
    public ushort? RawVersion { get; }
}
