// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.Randomize.StreamWindowed.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Extensions;

public sealed partial class IEnumerableExtensionsTests_Randomize
{

    /// <summary>
    /// Verifies that <see cref="RandomizationMode.StreamWindowed" /> yields every source element when the source is
    /// exactly the internal window size (64 elements), so the streaming-replacement loop body is never entered and only
    /// the final flush is exercised.
    /// </summary>
    [TestMethod]
    public void Randomize_StreamWindowed_WhenSourceEqualsWindowSize_ShouldReturnPermutationOfSource()
    {
        const int windowSize = 64;
        var source = Enumerable.Range(1, windowSize).ToArray();

        var result = source
            .Randomize(RandomizationMode.StreamWindowed, CreateSeededRng())
            .ToArray();

        Assert.HasCount(windowSize, result, "All source elements must be yielded when source == window size.");
        CollectionAssert.AreEquivalent(source, result);
    }

    /// <summary>
    /// Verifies that <see cref="RandomizationMode.StreamWindowed" /> applied to a source larger than the window size
    /// produces a permutation that genuinely differs from the input order — confirming the streaming-replacement loop
    /// actually permutes the sequence rather than yielding the input verbatim.
    /// </summary>
    [TestMethod]
    public void Randomize_StreamWindowed_WhenSourceExceedsWindowSize_ShouldNotYieldSourceInExactlyOriginalOrder()
    {
        var source = Enumerable.Range(1, 200).ToArray();

        var result = source
            .Randomize(RandomizationMode.StreamWindowed, CreateSeededRng())
            .ToArray();

        Assert.IsFalse(source.SequenceEqual(result),
            "Stream-windowed shuffle of 200 items must permute the input rather than yield it in original order.");
    }

    /// <summary>
    /// Verifies that <see cref="RandomizationMode.StreamWindowed" /> yields a permutation of the entire source when the
    /// source is larger than the internal 64-element window, exercising the streaming-replacement loop body that swaps
    /// incoming elements into random window slots before yielding the displaced value.
    /// </summary>
    [TestMethod]
    public void Randomize_StreamWindowed_WhenSourceExceedsWindowSize_ShouldReturnPermutationOfSource()
    {
        // The window size baked into StreamWindowedShuffle is 64; produce a source that comfortably
        // overflows the initial fill so the streaming-replacement loop runs many times.
        var source = Enumerable.Range(1, 200).ToArray();

        var result = source
            .Randomize(RandomizationMode.StreamWindowed, CreateSeededRng())
            .ToArray();

        Assert.HasCount(source.Length, result,
            "Stream-windowed shuffle must yield every source element exactly once.");
        CollectionAssert.AreEquivalent(source, result);
    }

    /// <summary>
    /// Verifies that <see cref="RandomizationMode.StreamWindowed" /> with an empty source yields no items and never
    /// enters the streaming-replacement loop.
    /// </summary>
    [TestMethod]
    public void Randomize_StreamWindowed_WhenSourceIsEmpty_ShouldYieldNoItems()
    {
        var source = Array.Empty<int>();

        var result = source
            .Randomize(RandomizationMode.StreamWindowed, CreateSeededRng())
            .ToArray();

        Assert.IsEmpty(result);
    }

}
