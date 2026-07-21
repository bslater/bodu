// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XofKnownAnswer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Represents a single known-answer test vector for an extendable output function (XOF) or customizable XOF (CXOF) — a
/// message, an optional customization string, and the expected variable-length output — as parsed from a NIST
/// Lightweight Cryptography <c>LWC_XOF_KAT</c> / <c>LWC_CXOF_KAT</c> reference file.
/// </summary>
public sealed record XofKnownAnswer
    : IKat
{
    /// <summary>
    /// Gets the short, human-readable label identifying this row (for example <c>"CXOF Count 45"</c>).
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the raw message bytes fed to the XOF.
    /// </summary>
    public required byte[] Message { get; init; }

    /// <summary>
    /// Gets the customization string absorbed before the message, or <see langword="null" /> for a plain
    /// (non-customized) XOF vector that has no <c>Z</c> field.
    /// </summary>
    public byte[]? Customization { get; init; }

    /// <summary>
    /// Gets the expected output bytes (the reference <c>MD</c> field), whose length sets the squeeze length.
    /// </summary>
    public required byte[] Digest { get; init; }

    /// <summary>
    /// Gets an optional human-readable citation propagated from the source file for failure diagnostics.
    /// </summary>
    public string? Source { get; init; }

    /// <summary>
    /// Returns the row's diagnostic label.
    /// </summary>
    /// <returns>The value of <see cref="Name" />.</returns>
    public override string ToString() => Name;
}
