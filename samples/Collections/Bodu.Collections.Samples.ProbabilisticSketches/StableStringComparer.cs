// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StableStringComparer.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Samples.ProbabilisticSketches;

/// <summary>
/// An ordinal <see cref="string" /> equality comparer with a process-stable hash code. The probabilistic
/// sketches derive all of their bit positions from <see cref="IEqualityComparer{T}.GetHashCode(T)" />, and the
/// BCL <see cref="string.GetHashCode()" /> is randomized per process — so a default comparer would make every
/// sketch produce process-local bit patterns and non-reproducible estimates. Supplying this FNV-1a comparer
/// pins the hashing so the sample prints identical output (and the same false positive) every run.
/// </summary>
public sealed class StableStringComparer
    : IEqualityComparer<string>
{
    /// <summary>The 32-bit FNV-1a offset basis.</summary>
    private const uint OffsetBasis = 2166136261u;

    /// <summary>The 32-bit FNV-1a prime.</summary>
    private const uint Prime = 16777619u;

    /// <summary>
    /// Determines whether two strings are equal using ordinal comparison.
    /// </summary>
    /// <param name="x">The first string to compare.</param>
    /// <param name="y">The second string to compare.</param>
    /// <returns><see langword="true" /> if the strings are ordinally equal; otherwise, <see langword="false" />.</returns>
    public bool Equals(string? x, string? y) =>
        string.Equals(x, y, StringComparison.Ordinal);

    /// <summary>
    /// Computes a process-stable FNV-1a hash over the string's UTF-16 code units.
    /// </summary>
    /// <param name="obj">The string to hash.</param>
    /// <returns>A deterministic hash code that does not vary between processes.</returns>
    public int GetHashCode(string obj)
    {
        // FNV-1a: fold each code unit into the running hash - deterministic across runs and machines.
        var hash = OffsetBasis;
        foreach (var ch in obj)
            hash = (hash ^ ch) * Prime;

        return unchecked((int)hash);
    }
}
