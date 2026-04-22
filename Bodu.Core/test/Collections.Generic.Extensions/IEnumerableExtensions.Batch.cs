// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.Batch.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

﻿namespace Bodu.Collections.Generic.Extensions
{
    [TestClass]
    public sealed partial class IEnumerableExtensionsTests_Batch : EnumerableTests
    {
        public static IEnumerable<object[]> GetBatchTestCases() => new[]
        {
            new object[]
            {
                new EnumerableTestPlan<int>(
                    name: "Batch - even split",
                    source: Enumerable.Range(1, 10),
                    invoke: source => source.Batch(2),
                    expectedResult: new[]
                    {
                        new[] { 1, 2 },
                        new[] { 3, 4 },
                        new[] { 5, 6 },
                        new[] { 7, 8 },
                        new[] { 9, 10 }
                    }
                )
            },
            new object[]
            {
                new EnumerableTestPlan<int>(
                    name: "Batch - uneven split",
                    source: Enumerable.Range(1, 10),
                    invoke: source => source.Batch(3),
                    expectedResult: new[]
                    {
                        new[] { 1, 2, 3 },
                        new[] { 4, 5, 6 },
                        new[] { 7, 8, 9 },
                        new[] { 10 }
                    }
                )
            },
            new object[]
            {
                new EnumerableTestPlan<int>(
                    name: "Batch - with selector",
                    source: Enumerable.Range(1, 10),
                    invoke: source => source.Batch(2, x => $"Item{x}"),
                    expectedResult: new[]
                    {
                        new[] { "Item1", "Item2" },
                        new[] { "Item3", "Item4" },
                        new[] { "Item5", "Item6" },
                        new[] { "Item7", "Item8" },
                        new[] { "Item9", "Item10" }
                    }
                )
            },
            new object[]
            {
                new EnumerableTestPlan<int>(
                    name: "Batch - selector with index",
                    source: Enumerable.Range(1, 10),
                    invoke: source => source.Batch(2, (x, i) => $"{i}:{x}"),
                    expectedResult: new[]
                    {
                        new[] { "0:1", "1:2" },
                        new[] { "2:3", "3:4" },
                        new[] { "4:5", "5:6" },
                        new[] { "6:7", "7:8" },
                        new[] { "8:9", "9:10" }
                    }
                )
            }
        };

        /// <summary>
        /// Verifies that Batch, when Called, Defer Execution.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(GetBatchTestCases), DynamicDataSourceType.Method)]
        public void Batch_WhenCalled_ShouldDeferExecution(EnumerableTestPlan<int> testCase)
        {
            AssertExecutionIsDeferred(testCase.Name, testCase.Invoke, testCase.Source);
        }

        /// <summary>
        /// Verifies that Batch, when Enumerated, Trigger Execution.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(GetBatchTestCases), DynamicDataSourceType.Method)]
        public void Batch_WhenEnumerated_ShouldTriggerExecution(EnumerableTestPlan<int> testCase)
        {
            AssertExecutionOccursOnEnumeration(testCase.Name, testCase.Invoke, testCase.Source);
        }

        /// <summary>
        /// Verifies that Batch, when Enumerated, returns Expected Results.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(GetBatchTestCases), DynamicDataSourceType.Method)]
        public void Batch_WhenEnumerated_ShouldReturnExpectedResults(EnumerableTestPlan<int> testCase)
        {
            AssertExecutionReturnsExpectedResults(testCase.Name, testCase.Invoke, testCase.Source, testCase.ExpectedResult, testCase.ResultSelector);
        }

        /// <summary>
        /// Verifies that Batch, when Source Is Null, throws Exactly.
        /// </summary>
        [TestMethod]
        public void Batch_WhenSourceIsNull_ShouldThrowExactly()
        {
            IEnumerable<int>? source = null!;
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                source.Batch(2).ToList();
            });
        }

        /// <summary>
        /// Verifies that Batch, with Selector, when Selector Is Null, throws Exactly.
        /// </summary>
        [TestMethod]
        public void Batch_WithSelector_WhenSelectorIsNull_ShouldThrowExactly()
        {
            var source = new[] { 1, 2, 3 };
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                source.Batch(2, selector: (Func<int, int>)null!).ToList();
            });
        }

        /// <summary>
        /// Verifies that Batch, with Index Selector, when Selector Is Null, throws Exactly.
        /// </summary>
        [TestMethod]
        public void Batch_WithIndexSelector_WhenSelectorIsNull_ShouldThrowExactly()
        {
            var source = new[] { 1, 2, 3 };
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                source.Batch(2, selector: (Func<int, int, int>)null!).ToList();
            });
        }

        /// <summary>
        /// Verifies that Batch, when Size Is Invalid, throws Exactly.
        /// </summary>
        [TestMethod]
        [DataRow(-1)]
        [DataRow(0)]
        public void Batch_WhenSizeIsInvalid_ShouldThrowExactly(int size)
        {
            var source = new[] { 1, 2, 3 };
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                source.Batch(size).ToList();
            });
        }

        /// <summary>
        /// Verifies that Batch, with Empty Source, returns Empty.
        /// </summary>
        [TestMethod]
        public void Batch_WithEmptySource_ShouldReturnEmpty()
        {
            var actual = Array.Empty<int>().Batch(2).ToList();
            Assert.AreEqual(0, actual.Count);
        }
    }
}