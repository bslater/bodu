// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AlgorithmSpecification{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Serves as the base for the per-variant specification records that describe an algorithm's expected observable
/// properties, carrying the variant's curated known-answer test vectors in a single uniform <see cref="KnownAnswers" />
/// slot.
/// </summary>
/// <typeparam name="TKat">The known-answer vector type this algorithm family is exercised with.</typeparam>
/// <remarks>
/// <para>
/// Each algorithm family derives a concrete specification (for example <c>HashAlgorithmSpecification</c> or
/// <c>BlockCipherSpecification</c>) that adds its family-specific properties — key sizes, block sizes, boundary lengths
/// — and binds <typeparamref name="TKat" /> to its operation-shaped vector type. Contract test bases read
/// <see cref="KnownAnswers" /> uniformly, so the same data-driven harness drives every family.
/// </para>
/// <para>
/// The slot holds the curated, in-memory vectors that travel with the specification. Large external corpora (NIST ACVP,
/// Wycheproof, NIST LWC) remain lazily file-loaded by their dedicated loaders and are bound to the regression-tier test
/// sites directly rather than materialized onto the specification.
/// </para>
/// </remarks>
public abstract record AlgorithmSpecification<TKat>
    where TKat : CryptoKnownAnswer
{
    /// <summary>
    /// Gets the curated known-answer test vectors associated with this variant. Defaults to an empty collection, in
    /// which case the harness emits no vector assertions for the variant.
    /// </summary>
    /// <value>The in-memory vectors carried by this specification.</value>
    public IReadOnlyList<TKat> KnownAnswers { get; init; } = [];
}
