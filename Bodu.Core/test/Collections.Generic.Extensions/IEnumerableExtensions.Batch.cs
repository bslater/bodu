// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.Batch.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Extensions;

[TestClass]
public sealed partial class IEnumerableExtensionsTests_Batch : EnumerableTests
{
    public static IEnumerable<object[]> GetBatchTestCases() =>
    [
        new object[]
        {
            new EnumerableTestPlan<int>(
                name: "Batch - even split",
                source: Enumerable.Range(1, 10),
                invoke: source => source.Batch(2),
                expectedResult: new[]
                {
                    new[] { 1, 2 },
                    [3, 4],
                    [5, 6],
                    [7, 8],
                    [9, 10]
                }
            )
        },
        [
            new EnumerableTestPlan<int>(
                name: "Batch - uneven split",
                source: Enumerable.Range(1, 10),
                invoke: source => source.Batch(3),
                expectedResult: new[]
                {
                    new[] { 1, 2, 3 },
                    [4, 5, 6],
                    [7, 8, 9],
                    [10]
                }
            )
        ],
        [
            new EnumerableTestPlan<int>(
                name: "Batch - with selector",
                source: Enumerable.Range(1, 10),
                invoke: source => source.Batch(2, x => $"Item{x}"),
                expectedResult: new[]
                {
                    new[] { "Item1", "Item2" },
                    ["Item3", "Item4"],
                    ["Item5", "Item6"],
                    ["Item7", "Item8"],
                    ["Item9", "Item10"]
                }
            )
        ],
        [
            new EnumerableTestPlan<int>(
                name: "Batch - selector with index",
                source: Enumerable.Range(1, 10),
                invoke: source => source.Batch(2, (x, i) => $"{i}:{x}"),
                expectedResult: new[]
                {
                    new[] { "0:1", "1:2" },
                    ["2:3", "3:4"],
                    ["4:5", "5:6"],
                    ["6:7", "7:8"],
                    ["8:9", "9:10"]
                }
            )
        ]
    ];

    /// <summary>
    /// Verifies that <see cref="IEnumerableExtensions.Batch{T}(IEnumerable{T}, int)" /> defers execution until the returned sequence is enumerated.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetBatchTestCases))]
    public void Batch_WhenCalled_ShouldDeferExecution(EnumerableTestPlan<int> testCase) => AssertExecutionIsDeferred(testCase.Name, testCase.Invoke, testCase.Source);

    /// <summary>
    /// Verifies that enumerating a batched sequence triggers execution against its source.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetBatchTestCases))]
    public void Batch_WhenEnumerated_ShouldTriggerExecution(EnumerableTestPlan<int> testCase) => AssertExecutionOccursOnEnumeration(testCase.Name, testCase.Invoke, testCase.Source);

    /// <summary>
    /// Verifies that enumerating a batched sequence yields the expected groups for even/uneven splits and for projection overloads (selector and indexed selector).
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetBatchTestCases))]
    public void Batch_WhenEnumerated_ShouldReturnExpectedResults(EnumerableTestPlan<int> testCase) => AssertExecutionReturnsExpectedResults(testCase.Name, testCase.Invoke, testCase.Source, testCase.ExpectedResult, testCase.ResultSelector);

    /// <summary>
    /// Verifies that a <see langword="null" /> source throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Batch_WhenSourceIsNull_ShouldThrowExactly()
    {
        IEnumerable<int>? source = null!;
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.Batch(2).ToList();
        });
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> projection selector throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Batch_WithSelector_WhenSelectorIsNull_ShouldThrowExactly()
    {
        var source = new[] { 1, 2, 3 };
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.Batch(2, selector: (Func<int, int>)null!).ToList();
        });
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> indexed projection selector throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Batch_WithIndexSelector_WhenSelectorIsNull_ShouldThrowExactly()
    {
        var source = new[] { 1, 2, 3 };
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.Batch(2, selector: (Func<int, int, int>)null!).ToList();
        });
    }

    /// <summary>
    /// Verifies that a non-positive batch size throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(0)]
    public void Batch_WhenSizeIsInvalid_ShouldThrowExactly(int size)
    {
        var source = new[] { 1, 2, 3 };
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = source.Batch(size).ToList();
        });
    }

    /// <summary>
    /// Verifies that batching an empty source produces an empty sequence with no batches.
    /// </summary>
    [TestMethod]
    public void Batch_WithEmptySource_ShouldReturnEmpty()
    {
        var actual = Array.Empty<int>().Batch(2).ToList();
        Assert.IsEmpty(actual);
    }
}
