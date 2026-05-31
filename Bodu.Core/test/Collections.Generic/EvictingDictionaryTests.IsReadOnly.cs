// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EvictingDictionaryTests.IsReadOnly.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class EvictingDictionaryTests
{

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.IsReadOnly" /> returns false when accessed through the ICollection interface.
    /// </summary>
    [TestMethod]
    public void IsReadOnly_ShouldReturnFalse()
    {
        var dictionary = new EvictingDictionary<string, int>(3);
        Assert.IsFalse(dictionary.IsReadOnly);
    }

}
