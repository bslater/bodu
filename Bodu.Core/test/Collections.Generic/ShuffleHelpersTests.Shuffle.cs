// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ShuffleHelpersTests.Shuffle.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Collections.Generic.Extensions;

namespace Bodu.Collections.Generic;

public partial class ShuffleHelpersTests
{

    /// <summary>
    /// Verifies that shuffling a single-element array does not change its contents.
    /// </summary>
    [TestMethod]
    public void Shuffle_WhenArrayHasOneItem_ShouldReturnSameItem()
    {
        int[] buffer = [42];
        int[] original = buffer.ToArray();

        ShuffleHelpers.Shuffle(buffer, new SystemRandomAdapter());
        CollectionAssert.AreEqual(original, buffer);
    }

    /// <summary>
    /// Verifies that passing a null array to Shuffle throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Shuffle_WhenArrayIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            ShuffleHelpers.Shuffle<int>(null!, new SystemRandomAdapter());
        });
    }

    /// <summary>
    /// Verifies that shuffling a multi-item array retains all elements.
    /// </summary>
    [TestMethod]
    public void Shuffle_WhenCalled_ShouldContainSameElements_UsingArray()
    {
        int[] buffer = Enumerable.Range(1, 50).ToArray();
        int[] original = buffer.ToArray();

        ShuffleHelpers.Shuffle(buffer, new SystemRandomAdapter());
        CollectionAssert.AreEquivalent(original, buffer);
    }

    /// <summary>
    /// Verifies that shuffling a multi-item array changes the original order.
    /// </summary>
    [TestMethod]
    public void Shuffle_WhenCalled_ShouldMutateOriginalOrder_UsingArray()
    {
        int[] buffer = Enumerable.Range(1, 20).ToArray();
        int[] original = buffer.ToArray();

        ShuffleHelpers.Shuffle(buffer, new SystemRandomAdapter());
        AssertOrderChanged(buffer, original, "Array shuffle did not alter order.");
    }

    /// <summary>
    /// Runs 20,000 in-place shuffles of a 10-element array to validate statistical uniformity of output positions. Uses a fixed-seed
    /// <see cref="XorShiftRandom" /> to ensure deterministic, reproducible results. Each value should appear roughly equally in each
    /// position, with no more than 2 statistically significant outliers.
    /// </summary>
    [TestMethod]
    public void Shuffle_WhenRepeated_ShouldDistributeItemsStatistically()
    {
        const int runs = 20000;
        const int size = 10;
        int[,] tracker = new int[size, size];
        int[] original = Enumerable.Range(0, size).ToArray();
        var rng = new XorShiftRandom(12345);

        for (int r = 0; r < runs; r++)
        {
            int[] buffer = original.ToArray();
            ShuffleHelpers.Shuffle(buffer, rng);

            for (int i = 0; i < size; i++)
                tracker[i, buffer[i]]++;
        }

        AssertStatisticalUniformity(tracker, size, label: nameof(ShuffleHelpers.Shuffle));
    }

    /// <summary>
    /// Verifies that passing a null RNG to Shuffle (array) throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Shuffle_WhenRngIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            ShuffleHelpers.Shuffle(new[] { 1, 2, 3 }, null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ShuffleHelpers.Shuffle" /> produces the same permutation when called with an identically seeded
    /// <see cref="XorShiftRandom" />, confirming deterministic behaviour.
    /// </summary>
    [TestMethod]
    public void Shuffle_WithFixedSeed_ShouldProduceDeterministicOutput()
    {
        int[] buffer1 = Enumerable.Range(1, 10).ToArray();
        int[] buffer2 = Enumerable.Range(1, 10).ToArray();

        ShuffleHelpers.Shuffle(buffer1, new XorShiftRandom(42));
        ShuffleHelpers.Shuffle(buffer2, new XorShiftRandom(42));

        CollectionAssert.AreEqual(buffer1, buffer2);
    }
    /// <summary>
    /// Asserts that two arrays differ in order, indicating successful shuffling.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="buffer">The shuffled buffer.</param>
    /// <param name="original">The original buffer before shuffling.</param>
    /// <param name="message">The assertion message to display on failure.</param>
    private static void AssertOrderChanged<T>(T[] buffer, T[] original, string message)
    {
        bool changed = !buffer.SequenceEqual(original);
        Assert.IsTrue(changed, message);
    }

#if !NETSTANDARD2_0

    /// <summary>
    /// Verifies that passing a null RNG to Shuffle (span) throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Shuffle_WhenValidSpanAndRngIsNull_ShouldThrowExactly()
    {
        int[] buffer = new[] { 1, 2, 3 };
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            ShuffleHelpers.Shuffle(buffer.AsSpan(), null!);
        });
    }

    /// <summary>
    /// Verifies that passing a null RNG to Shuffle (memory) throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Shuffle_WhenValidMemoryAndRngIsNull_ShouldThrowExactly()
    {
        int[] buffer = new[] { 1, 2, 3 };
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            ShuffleHelpers.Shuffle(buffer.AsMemory(), null!);
        });
    }

    /// <summary>
    /// Verifies that shuffling an empty span does not throw.
    /// </summary>
    [TestMethod]
    public void Shuffle_WhenSpanIsEmpty_ShouldNotThrow()
    {
        Span<int> span = [];
        ShuffleHelpers.Shuffle(span, new SystemRandomAdapter());
    }

    /// <summary>
    /// Verifies that shuffling a single-element span does not change its contents.
    /// </summary>
    [TestMethod]
    public void Shuffle_WhenSpanHasOneItem_ShouldReturnSameItem()
    {
        int[] buffer = [42];
        int[] original = buffer.ToArray();

        ShuffleHelpers.Shuffle(buffer.AsSpan(), new SystemRandomAdapter());
        CollectionAssert.AreEqual(original, buffer);
    }

    /// <summary>
    /// Verifies that shuffling a multi-item span changes the original order.
    /// </summary>
    [TestMethod]
    public void Shuffle_WhenCalled_ShouldMutateOriginalOrder_UsingSpan()
    {
        int[] buffer = Enumerable.Range(1, 20).ToArray();
        int[] original = buffer.ToArray();

        ShuffleHelpers.Shuffle(buffer.AsSpan(), new SystemRandomAdapter());
        AssertOrderChanged(buffer, original, "Span shuffle did not alter order.");
    }

    /// <summary>
    /// Verifies that shuffling a span retains all original values.
    /// </summary>
    [TestMethod]
    public void Shuffle_WhenCalled_ShouldContainSameElements_UsingSpan()
    {
        int[] buffer = Enumerable.Range(1, 100).ToArray();
        int[] original = buffer.ToArray();

        ShuffleHelpers.Shuffle(buffer.AsSpan(), new SystemRandomAdapter());
        CollectionAssert.AreEquivalent(original, buffer);
    }

    /// <summary>
    /// Verifies that shuffling a memory block retains all original values.
    /// </summary>
    [TestMethod]
    public void Shuffle_WhenCalled_ShouldContainSameElements_UsingMemory()
    {
        int[] buffer = Enumerable.Range(1, 50).ToArray();
        int[] original = buffer.ToArray();

        ShuffleHelpers.Shuffle(buffer.AsMemory(), new SystemRandomAdapter());
        CollectionAssert.AreEquivalent(original, buffer);
    }

    /// <summary>
    /// Verifies that shuffling a memory block changes the original order.
    /// </summary>
    [TestMethod]
    public void Shuffle_WhenCalled_ShouldMutateOriginalOrder_UsingMemory()
    {
        int[] buffer = Enumerable.Range(1, 50).ToArray();
        int[] original = buffer.ToArray();

        ShuffleHelpers.Shuffle(buffer.AsMemory(), new SystemRandomAdapter());
        AssertOrderChanged(buffer, original, "Memory shuffle did not alter order.");
    }

    /// <summary>
    /// Verifies that shuffling a single-element memory block does not change its contents.
    /// </summary>
    [TestMethod]
    public void Shuffle_WhenMemoryHasOneItem_ShouldReturnSameItem()
    {
        int[] buffer = [42];
        int[] original = buffer.ToArray();

        ShuffleHelpers.Shuffle(buffer.AsMemory(), new SystemRandomAdapter());
        CollectionAssert.AreEqual(original, buffer);
    }
#endif


}
