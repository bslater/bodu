// ---------------------------------------------------------------------------------------------------------------
// <copyright file="KeyedHashAlgorithmKnownAnswers.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Declares the keyed known-answer test vectors associated with a keyed-hash specification variant. Sits alongside
/// <see cref="HashAlgorithmKnownAnswers" /> on <see cref="KeyedAlgorithmSpecification" /> so unkeyed shared inputs and
/// keyed per-row vectors stay clearly separated in the spec record.
/// </summary>
/// <remarks>
/// The keyed harness flattens every entry in <see cref="Vectors" /> into a data-driven test row, applying the row's
/// <see cref="KeyedHashAlgorithmKnownAnswer.Key" /> (or the variant default) to the algorithm before computing the
/// digest and comparing against the expected hex.
/// </remarks>
public sealed record KeyedHashAlgorithmKnownAnswers
{
    /// <summary>
    /// Gets the keyed known-answer vectors for this variant. Defaults to an empty list, in which case the keyed harness
    /// emits no per-row assertions for the variant.
    /// </summary>
    public IReadOnlyList<KeyedHashAlgorithmKnownAnswer> Vectors { get; init; } =
        Array.Empty<KeyedHashAlgorithmKnownAnswer>();
}
