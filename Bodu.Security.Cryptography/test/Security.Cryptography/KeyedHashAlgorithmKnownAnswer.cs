// ---------------------------------------------------------------------------------------------------------------
// <copyright file="KeyedHashAlgorithmKnownAnswer.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Represents a single known-answer test vector for a keyed hash family — a named input payload paired with an
/// optional per-row key, an optional descriptive profile tag, and the expected hex-encoded digest produced by the
/// algorithm under test for the specification's selected variant.
/// </summary>
/// <remarks>
/// <para>
/// Instances populate <see cref="KeyedHashAlgorithmKnownAnswers.Vectors" /> on the keyed-hash specification. The
/// per-row <see cref="Key" /> lets a single algorithm variant span multiple keyed test cases — for example, the
/// Skein 1.3 / NIST CD <c>random+MAC</c> KAT entries, where every vector carries its own randomly chosen MAC key
/// of varying length.
/// </para>
/// <para>
/// When <see cref="Key" /> is <see langword="null" /> the harness uses the variant's default key from
/// <see cref="KeyedAlgorithmSpecification.TestKey" />. When <see cref="Key" /> is an empty array the harness
/// assigns the empty-key sentinel that variable-length-optional-key algorithms (Skein, BLAKE2) treat as the
/// canonical unkeyed mode.
/// </para>
/// </remarks>
public sealed record KeyedHashAlgorithmKnownAnswer
{
    /// <summary>Gets the semantic name of this test vector used in diagnostic messages.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the raw input bytes to feed to the algorithm.</summary>
    public required byte[] Input { get; init; }

    /// <summary>Gets the hex-encoded expected digest produced by hashing <see cref="Input" /> with this row's key.</summary>
    public required string ExpectedHex { get; init; }

    /// <summary>
    /// Gets the per-row key applied to the algorithm before computing the digest, or <see langword="null" /> when the
    /// variant's default <see cref="KeyedAlgorithmSpecification.TestKey" /> should be used.
    /// </summary>
    /// <value>The key bytes, an empty array for the unkeyed sentinel, or <see langword="null" /> to defer to the variant default.</value>
    public byte[]? Key { get; init; }

    /// <summary>
    /// Gets an optional free-form profile tag describing the source or category of this vector — for example
    /// <c>"Spec Appendix C"</c>, <c>"NIST CD KAT"</c>, or <c>"BouncyCastle reference"</c>. Surfaced in failure
    /// diagnostics so a test report makes the provenance obvious.
    /// </summary>
    public string? Profile { get; init; }
}
