// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.Randomize.LazySource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Extensions;

public sealed partial class IEnumerableExtensionsTests_Randomize
{

    /// <summary>
    /// Verifies that
    /// <see cref="IEnumerableExtensions.Randomize{T}(IEnumerable{T}, RandomizationMode, IRandomGenerator?, int?)" />
    /// with <see cref="RandomizationMode.StreamWindowed" /> handles a lazy iterator that does not implement
    /// <see cref="IReadOnlyCollection{T}" />, yielding a permutation of every source element.
    /// </summary>
    [TestMethod]
    public void Randomize_StreamWindowed_WhenSourceIsLazyIterator_ShouldReturnPermutationOfElements()
    {
        int[] expected = Enumerable.Range(1, 8).ToArray();

        int[] result = LazyYield(8).Randomize(RandomizationMode.StreamWindowed, CreateSeededRng()).ToArray();

        CollectionAssert.AreEquivalent(expected, result);
    }

    private static IEnumerable<int> LazyYield(int count)
    {
        for (int i = 1; i <= count; i++)
            yield return i;
    }

    /// <summary>
    /// Verifies that <see cref="RandomizationMode.StreamWindowed" /> defers source enumeration until the returned
    /// sequence is enumerated, matching the documented deferred-execution contract.
    /// </summary>
    [TestMethod]
    public void Randomize_StreamWindowed_WhenSourceThrowsOnEnumeration_ShouldDeferUntilEnumerated()
    {
        IEnumerable<int> result = ThrowingSource().Randomize(RandomizationMode.StreamWindowed, CreateSeededRng());

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = result.ToArray();
        });
    }

    /// <summary>
    /// Verifies that <see cref="RandomizationMode.ReservoirSample" /> defers source enumeration until the returned
    /// sequence is enumerated, matching the documented deferred-execution contract.
    /// </summary>
    [TestMethod]
    public void Randomize_ReservoirSample_WhenSourceThrowsOnEnumeration_ShouldDeferUntilEnumerated()
    {
        IEnumerable<int> result = ThrowingSource().Randomize(RandomizationMode.ReservoirSample, CreateSeededRng(), 1);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = result.ToArray();
        });
    }

    private static IEnumerable<int> ThrowingSource()
    {
        throw new InvalidOperationException("The source must not be enumerated before the result is.");
#pragma warning disable CS0162 // Unreachable code: required to make this method an iterator.
        yield break;
#pragma warning restore CS0162
    }

}
