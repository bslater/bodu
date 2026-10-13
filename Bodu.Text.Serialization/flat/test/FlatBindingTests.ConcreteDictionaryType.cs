// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FlatBindingTests.ConcreteDictionaryType.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization;

public sealed partial class FlatBindingTests
{
    /// <summary>
    /// Verifies that a dictionary interface is created as a <see cref="Dictionary{TKey, TValue}" />, and a concrete
    /// dictionary type as itself.
    /// </summary>
    [TestMethod]
    public void ConcreteDictionaryType_WhenTheTypeIsAnInterface_ShouldReturnADictionary()
    {
        Assert.AreEqual(typeof(Dictionary<string, int>), FlatBinding.ConcreteDictionaryType(typeof(IDictionary<string, int>), typeof(int)));
        Assert.AreEqual(typeof(SortedList<string, int>), FlatBinding.ConcreteDictionaryType(typeof(SortedList<string, int>), typeof(int)));
    }
}
