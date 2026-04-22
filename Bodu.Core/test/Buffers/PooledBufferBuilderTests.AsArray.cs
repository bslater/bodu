// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PooledBufferBuilderTests.AsArray.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

﻿namespace Bodu.Buffers
{
    public partial class PooledBufferBuilderTests
    {
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