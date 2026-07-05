// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalTreeTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class IntervalTreeTests
{
    /// <summary>
    /// Verifies that the parameterless constructor creates an empty tree using the default comparer.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenParameterless_ShouldCreateEmptyTreeWithDefaultComparer()
    {
        var sut = new IntervalTree<int>();

        Assert.AreEqual(0, sut.Count);
        Assert.AreSame(Comparer<int>.Default, sut.Comparer);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> comparer argument falls back to the default comparer.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenComparerIsNull_ShouldUseDefaultComparer()
    {
        var sut = new IntervalTree<int>(null);

        Assert.AreSame(Comparer<int>.Default, sut.Comparer);
    }

    /// <summary>
    /// Verifies that an explicitly supplied comparer is retained and exposed via
    /// <see cref="IntervalTree{T}.Comparer" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenComparerSupplied_ShouldExposeSuppliedComparer()
    {
        IComparer<string> comparer = StringComparer.OrdinalIgnoreCase;

        var sut = new IntervalTree<string>(comparer);

        Assert.AreSame(comparer, sut.Comparer);
    }
}
