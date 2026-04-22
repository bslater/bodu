// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PooledBufferBuilderTests.Count.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

﻿namespace Bodu.Buffers
{
    public partial class PooledBufferBuilderTests
    {
        /// <summary>
        /// Verifies that Count, when Items Added, Using Span, returns Accurate Value.
        /// </summary>
        [TestMethod]
        public void Count_WhenItemsAdded_ShouldReturnAccurateValue_UsingSpan()
        {
            using var builder = new PooledBufferBuilder<string>();
            builder.AppendRange(new[] { "a", "b", "c" });

            Assert.AreEqual(3, builder.Count);
        }
    }
}