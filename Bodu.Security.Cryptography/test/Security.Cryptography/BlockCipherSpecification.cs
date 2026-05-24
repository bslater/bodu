// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockCipherSpecification.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Describes the expected observable properties of a single <typeparamref name="TBlockCipher" /> variant
/// for use in constructor and behavioural tests.
/// </summary>
public sealed record BlockCipherSpecification
{
    /// <summary>Gets the expected block size in bytes.</summary>
    public required int BlockSize { get; init; }

    /// <summary>Gets the expected key size in bytes.</summary>
    public required int KeySize { get; init; }

    /// <summary>
    /// Gets a deterministic test key of <see cref="KeySize" /> bytes used to construct cipher instances
    /// during tests. Defaults to <see langword="null" />, in which case the base test class generates an
    /// all-zero key of <see cref="KeySize" /> bytes.
    /// </summary>
    public byte[]? TestKey { get; init; } = null;

    /// <summary>
    /// Gets the expected tweak size in bytes, or <c>0</c> if the cipher is not tweakable.
    /// </summary>
    public int TweakSize { get; init; } = 0;

    /// <summary>Gets a value indicating whether this cipher requires a tweak.</summary>
    public bool IsTweakable => TweakSize > 0;

    /// <summary>
    /// Gets a deterministic test tweak of <see cref="TweakSize" /> bytes, or all-zero when
    /// <see langword="null" />. Ignored for non-tweakable ciphers.
    /// </summary>
    public byte[]? TestTweak { get; init; } = null;

    /// <summary>
    /// Gets a value indicating whether the cipher is deterministic — identical inputs always produce
    /// identical outputs regardless of instance or call count. Defaults to <see langword="true" />.
    /// </summary>
    public bool IsDeterministic { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether the cipher supports in-place transformation where the input and
    /// output buffers overlap. Defaults to <see langword="false" />.
    /// </summary>
    public bool SupportsInPlaceTransform { get; init; } = false;

    /// <summary>
    /// Gets the input lengths used to exercise distinct internal algorithm paths during boundary tests.
    /// Defaults to a general set covering sub-block, exact-block, and multi-block lengths derived from
    /// <see cref="BlockSize" /> at runtime when <see langword="null" />.
    /// </summary>
    public IReadOnlyList<int>? BoundaryLengths { get; init; } = null;

    /// <summary>
    /// Gets the boundary lengths to use, falling back to a sensible default derived from
    /// <see cref="BlockSize" /> when <see cref="BoundaryLengths" /> is <see langword="null" />.
    /// </summary>
    public IReadOnlyList<int> GetBoundaryLengths() =>
        BoundaryLengths ??
        [BlockSize - 1, BlockSize, BlockSize * 2, BlockSize * 3 + 1];
}
