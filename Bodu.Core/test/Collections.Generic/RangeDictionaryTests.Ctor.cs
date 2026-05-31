// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RangeDictionaryTests.Ctor.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class RangeDictionaryTests
{

    // --------------------------------------------------------
    // Comparer-only constructor
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that a <see langword="null" /> comparer is replaced with <see cref="Comparer{T}.Default" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenComparerIsNull_ShouldUseDefaultComparer()
    {
        var sut = new RangeDictionary<int, string>(null);

        Assert.AreSame(Comparer<int>.Default, sut.Comparer);
    }

    /// <summary>
    /// Verifies that a non-<see langword="null" /> comparer is stored verbatim.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenComparerIsProvided_ShouldUseSpecifiedComparer()
    {
        IComparer<int> comparer = Comparer<int>.Default;

        var sut = new RangeDictionary<int, string>(comparer);

        Assert.AreSame(comparer, sut.Comparer);
    }
    // --------------------------------------------------------
    // Default constructor
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that the default constructor produces an empty dictionary using the default comparer.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDefault_ShouldBeEmptyWithDefaultComparer()
    {
        var sut = new RangeDictionary<int, string>();

        Assert.AreEqual(0, sut.Count);
        Assert.AreEqual(0, sut.Capacity);
        Assert.AreSame(Comparer<int>.Default, sut.Comparer);
    }

}
