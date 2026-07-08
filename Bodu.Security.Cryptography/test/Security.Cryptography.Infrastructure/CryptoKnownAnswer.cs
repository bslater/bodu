// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoKnownAnswer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Serves as the common root for every cryptographic known-answer test vector in the suite. Carries the two concerns
/// shared by all KAT families — the human-readable <see cref="Name" /> that labels the row in test output and the
/// optional <see cref="Provenance" /> that records where the vector came from.
/// </summary>
/// <remarks>
/// <para>
/// Concrete vectors derive from this root through a small, operation-shaped hierarchy rather than defining a bespoke
/// record per algorithm: secret-key operations extend <see cref="KeyedKnownAnswer" />; asymmetric operations extend
/// <see cref="AsymmetricKnownAnswer" />. Every leaf record uses object-initializer (<c>required</c> property)
/// construction so the shared <see cref="Name" /> and <see cref="Provenance" /> compose uniformly with the
/// family-specific fields.
/// </para>
/// <para>
/// Vector bytes are stored decoded (<see cref="byte" /> arrays). Declare literals tersely with the
/// <see cref="KatBytes.Hex(string)" /> helper imported via <c>using static</c>.
/// </para>
/// </remarks>
public abstract record CryptoKnownAnswer : IKat
{
    /// <summary>
    /// Gets the short, human-readable label that identifies this vector in test output.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the provenance of this vector, or <see langword="null" /> when the source is unspecified.
    /// </summary>
    /// <value>
    /// A <see cref="KatProvenance" /> describing the source kind and citation, or <see langword="null" />.
    /// </value>
    public KatProvenance? Provenance { get; init; }
}
