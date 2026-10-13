// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RingBackedCollectionTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public sealed partial class RingBackedCollectionTests
{
    /// <summary>
    /// Verifies that the capacity constructor rejects a capacity one above <see cref="Array.MaxLength" /> with
    /// <see cref="ArgumentOutOfRangeException" /> naming <c>capacity</c>.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCapacityExceedsArrayMaxLength_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new TestRing(Array.MaxLength + 1);
        });

        Assert.AreEqual("capacity", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the collection constructor rejects a capacity one above <see cref="Array.MaxLength" /> with
    /// <see cref="ArgumentOutOfRangeException" /> naming <c>capacity</c>.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCollectionProvidedAndCapacityExceedsArrayMaxLength_ShouldThrowArgumentOutOfRangeException()
    {
        int[] source = [1, 2];

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new TestRing(source, Array.MaxLength + 1);
        });

        Assert.AreEqual("capacity", ex.ParamName);
    }
}
