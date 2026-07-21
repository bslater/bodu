// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SkeinKnownAnswer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Represents a single Skein hash known-answer vector parsed from a Skein 1.3 <c>skein_golden_kat</c> reference file:
/// the Threefish state size, the requested output size, the message, and the expected digest.
/// </summary>
public sealed record SkeinKnownAnswer
    : IKat
{
    /// <summary>
    /// Gets the short, human-readable label identifying this row.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the Skein state (Threefish block) size in bits — 256, 512, or 1024.
    /// </summary>
    public required int StateSizeBits { get; init; }

    /// <summary>
    /// Gets the requested output size in bits.
    /// </summary>
    public required int OutputBits { get; init; }

    /// <summary>
    /// Gets the raw message bytes fed to the algorithm.
    /// </summary>
    public required byte[] Message { get; init; }

    /// <summary>
    /// Gets the expected digest.
    /// </summary>
    public required byte[] Digest { get; init; }

    /// <summary>
    /// Returns the row's diagnostic label.
    /// </summary>
    /// <returns>The value of <see cref="Name" />.</returns>
    public override string ToString() => Name;
}
