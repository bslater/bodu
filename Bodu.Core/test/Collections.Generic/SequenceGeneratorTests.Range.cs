// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequenceGeneratorTests.Range.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

﻿namespace Bodu.Collections.Generic
{
    public partial class SequenceGeneratorTests
    {/// <summary>
     /// Verifies that Range returns an inclusive ascending or descending integer sequence. </summary>
        /// <summary>
        /// Verifies that <see cref="SequenceGenerator.Range" />, when StartAndStopAreValid, returns the expected value.
        /// </summary>
        [TestMethod]
        [DataRow(1, 5, new[] { 1, 2, 3, 4, 5 })]
        [DataRow(5, 1, new[] { 5, 4, 3, 2, 1 })]
        [DataRow(3, 3, new[] { 3 })]
        public void Range_WhenStartAndStopAreValid_ShouldReturnInclusiveSequence(int start, int stop, int[] expected)
        {
            var actual = SequenceGenerator.Range(start, stop).ToArray();
            CollectionAssert.AreEqual(expected, actual);
        }

        /// <summary>
        /// Verifies that Range with a step generates correctly spaced results.
        /// </summary>
        [TestMethod]
        [DataRow(1, 10, 2, new[] { 1, 3, 5, 7, 9 })]
        [DataRow(10, 1, -3, new[] { 10, 7, 4, 1 })]
        [DataRow(4, 4, 0, new[] { 4, 4, 4, 4 })]
        public void Range_WhenStepIsSpecified_ShouldRespectStepDirection(int start, int stop, int step, int[] expected)
        {
            var actual = SequenceGenerator.Range(start, stop, step).Take(expected.Length).ToArray();
            CollectionAssert.AreEqual(expected, actual);
        }

        /// <summary>
        /// Verifies that Range returns a valid long sequence of the expected count.
        /// </summary>
        [TestMethod]
        public void Range_WhenCountIsPositive_ShouldReturnCorrectLongSequence()
        {
            var actual = SequenceGenerator.Range(1000L, 5).ToArray();
            CollectionAssert.AreEqual(new long[] { 1000, 1001, 1002, 1003, 1004 }, actual);
        }

        /// <summary>
        /// Verifies that Range throws when the count is negative.
        /// </summary>
        [TestMethod]
        public void Range_WhenCountIsNegative_ShouldThrowExactly()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                SequenceGenerator.Range(1L, -1).ToArray();
            });
        }

        /// <summary>
        /// Verifies that Range throws if the resulting long range would overflow.
        /// </summary>
        [TestMethod]
        public void Range_WhenResultWouldOverflowLong_ShouldThrowExactly()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                SequenceGenerator.Range(long.MaxValue - 2, 5).ToArray();
            });
        }

        /// <summary>
        /// Verifies that Range handles extreme Int32 values correctly.
        /// </summary>
        [TestMethod]
        [DataRow(int.MinValue, int.MinValue + 2, new[] { int.MinValue, int.MinValue + 1, int.MinValue + 2 })]
        [DataRow(int.MaxValue - 2, int.MaxValue, new[] { int.MaxValue - 2, int.MaxValue - 1, int.MaxValue })]
        public void Range_WhenAtInt32Boundaries_ShouldReturnExpectedSequence(int start, int stop, int[] expected)
        {
            var actual = SequenceGenerator.Range(start, stop).ToArray();
            CollectionAssert.AreEqual(expected, actual);
        }

        /// <summary>
        /// Verifies that a step of zero repeats the start value indefinitely.
        /// </summary>
        [TestMethod]
        public void Range_WhenStepIsZero_ShouldRepeatStartIndefinitely()
        {
            var actual = SequenceGenerator.Range(7, 100, 0).Take(4).ToArray();
            CollectionAssert.AreEqual(new[] { 7, 7, 7, 7 }, actual);
        }

        /// <summary>
        /// Verifies that an incorrectly directed step yields an empty sequence.
        /// </summary>
        [TestMethod]
        public void Range_WhenStepDirectionIsInvalid_ShouldYieldEmptySequence()
        {
            var actual = SequenceGenerator.Range(0, 10, -1).ToArray();
            Assert.AreEqual(0, actual.Length);
        }

        /// <summary>
        /// Verifies that a count of zero yields an empty long sequence.
        /// </summary>
        [TestMethod]
        public void Range_WhenCountIsZero_ShouldReturnEmptySequence()
        {
            var actual = SequenceGenerator.Range(500L, 0).ToArray();
            Assert.AreEqual(0, actual.Length);
        }

        /// <summary>
        /// Verifies that a range of one value at the long maximum boundary succeeds.
        /// </summary>
        [TestMethod]
        public void Range_WhenStartIsLongMaxAndCountIsOne_ShouldReturnSingleValue()
        {
            var actual = SequenceGenerator.Range(long.MaxValue, 1).ToArray();
            CollectionAssert.AreEqual(new[] { long.MaxValue }, actual);
        }

        /// <summary>
        /// Verifies that Range defers execution when called.
        /// </summary>
        [TestMethod]
        public void Range_WhenCalled_ShouldDeferExecution()
        {
            int start = 1;
            int stop = 10;

            AssertExecutionIsDeferred("Range", _ =>
                SequenceGenerator.Range(start, stop), new[] { start, stop });
        }

        /// <summary>
        /// Verifies that Range with step defers execution until enumerated.
        /// </summary>
        [TestMethod]
        public void Range_WhenStepIsSpecified_ShouldDeferExecution()
        {
            int start = 1;
            int stop = 10;

            AssertExecutionIsDeferred("Range", _ =>
                SequenceGenerator.Range(start, stop, 1), new[] { start, stop });
        }
    }
}