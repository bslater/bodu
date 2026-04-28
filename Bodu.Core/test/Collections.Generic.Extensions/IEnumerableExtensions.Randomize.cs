// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.Randomize.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace Bodu.Collections.Generic.Extensions;

[TestClass]
public sealed partial class IEnumerableExtensionsTests_Randomize
{
    /// <summary>
    /// Builds a deterministic <see cref="IRandomGenerator"/> backed by a seeded <see cref="Random"/>
    /// so tests produce reproducible permutations.
    /// </summary>
    private static IRandomGenerator CreateSeededRng(int seed = 12345) =>
        new SystemRandomAdapter(new Random(seed));

    /// <summary>
    /// Provides <see cref="RandomizationMode"/> values that require a non-null count parameter.
    /// </summary>
    public static IEnumerable<object[]> GetModesRequiringCount() => new[]
    {
        new object[] { RandomizationMode.ReservoirSample },
        new object[] { RandomizationMode.LazyShuffle },
    };

    // =========================================================================
    // Default overload — Randomize<T>(IEnumerable<T>)
    // =========================================================================

    /// <summary>
    /// Verifies that the parameterless <c>Randomize</c> overload returns a permutation containing
    /// exactly the same elements as the source sequence.
    /// </summary>
    [TestMethod]
    public void Randomize_WhenCalled_ForDefaultOverload_ShouldReturnPermutationOfSource()
    {
        int[] source = Enumerable.Range(1, 20).ToArray();

        int[] result = source.Randomize().ToArray();

        CollectionAssert.AreEquivalent(source, result);
    }

    /// <summary>
    /// Verifies that the parameterless <c>Randomize</c> overload throws <see cref="ArgumentNullException" />
    /// when the source is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Randomize_WhenSourceIsNull_ForDefaultOverload_ShouldThrowArgumentNullException()
    {
        IEnumerable<int>? source = null;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source!.Randomize().ToArray();
        });
    }

    // =========================================================================
    // Explicit overload — Randomize<T>(IEnumerable<T>, RandomizationMode, IRandomGenerator, int?)
    // =========================================================================

    /// <summary>
    /// Verifies that <c>Randomize</c> with <see cref="RandomizationMode.BufferAll"/> and no count returns
    /// a permutation containing all source elements.
    /// </summary>
    [TestMethod]
    public void Randomize_WhenModeIsBufferAllAndCountIsNull_ShouldReturnPermutationOfSource()
    {
        int[] source = Enumerable.Range(1, 10).ToArray();

        int[] result = source.Randomize(RandomizationMode.BufferAll, CreateSeededRng()).ToArray();

        CollectionAssert.AreEquivalent(source, result);
    }

    /// <summary>
    /// Verifies that <c>Randomize</c> with <see cref="RandomizationMode.BufferAll"/> and a count returns
    /// exactly that number of elements drawn from the source.
    /// </summary>
    [TestMethod]
    public void Randomize_WhenModeIsBufferAllAndCountIsPositive_ShouldReturnRequestedNumberOfElements()
    {
        int[] source = Enumerable.Range(1, 10).ToArray();

        int[] result = source.Randomize(RandomizationMode.BufferAll, CreateSeededRng(), count: 4).ToArray();

        Assert.AreEqual(4, result.Length);
        foreach (int value in result)
            CollectionAssert.Contains(source, value);
    }

    /// <summary>
    /// Verifies that each mode requiring a count returns exactly the requested number of elements,
    /// all of which are members of the source sequence.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetModesRequiringCount), DynamicDataSourceType.Method)]
    public void Randomize_WhenModeRequiresCount_ShouldReturnRequestedNumberOfElements(RandomizationMode mode)
    {
        int[] source = Enumerable.Range(1, 20).ToArray();

        int[] result = source.Randomize(mode, CreateSeededRng(), count: 5).ToArray();

        Assert.AreEqual(5, result.Length);
        foreach (int value in result)
            CollectionAssert.Contains(source, value);
    }

    /// <summary>
    /// Verifies that <see cref="RandomizationMode.StreamWindowed"/> yields every element of the source,
    /// confirming the method does not drop elements when no count is specified.
    /// </summary>
    [TestMethod]
    public void Randomize_WhenModeIsStreamWindowed_ShouldReturnPermutationOfSource()
    {
        int[] source = Enumerable.Range(1, 30).ToArray();

        int[] result = source.Randomize(RandomizationMode.StreamWindowed, CreateSeededRng()).ToArray();

        CollectionAssert.AreEquivalent(source, result);
    }

    /// <summary>
    /// Verifies that <c>Randomize</c> throws <see cref="ArgumentNullException" /> eagerly when the source is null.
    /// </summary>
    [TestMethod]
    public void Randomize_WhenSourceIsNull_ForExplicitOverload_ShouldThrowArgumentNullException()
    {
        IEnumerable<int>? source = null;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source!.Randomize(RandomizationMode.BufferAll, CreateSeededRng());
        });
    }

    /// <summary>
    /// Verifies that <c>Randomize</c> throws <see cref="ArgumentNullException" /> eagerly when the RNG is null.
    /// </summary>
    [TestMethod]
    public void Randomize_WhenRngIsNull_ShouldThrowArgumentNullException()
    {
        int[] source = { 1, 2, 3 };
        IRandomGenerator? rng = null;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.Randomize(RandomizationMode.BufferAll, rng!);
        });
    }

    /// <summary>
    /// Verifies that a negative count throws <see cref="ArgumentOutOfRangeException" /> eagerly.
    /// </summary>
    [TestMethod]
    public void Randomize_WhenCountIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        int[] source = { 1, 2, 3 };

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = source.Randomize(RandomizationMode.BufferAll, CreateSeededRng(), count: -1);
        });
    }

    /// <summary>
    /// Verifies that an undefined <see cref="RandomizationMode" /> value throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Randomize_WhenModeIsUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        int[] source = { 1, 2, 3 };

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = source.Randomize((RandomizationMode)999, CreateSeededRng());
        });
    }

    /// <summary>
    /// Verifies that <see cref="RandomizationMode.ReservoirSample" /> and <see cref="RandomizationMode.LazyShuffle" />
    /// throw <see cref="ArgumentException" /> with parameter name <c>count</c> when count is null.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetModesRequiringCount), DynamicDataSourceType.Method)]
    public void Randomize_WhenCountIsNullForCountRequiringMode_ShouldThrowArgumentException(RandomizationMode mode)
    {
        int[] source = { 1, 2, 3 };

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = source.Randomize(mode, CreateSeededRng(), count: null);
        });

        Assert.AreEqual("count", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <c>Randomize</c> with <see cref="RandomizationMode.BufferAll" /> and a count that exceeds the
    /// number of available elements throws <see cref="ArgumentException" /> via the
    /// <c>ThrowIfGreaterThanOther</c> validation inside the buffered implementation.
    /// </summary>
    [TestMethod]
    public void Randomize_WhenCountExceedsSourceLength_ForBufferAllMode_ShouldThrowArgumentException()
    {
        int[] source = { 1, 2, 3 };

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = source.Randomize(RandomizationMode.BufferAll, CreateSeededRng(), count: 10).ToArray();
        });
    }

    /// <summary>
    /// Verifies that <see cref="RandomizationMode.ReservoirSample" /> throws <see cref="ArgumentOutOfRangeException" />
    /// when <c>count</c> exceeds the number of available elements, confirming the reservoir-fill contract.
    /// </summary>
    [TestMethod]
    public void Randomize_WhenCountExceedsSourceLength_ForReservoirSampleMode_ShouldThrowArgumentOutOfRangeException()
    {
        int[] source = { 1, 2, 3 };

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = source.Randomize(RandomizationMode.ReservoirSample, CreateSeededRng(), count: 10).ToArray();
        });
    }

    /// <summary>
    /// Verifies that <c>Randomize</c> in <see cref="RandomizationMode.StreamWindowed" /> mode defers execution —
    /// the source is not enumerated until the returned sequence is consumed.
    /// </summary>
    [TestMethod]
    public void Randomize_WhenModeIsStreamWindowed_ShouldDeferExecution()
    {
        bool wasEnumerated = false;

        IEnumerable<int> source = Tracked();

        IEnumerable<int> deferred = source.Randomize(RandomizationMode.StreamWindowed, CreateSeededRng());

        Assert.IsFalse(wasEnumerated, "Randomize with StreamWindowed should defer execution until enumeration.");

        _ = deferred.ToArray();

        Assert.IsTrue(wasEnumerated, "Randomize should enumerate the source on ToArray().");

        IEnumerable<int> Tracked()
        {
            wasEnumerated = true;
            yield return 1;
            yield return 2;
            yield return 3;
        }
    }

    /// <summary>
    /// Verifies that invoking <c>Randomize</c> with a null RNG throws immediately, before the sequence is enumerated,
    /// confirming the eager-validation contract.
    /// </summary>
    [TestMethod]
    public void Randomize_WhenRngIsNull_ShouldThrowImmediatelyWithoutEnumeratingSource()
    {
        bool wasEnumerated = false;

        IEnumerable<int> source = Tracked();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.Randomize(RandomizationMode.BufferAll, null!);
        });

        Assert.IsFalse(wasEnumerated, "Eager validation must run before the source is enumerated.");

        IEnumerable<int> Tracked()
        {
            wasEnumerated = true;
            yield return 1;
        }
    }
}
