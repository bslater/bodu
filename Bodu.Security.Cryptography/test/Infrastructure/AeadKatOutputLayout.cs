// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AeadKatOutputLayout.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Describes how a known-answer vector's combined output is laid out in its source, so a test can assert the Bodu
/// <c>ciphertext ‖ tag</c> convention against a source that may use a different order.
/// </summary>
public enum AeadKatOutputLayout
{
    /// <summary>
    /// The source represents ciphertext and tag separately (detached).
    /// </summary>
    DetachedTag,

    /// <summary>
    /// The source combined output is the ciphertext followed by the tag — the Bodu convention.
    /// </summary>
    CiphertextThenTag,

    /// <summary>
    /// The source combined output is the tag followed by the ciphertext — the libsodium secretbox convention.
    /// </summary>
    TagThenCiphertext,
}
