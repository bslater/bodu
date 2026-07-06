// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Rfc8439TestVector.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Represents a single parsed RFC 8439 appendix test vector — its block number, the labelled hex-dump fields keyed by
/// their heading (for example <c>Key</c>, <c>Nonce</c>, <c>Plaintext</c>, <c>Ciphertext</c>), and the optional
/// <c>Initial Block Counter</c> scalar.
/// </summary>
/// <param name="Number">The <c>Test Vector #N</c> block number within its appendix section.</param>
/// <param name="Fields">The labelled byte fields decoded from the block's hex dumps.</param>
/// <param name="InitialBlockCounter">The declared initial block counter, or <see langword="null" /> when absent.</param>
public sealed record Rfc8439TestVector(
    int Number,
    IReadOnlyDictionary<string, byte[]> Fields,
    uint? InitialBlockCounter)
{
    /// <summary>Returns the bytes of the field named <paramref name="label" />.</summary>
    /// <param name="label">The field heading, matched case-insensitively.</param>
    /// <returns>The decoded bytes for that field.</returns>
    /// <exception cref="KeyNotFoundException">No field with the given label is present.</exception>
    public byte[] Field(string label) =>
        Fields.TryGetValue(label, out byte[]? value)
            ? value
            : throw new KeyNotFoundException($"RFC 8439 test vector #{Number} has no '{label}' field.");
}
