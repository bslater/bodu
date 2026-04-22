// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PooledBufferBuilderTests.AsArray.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

﻿namespace Bodu.Buffers
{
    public partial class PooledBufferBuilderTests
    {
        /// <summary>
        /// Verifies that <see cref="PooledBufferBuilder.AsArray" />, when AccessedAfterAppend, UsingArray, returns the expected value.
        /// </summary>
        [TestMethod]
        public void AsArray_WhenAccessedAfterAppend_ShouldMatchAsSpanContents_UsingArray()
        {
            var source = new[] { 10, 20, 30 };
            using var builder = new PooledBufferBuilder<int>();

            builder.AppendRange(source);
            var span = builder.AsSpan();
            var array = builder.AsArray();

            CollectionAssert.AreEqual(span.ToArray(), array.Take(span.Length).ToArray());
        }
    }
}