// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiValueDictionaryTests.TypeCoverage.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Collections.Generic;

public partial class MultiValueDictionaryTests
{
    // --------------------------------------------------------
    // Private helper types
    // --------------------------------------------------------

    /// <summary>Value-type key with structural (value-based) equality via record struct.</summary>
    private readonly record struct Coord(int Row, int Col);

    /// <summary>Reference-type value with no overridden equality — uses reference identity.</summary>
    private sealed class Label
    {
        public string Text { get; }

        public Label(string text) { Text = text; }
    }

    // --------------------------------------------------------
    // Value-type struct as TKey
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that a struct key with the same field values is treated as the same key regardless of
    /// which instance is used, confirming value-type equality semantics for dictionary lookup.
    /// </summary>
    [TestMethod]
    public void MultiValueDictionary_WhenKeyIsValueTypeStruct_ShouldMatchByValue()
    {
        MultiValueDictionary<Coord, string> sut = new MultiValueDictionary<Coord, string>();
        Coord key = new Coord(1, 2);

        sut.Add(key, "alpha");
        sut.Add(new Coord(1, 2), "beta");

        Assert.AreEqual(1, sut.KeyCount);
        Assert.AreEqual(2, sut.Count);
        Assert.AreEqual(2, sut[new Coord(1, 2)].Count);
    }

    /// <summary>
    /// Verifies that two struct keys with different field values are treated as distinct keys.
    /// </summary>
    [TestMethod]
    public void MultiValueDictionary_WhenKeyIsValueTypeStructWithDifferentFields_ShouldStoreSeparately()
    {
        MultiValueDictionary<Coord, string> sut = new MultiValueDictionary<Coord, string>();

        sut.Add(new Coord(1, 2), "a");
        sut.Add(new Coord(3, 4), "b");

        Assert.AreEqual(2, sut.KeyCount);
        Assert.AreEqual(2, sut.Count);
        Assert.AreEqual("a", sut[new Coord(1, 2)][0]);
        Assert.AreEqual("b", sut[new Coord(3, 4)][0]);
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.ContainsKey"/> matches a struct key by
    /// value, not by instance identity.
    /// </summary>
    [TestMethod]
    public void ContainsKey_WhenKeyIsValueTypeStruct_ShouldMatchByValue()
    {
        MultiValueDictionary<Coord, string> sut = new MultiValueDictionary<Coord, string>();
        sut.Add(new Coord(7, 8), "x");

        Assert.IsTrue(sut.ContainsKey(new Coord(7, 8)));
        Assert.IsFalse(sut.ContainsKey(new Coord(7, 9)));
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.RemoveAll"/> removes the correct entry
    /// when the key is a value-type struct, located by value equality.
    /// </summary>
    [TestMethod]
    public void RemoveAll_WhenKeyIsValueTypeStruct_ShouldRemoveByValue()
    {
        MultiValueDictionary<Coord, string> sut = new MultiValueDictionary<Coord, string>();
        sut.Add(new Coord(1, 2), "a");
        sut.Add(new Coord(3, 4), "b");

        var removed = sut.RemoveAll(new Coord(1, 2));

        Assert.IsTrue(removed);
        Assert.AreEqual(1, sut.KeyCount);
        Assert.IsFalse(sut.ContainsKey(new Coord(1, 2)));
        Assert.IsTrue(sut.ContainsKey(new Coord(3, 4)));
    }

    // --------------------------------------------------------
    // Value-type struct as TValue
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that struct values are stored and retrieved by value, and that insertion order is preserved.
    /// </summary>
    [TestMethod]
    public void MultiValueDictionary_WhenValueIsValueTypeStruct_ShouldStoreAndRetrieveByValue()
    {
        MultiValueDictionary<string, Coord> sut = new MultiValueDictionary<string, Coord>();
        sut.Add("grid", new Coord(1, 2));
        sut.Add("grid", new Coord(3, 4));

        IReadOnlyList<Coord> values = sut["grid"];

        Assert.AreEqual(2, values.Count);
        Assert.AreEqual(new Coord(1, 2), values[0]);
        Assert.AreEqual(new Coord(3, 4), values[1]);
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.ContainsValue"/> locates a struct value
    /// by value equality, not by reference.
    /// </summary>
    [TestMethod]
    public void ContainsValue_WhenValueIsValueTypeStruct_ShouldMatchByValue()
    {
        MultiValueDictionary<string, Coord> sut = new MultiValueDictionary<string, Coord>();
        sut.Add("k", new Coord(5, 6));

        Assert.IsTrue(sut.ContainsValue("k", new Coord(5, 6)));
        Assert.IsFalse(sut.ContainsValue("k", new Coord(5, 7)));
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Remove(TKey,TValue)"/> removes a struct
    /// value by value equality.
    /// </summary>
    [TestMethod]
    public void Remove_WhenValueIsValueTypeStruct_ShouldRemoveByValue()
    {
        MultiValueDictionary<string, Coord> sut = new MultiValueDictionary<string, Coord>();
        sut.Add("k", new Coord(1, 2));
        sut.Add("k", new Coord(3, 4));

        var removed = sut.Remove("k", new Coord(1, 2));

        Assert.IsTrue(removed);
        Assert.AreEqual(1, sut.Count);
        Assert.IsFalse(sut.ContainsValue("k", new Coord(1, 2)));
        Assert.IsTrue(sut.ContainsValue("k", new Coord(3, 4)));
    }

    // --------------------------------------------------------
    // Reference-type class as TValue (reference identity)
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that two distinct <see cref="Label"/> instances with the same text are stored as separate
    /// values, because <see cref="Label"/> uses reference identity.
    /// </summary>
    [TestMethod]
    public void MultiValueDictionary_WhenValueIsReferenceType_ShouldStoreBothInstances()
    {
        Label l1 = new Label("hello");
        Label l2 = new Label("hello");

        MultiValueDictionary<string, Label> sut = new MultiValueDictionary<string, Label>();
        sut.Add("k", l1);
        sut.Add("k", l2);

        Assert.AreEqual(2, sut.Count);
        Assert.AreEqual(2, sut["k"].Count);
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.ContainsValue"/> uses reference equality
    /// for reference-type values with no overridden <see cref="object.Equals(object)"/>.
    /// </summary>
    [TestMethod]
    public void ContainsValue_WhenValueIsReferenceTypeAndSameInstance_ShouldReturnTrue()
    {
        Label l1 = new Label("hello");
        Label l2 = new Label("hello");

        MultiValueDictionary<string, Label> sut = new MultiValueDictionary<string, Label>();
        sut.Add("k", l1);

        Assert.IsTrue(sut.ContainsValue("k", l1));
        Assert.IsFalse(sut.ContainsValue("k", l2));
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Remove(TKey,TValue)"/> removes only the
    /// specific <see cref="Label"/> instance that was requested, leaving other instances untouched.
    /// </summary>
    [TestMethod]
    public void Remove_WhenValueIsReferenceType_ShouldRemoveOnlyRequestedInstance()
    {
        Label l1 = new Label("hello");
        Label l2 = new Label("hello");

        MultiValueDictionary<string, Label> sut = new MultiValueDictionary<string, Label>();
        sut.Add("k", l1);
        sut.Add("k", l2);

        var removed = sut.Remove("k", l1);

        Assert.IsTrue(removed);
        Assert.AreEqual(1, sut.Count);
        Assert.IsTrue(sut.ContainsValue("k", l2));
        Assert.IsFalse(sut.ContainsValue("k", l1));
    }

    // --------------------------------------------------------
    // Custom comparer for TKey
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that a custom key comparer causes case-insensitively equal string keys to be merged into a
    /// single entry, accumulating their values together.
    /// </summary>
    [TestMethod]
    public void MultiValueDictionary_WhenCustomComparerUsed_ShouldMergeEquivalentKeys()
    {
        MultiValueDictionary<string, int> sut =
            new MultiValueDictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);

        sut.Add("Alpha", 1);
        sut.Add("ALPHA", 2);
        sut.Add("alpha", 3);

        Assert.AreEqual(1, sut.KeyCount);
        Assert.AreEqual(3, sut.Count);
        Assert.AreEqual(3, sut["Alpha"].Count);
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.ContainsKey"/> honours the custom key
    /// comparer, returning <see langword="true"/> for a key that differs only in case.
    /// </summary>
    [TestMethod]
    public void ContainsKey_WhenCustomComparerUsed_ShouldMatchCaseInsensitively()
    {
        MultiValueDictionary<string, int> sut =
            new MultiValueDictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);
        sut.Add("FOO", 1);

        Assert.IsTrue(sut.ContainsKey("foo"));
        Assert.IsTrue(sut.ContainsKey("FOO"));
        Assert.IsTrue(sut.ContainsKey("Foo"));
    }
}
