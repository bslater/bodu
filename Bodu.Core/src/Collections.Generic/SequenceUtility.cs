// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequenceUtility.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Internal;

/// <summary>
/// Provides internal helper methods for working with enumerable sequences.
/// </summary>
internal static class SequenceUtility
{
    /// <summary>
    /// Ensures that the given sequence can be safely enumerated more than once by materializing it if needed.
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequence.</typeparam>
    /// <param name="sequence">The sequence to evaluate. Must not be <see langword="null" />.</param>
    /// <returns>The original sequence if it is already a collection; otherwise, a materialized array.</returns>
    /// <remarks>
    /// <para>
    /// This method assumes <paramref name="sequence" /> is non-null. The caller is responsible for validating the
    /// input.
    /// </para>
    /// <para>
    /// On .NET 6.0 or later,
    /// <see cref="System.Linq.Enumerable.TryGetNonEnumeratedCount{TSource}(IEnumerable{TSource}, out int)" /> is used
    /// to avoid unnecessary allocations for known-length sequences.
    /// </para>
    /// </remarks>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<T> EnsureMaterialized<T>(IEnumerable<T> sequence) =>
#if NET6_0_OR_GREATER
        sequence.TryGetNonEnumeratedCount(out _)
            ? sequence
            : sequence.ToArray();

#else
        sequence is ICollection<T>
            ? sequence
            : sequence.ToArray();
#endif
}
