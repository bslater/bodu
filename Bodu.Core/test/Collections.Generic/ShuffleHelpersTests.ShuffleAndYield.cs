// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ShuffleHelpersTests.ShuffleAndYield.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Infrastructure;

namespace Bodu.Collections.Generic;

public partial class ShuffleHelpersTests
{

    /// <summary>
    /// Verifies that ShuffleAndYield for an array returns a subset of the specified count, and all returned elements are from the
    /// original array.
    /// </summary>
    [TestMethod]
    public void ShuffleAndYield_Array_ShouldReturnExpectedSubset()
    {
        var buffer = Enumerable.Range(1, 10).ToArray();
        var actual = ShuffleHelpers.ShuffleAndYield(buffer, new XorShiftRandom(), 5).ToArray();

        Assert.HasCount(5, actual);
        CollectionAssert.IsSubsetOf(actual, buffer);
    }

    /// <summary>
    /// Verifies that when count equals array length, all unique items are returned in a different order.
    /// </summary>
    [TestMethod]
    public void ShuffleAndYield_Array_WhenCountEqualsLength_ShouldReturnAllUniqueItems()
    {
        var buffer = Enumerable.Range(1, 10).ToArray();
        var actual = ShuffleHelpers.ShuffleAndYield(buffer, new XorShiftRandom(), buffer.Length).ToArray();

        CollectionAssert.AreEquivalent(buffer, actual);
        CollectionAssert.AllItemsAreUnique(actual);
    }

    /// <summary>
    /// Verifies that <see cref="ShuffleHelpers.ShuffleAndYield" /> for IEnumerable defers execution and does not enumerate the source until iterated.
    /// </summary>
    [TestMethod]
    public void ShuffleAndYield_IEnumerable_ShouldDeferExecution()
    {
        AssertExecutionIsDeferred(
            methodName: "ShuffleAndYield",
            invokeExtensionMethod: src => ShuffleHelpers.ShuffleAndYield(src, new XorShiftRandom(), 2),
            values: [1, 2, 3]);
    }

    /// <summary>
    /// Verifies that <see cref="ShuffleHelpers.ShuffleAndYield" /> only begins enumeration when the actual is iterated.
    /// </summary>
    [TestMethod]
    public void ShuffleAndYield_IEnumerable_ShouldEnumerateOnIteration()
    {
        var enumerated = false;

        var tracking = new TrackingEnumerable<int>(
            source: [1, 2, 3],
            onEnumerate: () => enumerated = true
        );

        _ = ShuffleHelpers.ShuffleAndYield(tracking, new XorShiftRandom(), 2).ToArray();
        Assert.IsTrue(enumerated, "Source should be enumerated upon iteration.");
    }

    /// <summary>
    /// Verifies that <see cref="ShuffleHelpers.ShuffleAndYield" /> for IEnumerable returns the expected count and all elements belong to the source.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(5)]
    [DataRow(10)]
    public void ShuffleAndYield_IEnumerable_ShouldReturnExpectedCount(int count)
    {
        IEnumerable<int> source = Enumerable.Range(1, 10);
        var actual = ShuffleHelpers.ShuffleAndYield(source, new XorShiftRandom(), count).ToArray();

        Assert.HasCount(count, actual);
        CollectionAssert.IsSubsetOf(actual, source.ToArray());
    }
    /// <summary>
    /// Verifies that ShuffleAndYield for IEnumerable returns a subset of the specified count, and all returned elements are from the
    /// original source.
    /// </summary>
    [TestMethod]
    public void ShuffleAndYield_IEnumerable_ShouldReturnExpectedSubset()
    {
        IEnumerable<int> source = Enumerable.Range(1, 10);
        var actual = ShuffleHelpers.ShuffleAndYield(source, new XorShiftRandom(), 3).ToArray();

        Assert.HasCount(3, actual);
        CollectionAssert.IsSubsetOf(actual, source.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="ShuffleHelpers.ShuffleAndYield" /> does not modify the contents of the original array.
    /// </summary>
    [TestMethod]
    public void ShuffleAndYield_ShouldNotMutateOriginalArray()
    {
        var original = Enumerable.Range(1, 10).ToArray();
        var copy = original.ToArray();
        Assert.AreNotSame(original, copy);

        _ = ShuffleHelpers.ShuffleAndYield(original, new XorShiftRandom(), 5).ToArray();
        CollectionAssert.AreEqual(copy, original);
    }

    /// <summary>
    /// Verifies that <see cref="ShuffleHelpers.ShuffleAndYield" /> correctly handles input with duplicate values.
    /// </summary>
    [TestMethod]
    public void ShuffleAndYield_ShouldWorkWithDuplicates()
    {
        var buffer = new[] { 5, 5, 5, 5, 5 };
        var actual = ShuffleHelpers.ShuffleAndYield(buffer, new XorShiftRandom(), 3).ToArray();

        CollectionAssert.AreEqual(new[] { 5, 5, 5 }, actual);
    }

    /// <summary>
    /// Verifies that <see cref="ShuffleHelpers.ShuffleAndYield" /> throws an ArgumentOutOfRangeException when count exceeds the number of elements in the input.
    /// </summary>
    [TestMethod]
    public void ShuffleAndYield_WhenCountExceedsLength_ShouldThrowExactly()
    {
        var buffer = new[] { 1, 2, 3 };
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            ShuffleHelpers.ShuffleAndYield(buffer, new XorShiftRandom(), 4).ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="ShuffleHelpers.ShuffleAndYield" /> throws an ArgumentOutOfRangeException when count is negative.
    /// </summary>
    [TestMethod]
    public void ShuffleAndYield_WhenCountNegative_ShouldThrowExactly()
    {
        var buffer = new[] { 1, 2, 3 };
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            ShuffleHelpers.ShuffleAndYield(buffer, new XorShiftRandom(), -1).ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="ShuffleHelpers.ShuffleAndYield" /> returns an empty array when the input is empty or the count is zero.
    /// </summary>
    [TestMethod]
    [DataRow(0, 0)]
    [DataRow(3, 0)]
    public void ShuffleAndYield_WhenEmptyOrZeroCount_ShouldReturnEmpty(int bufferSize, int count)
    {
        var buffer = Enumerable.Range(1, bufferSize).ToArray();
        var actual = ShuffleHelpers.ShuffleAndYield(buffer, new XorShiftRandom(), count).ToArray();

        if (count == 0 || bufferSize == 0)
            Assert.IsEmpty(actual);
    }

    /// <summary>
    /// Runs 20,000 shuffles using ShuffleAndYield of a 10-element array to validate statistical uniformity of output positions. Uses a
    /// fixed-seed <see cref="XorShiftRandom" /> to ensure deterministic, reproducible results. Each value should appear roughly equally
    /// in each position, with no more than 2 statistically significant outliers.
    /// </summary>
    [TestMethod]
    public void ShuffleAndYield_WhenRepeated_ShouldDistributeItemsStatistically()
    {
        const int runs = 20000;
        const int size = 10;
        var tracker = new int[size, size];
        var original = Enumerable.Range(0, size).ToArray();
        var rng = new XorShiftRandom(12345);

        for (var r = 0; r < runs; r++)
        {
            var shuffled = ShuffleHelpers.ShuffleAndYield(original, rng, size).ToArray();
            for (var i = 0; i < size; i++)
                tracker[i, shuffled[i]]++;
        }

        AssertStatisticalUniformity(tracker, size, label: nameof(ShuffleHelpers.ShuffleAndYield));
    }

    /// <summary>
    /// Verifies that <see cref="ShuffleHelpers.ShuffleAndYield" /> produces the same output sequence when called with an identically
    /// seeded <see cref="XorShiftRandom" />, confirming deterministic behaviour.
    /// </summary>
    [TestMethod]
    public void ShuffleAndYield_WithFixedSeed_ShouldProduceDeterministicOutput()
    {
        var buffer = Enumerable.Range(1, 10).ToArray();
        var result1 = ShuffleHelpers.ShuffleAndYield(buffer, new XorShiftRandom(42), buffer.Length).ToArray();
        var result2 = ShuffleHelpers.ShuffleAndYield(buffer, new XorShiftRandom(42), buffer.Length).ToArray();

        CollectionAssert.AreEqual(result1, result2);
    }

    /// <summary>
    /// Verifies that <see cref="ShuffleHelpers.ShuffleAndYield" /> works correctly with complex reference types and preserves object references.
    /// </summary>
    [TestMethod]
    public void ShuffleAndYield_WithReferenceTypes_ShouldReturnExpectedSubset_UsingArray()
    {
        Person[] source = Enumerable.Range(1, 10).Select(i => new Person(id: i, name: $"Person {i}")).ToArray();
        Person[] actual = ShuffleHelpers.ShuffleAndYield(source, new XorShiftRandom(), 5).ToArray();

        Assert.HasCount(5, actual);
        CollectionAssert.IsSubsetOf(actual, source);

        // Ensure the references point to the original objects
        foreach (Person? person in actual)
        {
            Assert.Contains(person, source);
        }
    }

    /// <summary>
    /// Verifies that <see cref="ShuffleHelpers.ShuffleAndYield" /> works correctly with complex reference types and preserves object references.
    /// </summary>
    [TestMethod]
    public void ShuffleAndYield_WithReferenceTypes_ShouldReturnExpectedSubset_UsingEnumerable()
    {
        IEnumerable<Person> source = Enumerable.Range(1, 10).Select(i => new Person(id: i, name: $"Person {i}")).AsEnumerable();
        Person[] actual = ShuffleHelpers.ShuffleAndYield(source, new XorShiftRandom(), 5).ToArray();

        Assert.HasCount(5, actual);
        CollectionAssert.IsSubsetOf(actual, source.ToArray());

        // Ensure the references point to the original objects
        foreach (Person? person in actual)
        {
            Assert.Contains(person, source);
        }
    }

#if !NETSTANDARD2_0

    /// <summary>
    /// Verifies that ShuffleAndYield for a span returns a subset of the specified count, and all returned elements are from the
    /// original span.
    /// </summary>
    [TestMethod]
    public void ShuffleAndYield_Span_ShouldReturnExpectedSubset()
    {
        Span<int> span = Enumerable.Range(1, 10).ToArray().AsSpan();
        var actual = ShuffleHelpers.ShuffleAndYield<int>(span, new XorShiftRandom(), 4).ToArray();

        Assert.HasCount(4, actual);
        CollectionAssert.IsSubsetOf(actual, span.ToArray());
    }

    /// <summary>
    /// Verifies that ShuffleAndYield for a memory block returns a subset of the specified count, and all returned elements are from the
    /// original memory block.
    /// </summary>
    [TestMethod]
    public void ShuffleAndYield_Memory_ShouldReturnExpectedSubset()
    {
        Memory<int> memory = Enumerable.Range(1, 8).ToArray().AsMemory();
        var actual = ShuffleHelpers.ShuffleAndYield(memory, new XorShiftRandom(), 3).ToArray();

        Assert.HasCount(3, actual);
        CollectionAssert.IsSubsetOf(actual, memory.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="ShuffleHelpers.ShuffleAndYield" /> using a span does not mutate the underlying original array.
    /// </summary>
    [TestMethod]
    public void ShuffleAndYield_Span_ShouldNotModifyOriginalSpan()
    {
        var original = Enumerable.Range(1, 10).ToArray();
        var copy = original.ToArray();

        _ = ShuffleHelpers.ShuffleAndYield<int>(original.AsSpan(), new XorShiftRandom(), 5).ToArray();

        CollectionAssert.AreEqual(copy, original, "Span-based shuffle should not mutate the original array.");
    }

    /// <summary>
    /// Verifies that <see cref="ShuffleHelpers.ShuffleAndYield" /> works correctly with complex reference types and preserves object references.
    /// </summary>
    [TestMethod]
    public void ShuffleAndYield_WithReferenceTypes_ShouldReturnExpectedSubset_UsingSpan()
    {
        Person[] source = Enumerable.Range(1, 10).Select(i => new Person(id: i, name: $"Person {i}")).ToArray();
        Person[] actual = ShuffleHelpers.ShuffleAndYield<Person>(source.AsSpan(), new XorShiftRandom(), 5).ToArray();

        Assert.HasCount(5, actual);
        CollectionAssert.IsSubsetOf(actual, source.ToArray());

        // Ensure the references point to the original objects
        foreach (Person? person in actual)
        {
            Assert.Contains(person, source);
        }
    }

    /// <summary>
    /// Verifies that <see cref="ShuffleHelpers.ShuffleAndYield" /> works correctly with complex reference types and preserves object references.
    /// </summary>
    [TestMethod]
    public void ShuffleAndYield_WithReferenceTypes_ShouldReturnExpectedSubset_UsingMemory()
    {
        Person[] source = Enumerable.Range(1, 10).Select(i => new Person(id: i, name: $"Person {i}")).ToArray();
        Person[] actual = ShuffleHelpers.ShuffleAndYield<Person>(source.AsMemory(), new XorShiftRandom(), 5).ToArray();

        Assert.HasCount(5, actual);
        CollectionAssert.IsSubsetOf(actual, source.ToArray());

        // Ensure the references point to the original objects
        foreach (Person? person in actual)
        {
            Assert.Contains(person, source);
        }
    }
#endif


}
