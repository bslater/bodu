// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequencedDictionaryTests.IsReadOnly.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic;

public partial class SequencedDictionaryTests
{
    /// <summary>
    /// Verifies that the dictionary reports itself as not read-only.
    /// </summary>
    [TestMethod]
    public void IsReadOnly_WhenQueried_ShouldBeFalse()
    {
        var dictionary = new SequencedDictionary<string, int>();

        Assert.IsFalse(dictionary.IsReadOnly);
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="IDictionary" /> surface reports the dictionary as neither read-only nor fixed-size.
    /// </summary>
    [TestMethod]
    public void IDictionary_WhenQueried_ShouldBeMutableAndNotFixedSize()
    {
        IDictionary dictionary = new SequencedDictionary<string, int>();

        Assert.IsFalse(dictionary.IsReadOnly);
        Assert.IsFalse(dictionary.IsFixedSize);
    }
}
