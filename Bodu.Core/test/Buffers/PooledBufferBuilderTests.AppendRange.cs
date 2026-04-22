// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PooledBufferBuilderTests.AppendRange.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

﻿namespace Bodu.Buffers
{
    public partial class PooledBufferBuilderTests
    {
        /// <summary>
        /// Verifies that <see cref="PooledBufferBuilder.AppendRange" />, when EnumerableUsed, UsingIEnumerable, returns the expected value.
        /// </summary>
        [TestMethod]
        public void AppendRange_WhenEnumerableUsed_ShouldAppendAllItems_UsingIEnumerable()
        {
            var source = Enumerable.Range(1, 50);
            using var builder = new PooledBufferBuilder<int>();

            builder.AppendRange(source);

            CollectionAssert.AreEqual(source.ToArray(), builder.AsSpan().ToArray());
        }

        /// <summary>
        /// Verifies that <see cref="PooledBufferBuilder.AppendRange" />, when ExceedsInitialSize, UsingIEnumerable, returns the expected value.
        /// </summary>
        [TestMethod]
        public void AppendRange_WhenExceedsInitialSize_ShouldExpandBuffer_UsingIEnumerable()
        {
            var source = Enumerable.Range(1, 1000);
            using var builder = new PooledBufferBuilder<int>();

            builder.AppendRange(source);

            Assert.AreEqual(1000, builder.Count);
        }
    }
}