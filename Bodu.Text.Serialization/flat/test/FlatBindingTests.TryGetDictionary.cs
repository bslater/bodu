// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FlatBindingTests.TryGetDictionary.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Dynamic;

namespace Bodu.Text.Serialization;

public sealed partial class FlatBindingTests
{
    /// <summary>
    /// Verifies that a value that implements the non-generic <see cref="IDictionary" /> is a dictionary whose entries
    /// are the value itself.
    /// </summary>
    [TestMethod]
    public void TryGetDictionary_WhenTheValueImplementsTheNonGenericInterface_ShouldReturnTheValueItself()
    {
        var value = new Dictionary<string, int> { ["b"] = 2, ["a"] = 1 };

        bool found = FlatBinding.TryGetDictionary(value, out IDictionary? dictionary);

        Assert.IsTrue(found);
        Assert.AreSame(value, dictionary);
    }

    /// <summary>
    /// Verifies that a value that implements only <see cref="IDictionary{TKey, TValue}" /> keyed by
    /// <see cref="string" /> is a dictionary whose entries are copied through that interface, in the order it
    /// enumerates them.
    /// </summary>
    [TestMethod]
    public void TryGetDictionary_WhenTheValueImplementsOnlyTheGenericInterface_ShouldCopyItsEntriesInOrder()
    {
        var value = new ExpandoObject();
        IDictionary<string, object?> entries = value;
        entries["b"] = 2;
        entries["a"] = "one";

        bool found = FlatBinding.TryGetDictionary(value, out IDictionary? dictionary);

        Assert.IsTrue(found);
        Assert.IsNotNull(dictionary);
        CollectionAssert.AreEqual(new object[] { "b", "a" }, dictionary.Keys.Cast<object>().ToArray());
        CollectionAssert.AreEqual(new object[] { 2, "one" }, dictionary.Values.Cast<object>().ToArray());
    }

    /// <summary>
    /// Verifies that <see langword="null" />, a scalar, a string and an enumerable that is not a dictionary are not
    /// dictionaries.
    /// </summary>
    [TestMethod]
    public void TryGetDictionary_WhenTheValueIsNotADictionary_ShouldReturnFalse()
    {
        object?[] values = [null, 42, "text", new List<string> { "a" }, new SortedSet<int> { 1 }];

        foreach (object? value in values)
        {
            bool found = FlatBinding.TryGetDictionary(value, out IDictionary? dictionary);

            Assert.IsFalse(found, value?.GetType().Name ?? "null");
            Assert.IsNull(dictionary, value?.GetType().Name ?? "null");
        }
    }
}
