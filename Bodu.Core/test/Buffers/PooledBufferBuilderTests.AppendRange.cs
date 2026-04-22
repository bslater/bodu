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
        /// Verifies that Append Range, when Enumerable Used, Using I Enumerable, Append All Items.
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
        /// Verifies that Append Range, when Exceeds Initial Size, Using I Enumerable, Expand Buffer.
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