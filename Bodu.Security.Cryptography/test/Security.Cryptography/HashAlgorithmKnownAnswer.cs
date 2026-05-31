// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashAlgorithmKnownAnswer.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Represents a single algorithm-specific known-answer test vector — a named input payload paired with the
/// expected hex-encoded digest produced by the algorithm under test.
/// </summary>
/// <remarks>
/// Instances populate the <see cref="HashAlgorithmKnownAnswers.Additional" /> collection and let concrete test
/// classes contribute algorithm-specific vectors (for example, the ISO test set for Whirlpool, or the Tiger
/// designer's <c>"Tiger"</c> input) without bespoke <c>[DataRow]</c> plumbing. Common shared inputs — empty,
/// <c>"ABC"</c>, the quick-brown-fox sentence, sixteen zero bytes, and the <c>0x00..0xFE</c> sequence — are
/// already covered by the typed slots on <see cref="HashAlgorithmKnownAnswers" /> and should not be duplicated
/// here.
/// </remarks>
public sealed record HashAlgorithmKnownAnswer : IKat
{
    /// <summary>Gets the semantic name of this test vector used in diagnostic messages.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the raw input bytes to feed to the algorithm.</summary>
    public required byte[] Input { get; init; }

    /// <summary>Gets the hex-encoded expected digest produced by hashing <see cref="Input" />.</summary>
    public required string ExpectedHex { get; init; }
}
