// ---------------------------------------------------------------------------------------------------------------
// <copyright file="KeyedHashAlgorithmSpecification .cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public record KeyedAlgorithmSpecification
    : HashAlgorithmSpecification
{
    /// <summary>Gets the minimum accepted key length in bytes.</summary>
    public required int MinKeyLength { get; init; }

    /// <summary>Gets the maximum accepted key length in bytes.</summary>
    public required int MaxKeyLength { get; init; }

    /// <summary>
    /// Gets the set of valid key lengths explicitly accepted by the algorithm. When empty, all lengths between
    /// <see cref="MinKeyLength" /> and <see cref="MaxKeyLength" /> inclusive are assumed to be valid.
    /// </summary>
    public IReadOnlyList<int> ValidKeyLengths { get; init; } = [];

    /// <summary>
    /// Gets a valid test key used to initialise the algorithm during fixture setup. Should be a well-known,
    /// deterministic value of <see cref="MaxKeyLength" /> bytes so all code paths are exercised.
    /// </summary>
    public required byte[] TestKey { get; init; }

    /// <summary>
    /// Gets the keyed known-answer test vectors associated with this variant. Each entry may carry its own per-row
    /// key, or fall back to <see cref="TestKey" /> when <see cref="KeyedHashAlgorithmKnownAnswer.Key" /> is
    /// <see langword="null" />.
    /// </summary>
    /// <value>
    /// A <see cref="KeyedHashAlgorithmKnownAnswers" /> record carrying per-row keyed vectors. Defaults to an empty
    /// record, in which case the keyed harness emits no per-row assertions for this variant.
    /// </value>
    public KeyedHashAlgorithmKnownAnswers KeyedAnswers { get; init; } = new();

    /// <summary>
    /// Gets a value indicating whether the algorithm requires an exact key length. <see langword="true" /> when
    /// <see cref="MinKeyLength" /> equals <see cref="MaxKeyLength" />.
    /// </summary>
    public bool RequiresFixedKeyLength => MinKeyLength == MaxKeyLength;

    /// <summary>
    /// Gets a value indicating whether the algorithm accepts variable-length keys.
    /// </summary>
    public bool AcceptsVariableLengthKeys => MinKeyLength != MaxKeyLength;

    /// <summary>
    /// Returns all key lengths that should be accepted by the algorithm — either the explicit
    /// <see cref="ValidKeyLengths" /> list, or every length between <see cref="MinKeyLength" /> and
    /// <see cref="MaxKeyLength" /> inclusive when no explicit list is provided.
    /// </summary>
    public IEnumerable<int> GetAcceptedKeyLengths()
        => ValidKeyLengths.Count > 0
            ? ValidKeyLengths
            : Enumerable.Range(MinKeyLength, MaxKeyLength - MinKeyLength + 1);

    /// <summary>
    /// Returns a representative set of key lengths that should be rejected by the algorithm — lengths just
    /// outside the valid range, useful for negative testing.
    /// </summary>
    /// <remarks>
    /// The zero-length entry is emitted only when <see cref="MinKeyLength" /> is strictly positive, allowing
    /// variable-length-optional-key algorithms (such as Skein, where an empty key selects an unkeyed mode) to
    /// declare <c>MinKeyLength = 0</c> without causing the length-rejection test to assert that an empty key throws.
    /// </remarks>
    public IEnumerable<int> GetRejectedKeyLengths()
    {
        if (MinKeyLength > 0)
        {
            yield return MinKeyLength - 1;
            yield return 0;
        }

        yield return MaxKeyLength + 1;
    }
}
