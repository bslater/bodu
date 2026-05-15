// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiValueDictionaryTests.Ctors.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Collections.Generic;

public partial class MultiValueDictionaryTests
{

    /// <summary>
    /// Verifies that passing a null comparer to the comparer constructor defaults to the default equality comparer.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenComparerIsNull_ShouldUseDefaultComparer()
    {
        var mvd =
            new MultiValueDictionary<string, int>((IEqualityComparer<string>?)null);

        Assert.AreEqual(EqualityComparer<string>.Default, mvd.Comparer);
    }

    /// <summary>
    /// Verifies that a custom comparer is used for key equality.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenComparerIsProvided_ShouldUseSpecifiedComparer()
    {
        var mvd =
            new MultiValueDictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        mvd.Add("KEY", 1);
        mvd.Add("key", 2);

        Assert.AreEqual(1, mvd.KeyCount);
        Assert.AreEqual(2, mvd.Count);
    }
    /// <summary>
    /// Verifies that the default constructor produces an empty dictionary with zero counts.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDefault_ShouldBeEmpty()
    {
        var mvd = new MultiValueDictionary<string, int>();

        Assert.AreEqual(0, mvd.Count);
        Assert.AreEqual(0, mvd.KeyCount);
    }

    /// <summary>
    /// Verifies that the default constructor uses the default equality comparer for keys.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDefault_ShouldUseDefaultComparer()
    {
        var mvd = new MultiValueDictionary<string, int>();

        Assert.AreEqual(EqualityComparer<string>.Default, mvd.Comparer);
    }

}
