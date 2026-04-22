// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashAlgorithmSpecification.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Describes the expected observable properties of a single <typeparamref name="TAlgorithm" /> variant for use in
/// constructor and behavioural tests.
/// </summary>
public record HashAlgorithmSpecification
{
    /// <summary>Gets the expected <see cref="HashAlgorithm.HashSize" /> in bits.</summary>
    public required int HashSize { get; init; }

    /// <summary>Gets the expected <see cref="HashAlgorithm.InputBlockSize" /> in bytes.</summary>
    public required int InputBlockSize { get; init; }

    /// <summary>Gets the expected <see cref="HashAlgorithm.OutputBlockSize" /> in bytes.</summary>
    public required int OutputBlockSize { get; init; }

    /// <summary>Gets the expected <see cref="HashAlgorithm.CanReuseTransform" /> value. Defaults to <see langword="true" />.</summary>
    public bool CanReuseTransform { get; init; } = true;

    /// <summary>Gets the expected <see cref="HashAlgorithm.CanTransformMultipleBlocks" /> value. Defaults to <see langword="true" />.</summary>
    public bool CanTransformMultipleBlocks { get; init; } = true;

    /// <summary>
    /// Gets the input lengths used to exercise distinct internal algorithm paths during hash distribution tests.
    /// Defaults to a general-purpose set suitable for most streaming hash algorithms; override for algorithms
    /// with well-defined internal path boundaries such as CityHash.
    /// </summary>
    public IReadOnlyList<int> BoundaryLengths { get; init; } = [1, 8, 16, 64];

    /// <summary>
    /// Gets the input length used to exercise the long/iterative internal path of the algorithm. Should be large
    /// enough to guarantee the iterative code path is reached. Defaults to 200 bytes.
    /// </summary>
    public int LongInputLength { get; init; } = 200;

    /// <summary>
    /// Gets the minimum number of non-zero bytes required in the output hash when hashing a long, varied input.
    /// Defaults to half of <c>HashSize / 8</c>, but may be set higher for algorithms with strong avalanche
    /// properties where full-byte entropy is expected.
    /// </summary>
    public int? MinNonZeroBytesForLongInput { get; init; } = null;

    /// <summary>
    /// Gets a value indicating whether the algorithm maintains state across transform operations.
    /// Stateless algorithms produce identical output for identical input regardless of prior hashing operations.
    /// Stateful algorithms must ensure correctness across reinitialization and partial block input.
    /// Defaults to <see langword="false" />.
    /// </summary>
    public bool IsStateless { get; init; } = false;
}