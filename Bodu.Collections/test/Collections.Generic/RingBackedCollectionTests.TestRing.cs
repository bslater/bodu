// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RingBackedCollectionTests.TestRing.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public sealed partial class RingBackedCollectionTests
{
    /// <summary>
    /// A minimal <see cref="RingBackedCollection{T}" /> that exposes the protected constructors to the tests.
    /// </summary>
    private sealed class TestRing
        : RingBackedCollection<int>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestRing" /> class with the specified capacity.
        /// </summary>
        /// <param name="capacity">The backing-array capacity passed to the base constructor.</param>
        public TestRing(int capacity)
            : base(capacity)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestRing" /> class from a collection and capacity.
        /// </summary>
        /// <param name="collection">The source elements passed to the base constructor.</param>
        /// <param name="capacity">The backing-array capacity passed to the base constructor.</param>
        public TestRing(IEnumerable<int> collection, int capacity)
            : base(collection, capacity)
        {
        }
    }
}
