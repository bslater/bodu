// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AeadKnownAnswer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Represents a single authenticated-encryption known-answer test vector — the inputs (key, nonce, associated data,
/// plaintext) and the expected outputs (ciphertext and authentication tag) for one AEAD trial.
/// </summary>
/// <remarks>
/// The ciphertext and tag are stored detached. <see cref="Layout" /> records how the originating source combined them so
/// a test can reconstruct the source's wire ordering when needed; <see cref="CiphertextWithTag" /> yields the Bodu
/// convention of ciphertext followed by tag. <see cref="Algorithm" /> optionally selects the construction under test for
/// suites that drive several AEAD algorithms from one vector list.
/// </remarks>
public sealed record AeadKnownAnswer : NoncedCipherKnownAnswer
{
    /// <summary>
    /// Gets the associated data authenticated but not encrypted; empty when the trial has no AAD.
    /// </summary>
    public byte[] AssociatedData { get; init; } = [];

    /// <summary>
    /// Gets the expected authentication tag.
    /// </summary>
    public required byte[] Tag { get; init; }

    /// <summary>
    /// Gets the combined-output layout used by the vector's originating source. Defaults to
    /// <see cref="AeadKatOutputLayout.DetachedTag" />.
    /// </summary>
    public AeadKatOutputLayout Layout { get; init; } = AeadKatOutputLayout.DetachedTag;

    /// <summary>
    /// Gets the construction name this vector exercises, or <see langword="null" /> when the consuming suite tests a
    /// single fixed algorithm.
    /// </summary>
    public string? Algorithm { get; init; }

    /// <summary>
    /// Gets the ciphertext followed by the tag, matching the wire format emitted by the Bodu AEAD transforms.
    /// </summary>
    /// <value>The concatenation of <see cref="SymmetricCipherKnownAnswer.Ciphertext" /> and <see cref="Tag" />.</value>
    public byte[] CiphertextWithTag => [.. Ciphertext, .. Tag];
}
