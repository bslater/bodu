// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PooledBufferBuilderTests.Dispose.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

﻿namespace Bodu.Buffers
{
    public partial class PooledBufferBuilderTests
    {
        /// <summary>
        /// Verifies that Dispose, when Called, Using Pooled Buffer Builder, Prevent Buffer Access.
        /// </summary>
        [TestMethod]
        public void Dispose_WhenCalled_ShouldPreventBufferAccess_UsingPooledBufferBuilder()
        {
            var builder = new PooledBufferBuilder<int>();
            builder.AppendRange(Enumerable.Range(1, 5));
            builder.Dispose();

            Assert.ThrowsExactly<ObjectDisposedException>(() => builder.AsArray());
            Assert.ThrowsExactly<ObjectDisposedException>(() => builder.AsSpan());
            Assert.ThrowsExactly<ObjectDisposedException>(() => _ = builder.Count);
        }

        /// <summary>
        /// Verifies that Dispose, when Invoked Multiple Times, Using Pooled Buffer Builder, does not Throw.
        /// </summary>
        [TestMethod]
        public void Dispose_WhenInvokedMultipleTimes_ShouldNotThrow_UsingPooledBufferBuilder()
        {
            var builder = new PooledBufferBuilder<int>();
            builder.AppendRange(new[] { 1, 2, 3 });

            builder.Dispose();
            builder.Dispose(); // no exception expected
        }
    }
}