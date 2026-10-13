// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FlatBindingTests.SetEntry.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Dynamic;

namespace Bodu.Text.Serialization;

public sealed partial class FlatBindingTests
{
    /// <summary>
    /// Verifies that an entry is stored in a dictionary that implements the non-generic
    /// <see cref="System.Collections.IDictionary" />, and replaces the value of a key already present.
    /// </summary>
    [TestMethod]
    public void SetEntry_WhenTheDictionaryImplementsTheNonGenericInterface_ShouldStoreTheEntry()
    {
        var dictionary = new Dictionary<string, int> { ["a"] = 0 };

        FlatBinding.SetEntry(dictionary, typeof(int), "a", 1);
        FlatBinding.SetEntry(dictionary, typeof(int), "b", 2);

        CollectionAssert.AreEqual(new[] { "a", "b" }, dictionary.Keys.ToArray());
        Assert.AreEqual(1, dictionary["a"]);
        Assert.AreEqual(2, dictionary["b"]);
    }

    /// <summary>
    /// Verifies that an entry is stored through <see cref="IDictionary{TKey, TValue}" /> in a dictionary that
    /// implements only that interface, and replaces the value of a key already present.
    /// </summary>
    [TestMethod]
    public void SetEntry_WhenTheDictionaryImplementsOnlyTheGenericInterface_ShouldStoreTheEntryThroughIt()
    {
        var value = new ExpandoObject();
        IDictionary<string, object?> dictionary = value;
        dictionary["a"] = 0;

        FlatBinding.SetEntry(value, typeof(object), "a", 1);
        FlatBinding.SetEntry(value, typeof(object), "b", "two");

        CollectionAssert.AreEqual(new[] { "a", "b" }, dictionary.Keys.ToArray());
        Assert.AreEqual(1, dictionary["a"]);
        Assert.AreEqual("two", dictionary["b"]);
    }
}
