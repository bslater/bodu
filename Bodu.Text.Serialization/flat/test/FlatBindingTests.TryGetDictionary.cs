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

    /// <summary>
    /// Verifies that a dictionary whose type is not keyed by <see cref="string" />, whether it implements only the
    /// non-generic <see cref="IDictionary" />, as <see cref="Hashtable" /> and <see cref="SortedList" /> do, or a
    /// generic dictionary with other keys, is not a dictionary the serializers write as entries, even when its keys
    /// are strings.
    /// </summary>
    [TestMethod]
    public void TryGetDictionary_WhenTheValueIsADictionaryWhoseKeysAreNotStrings_ShouldReturnFalse()
    {
        object[] values =
        [
            new Hashtable { ["a"] = 1 },
            new SortedList { ["a"] = 1 },
            new Dictionary<int, string> { [1] = "a" },
            new Dictionary<object, string> { ["a"] = "a" },
        ];

        foreach (object value in values)
        {
            bool found = FlatBinding.TryGetDictionary(value, out IDictionary? dictionary);

            Assert.IsFalse(found, value.GetType().Name);
            Assert.IsNull(dictionary, value.GetType().Name);
        }
    }

    /// <summary>
    /// Verifies that the overload that reports the value type gives the value type of the string-keyed dictionary the
    /// value implements, through the non-generic interface and through the generic one alike.
    /// </summary>
    [TestMethod]
    public void TryGetDictionary_WhenTheValueIsAStringKeyedDictionary_ShouldReportItsValueType()
    {
        var dictionary = new Dictionary<string, int> { ["a"] = 1 };
        var expando = new ExpandoObject();

        bool foundDictionary = FlatBinding.TryGetDictionary(dictionary, out IDictionary? entries, out Type? valueType);
        bool foundExpando = FlatBinding.TryGetDictionary(expando, out IDictionary? expandoEntries, out Type? expandoValueType);

        Assert.IsTrue(foundDictionary);
        Assert.AreSame(dictionary, entries);
        Assert.AreEqual(typeof(int), valueType);
        Assert.IsTrue(foundExpando);
        Assert.IsNotNull(expandoEntries);
        Assert.AreEqual(typeof(object), expandoValueType);
    }

    /// <summary>
    /// Verifies that the overload that reports the value type reports none for a value that is not a string-keyed
    /// dictionary.
    /// </summary>
    [TestMethod]
    public void TryGetDictionary_WhenTheValueIsNotAStringKeyedDictionary_ShouldReportNoValueType()
    {
        bool found = FlatBinding.TryGetDictionary(new Hashtable { ["a"] = 1 }, out IDictionary? entries, out Type? valueType);

        Assert.IsFalse(found);
        Assert.IsNull(entries);
        Assert.IsNull(valueType);
    }
}
