// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CircularBufferDebugViewTests.Ctor.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

﻿namespace Bodu.Collections.Generic
{
    public partial class CircularBufferDebugViewTests
    {
        /// <summary>
        /// Verifies that constructing a DebugView with a valid buffer instance succeeds.
        /// </summary>
        [TestMethod]
        public void Ctor_WithValidBuffer_ShouldInitialize()
        {
            var buffer = new CircularBuffer<int>(3);
            var view = new CircularBufferDebugView<int>(buffer);

            Assert.IsNotNull(view);
        }

        /// <summary>
        /// Verifies that constructing a DebugView with a null buffer throws ArgumentNullException.
        /// </summary>
        [TestMethod]
        public void Ctor_WithNullBuffer_ShouldThrowExactly()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                _ = new CircularBufferDebugView<int>(null!);
            });
        }
    }
}