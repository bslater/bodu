// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ProbabilisticHashing.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace Bodu.Collections.Probabilistic;

/// <summary>
/// Provides the shared hash-derivation routine used by the probabilistic sketch types (<see cref="BloomFilter{T}" />
/// and future sketches) to expand an element's comparer-supplied hash code into the two 64-bit values required for
/// Kirsch–Mitzenmacher double hashing.
/// </summary>
/// <remarks>
/// <para>
/// The sketches derive all hash material from <see cref="IEqualityComparer{T}.GetHashCode(T)" />, so every element
/// contributes at most 32 bits of entropy. The two 64-bit values are produced by seeding a SplitMix64-style avalanche
/// with the comparer hash, which decorrelates the bit positions selected by consecutive probe indexes but cannot
/// manufacture entropy the comparer did not supply. This is standard practice for comparer-based sketches; it bounds
/// the achievable false-positive floor by the collision rate of the underlying 32-bit hash.
/// </para>
/// <para>
/// The expansion itself is deterministic and platform-stable. Overall stability therefore depends solely on the
/// comparer: <see cref="string.GetHashCode()" /> is randomized per process, so sketches over <see cref="string" />
/// keyed by the default comparer produce process-local bit patterns (see the sketch-level documentation for the
/// export/import implications).
/// </para>
/// </remarks>
internal static class ProbabilisticHashing
{
    /// <summary>The SplitMix64 sequence increment (the 64-bit golden-ratio constant), used to separate the two seeds.</summary>
    private const ulong GoldenGamma = 0x9E3779B97F4A7C15UL;

    /// <summary>Twice <see cref="GoldenGamma" /> modulo 2⁶⁴ — the second seed offset in the SplitMix64 sequence.</summary>
    private const ulong GoldenGamma2 = 0x3C6EF372FE94F82AUL;

    /// <summary>
    /// Derives the two 64-bit hash values for <paramref name="item" /> used to drive Kirsch–Mitzenmacher double hashing
    /// (<c>g_i = h1 + i · h2</c>).
    /// </summary>
    /// <typeparam name="T">The type of the element being hashed.</typeparam>
    /// <param name="item">
    /// The element to hash. Callers guarantee non-null; the public sketch APIs reject <see langword="null" /> items.
    /// </param>
    /// <param name="comparer">
    /// The equality comparer supplying the element's hash code. Must not be <see langword="null" />.
    /// </param>
    /// <param name="h1">When the method returns, contains the first derived 64-bit hash value.</param>
    /// <param name="h2">
    /// When the method returns, contains the second derived 64-bit hash value. Always odd, so the probe stride never
    /// degenerates to zero.
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void DeriveHashPair<T>(T item, IEqualityComparer<T> comparer, out ulong h1, out ulong h2)
        where T : notnull
    {
        var seed = (ulong)(uint)comparer.GetHashCode(item);

        h1 = Mix64(seed + GoldenGamma);
        h2 = Mix64(seed + GoldenGamma2) | 1UL;
    }

    /// <summary>
    /// Applies the SplitMix64 finalizer to <paramref name="value" />, producing a well-avalanched 64-bit hash.
    /// </summary>
    /// <param name="value">The value to mix.</param>
    /// <returns>The avalanched 64-bit result; every input bit influences every output bit.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong Mix64(ulong value)
    {
        value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
        value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;

        return value ^ (value >> 31);
    }
}
