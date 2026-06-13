// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonCryptographicHashAlgorithmSpecification.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO.Hashing;

namespace Bodu.IO.Hashing;

/// <summary>
/// Describes the expected observable properties of a single <see cref="NonCryptographicHashAlgorithm" /> variant for
/// use in constructor and behavioural tests.
/// </summary>
/// <remarks>
/// Instances are supplied by derived test classes via
/// <see cref="NonCryptographicHashAlgorithmTests{TTest, TAlgorithm, TVariant}.GetSpecification(TVariant)" /> and drive
/// assertions that are common across every non-cryptographic hash algorithm under test — output width, optional block
/// size, and distribution-test boundary parameters.
/// </remarks>
public record NonCryptographicHashAlgorithmSpecification
{

    /// <summary>
    /// Gets the expected algorithm name string, when the concrete type exposes one via its own surface.
    /// </summary>
    /// <value>
    /// The canonical algorithm name (for example, <c>Fletcher-16</c>), or <see langword="null" /> when the algorithm
    /// does not publish a name and name checks should be skipped.
    /// </value>
    public string? AlgorithmName { get; init; }

    /// <summary>
    /// Gets the expected block size, in bytes, for algorithms that derive from
    /// <see cref="BlockNonCryptographicHashAlgorithm{T}" />.
    /// </summary>
    /// <value>The configured block size, or <see langword="null" /> for non-block algorithms.</value>
    public int? BlockSizeBytes { get; init; }

    /// <summary>
    /// Gets the input lengths used to exercise distinct internal algorithm paths during hash distribution tests.
    /// Defaults to a general-purpose set suitable for most streaming hash algorithms; override for algorithms with
    /// well-defined internal path boundaries.
    /// </summary>
    public IReadOnlyList<int> BoundaryLengths { get; init; } = [1, 8, 16, 64];
    /// <summary>
    /// Gets the expected <see cref="NonCryptographicHashAlgorithm.HashLengthInBytes" />.
    /// </summary>
    public required int HashLengthInBytes { get; init; }

    /// <summary>
    /// Optional override for the upper bound (in bytes) of the dense incremental-input test. When
    /// <see langword="null" />, the test defaults to <c>HashLengthInBytes * 4</c> for block-based algorithms and
    /// <c>16</c> for byte-stream algorithms (<see cref="HashLengthInBytes" /> ≤ 1). Override for hierarchical hashes
    /// (e.g. Blake3 chunks, KangarooTwelve leaves) so the test crosses every state-machine boundary.
    /// </summary>
    public int? IncrementalCoverageBytes { get; init; }

    /// <summary>
    /// Gets a value indicating whether the algorithm implements <see cref="IResumableHashAlgorithm" /> and supports
    /// continuing a previous finalised hash with additional input data.
    /// </summary>
    /// <value>
    /// <see langword="true" /> if the algorithm exposes resume semantics; otherwise, <see langword="false" />. Defaults
    /// to <see langword="false" />.
    /// </value>
    public bool IsResumable { get; init; } = false;

    /// <summary>
    /// Gets a value indicating whether the algorithm produces the same output regardless of any prior hashing
    /// operations performed on the instance.
    /// </summary>
    /// <value>
    /// <see langword="true" /> if the algorithm is entirely stateless; otherwise, <see langword="false" />. Defaults to
    /// <see langword="false" />.
    /// </value>
    public bool IsStateless { get; init; } = false;

    /// <summary>
    /// Gets the known-answer test vectors associated with this variant.
    /// </summary>
    /// <value>
    /// A <see cref="NonCryptographicHashKnownAnswers" /> record carrying the expected digests for the shared inputs and
    /// any algorithm-specific extension vectors. Defaults to an empty record, in which case the harness emits no
    /// named-input assertions for this variant.
    /// </value>
    public NonCryptographicHashKnownAnswers KnownAnswers { get; init; } = new();

    /// <summary>
    /// Gets the input length used to exercise the long/iterative internal path of the algorithm. Should be large enough
    /// to guarantee the iterative code path is reached. Defaults to 200 bytes.
    /// </summary>
    public int LongInputLength { get; init; } = 200;

    /// <summary>
    /// Gets the minimum number of non-zero bytes required in the output hash when hashing a long, varied input.
    /// Defaults to <see langword="null" />, in which case the base test uses half of <see cref="HashLengthInBytes" />.
    /// </summary>
    public int? MinNonZeroBytesForLongInput { get; init; } = null;

}
