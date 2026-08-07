// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerTests.UntypedDictionary.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Collections.Specialized;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies binding into a non-generic <see cref="IDictionary" />, which takes a different converter from the
/// generic dictionary path because the value type is not known statically.
/// </summary>
public partial class YamlSerializerTests
{
    /// <summary>
    /// Verifies that a mapping binds into a concrete non-generic dictionary, with values bound loosely.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenTargetIsUntypedDictionary_ShouldBindEveryEntry()
    {
        var dictionary = YamlSerializer.Deserialize<Hashtable>("name: Ada\ncount: 3\n")!;

        Assert.AreEqual(2, dictionary.Count);
        Assert.AreEqual("Ada", dictionary["name"]);
        Assert.IsNotNull(dictionary["count"]);
    }

    /// <summary>
    /// Verifies that an ordered non-generic dictionary is also supported, confirming the converter instantiates the
    /// requested type rather than a fixed one.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenTargetIsOrderedDictionary_ShouldBindEveryEntry()
    {
        var dictionary = YamlSerializer.Deserialize<OrderedDictionary>("first: 1\nsecond: 2\n")!;

        Assert.AreEqual(2, dictionary.Count);
        Assert.IsTrue(dictionary.Contains("first"));
        Assert.IsTrue(dictionary.Contains("second"));
    }

    /// <summary>
    /// Verifies that an empty mapping binds to an empty dictionary rather than to <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenUntypedDictionaryMappingIsEmpty_ShouldBindEmpty()
    {
        var dictionary = YamlSerializer.Deserialize<Hashtable>("{}")!;

        Assert.AreEqual(0, dictionary.Count);
    }

    /// <summary>
    /// Verifies that nested mappings and sequences bind loosely inside an untyped dictionary.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenUntypedDictionaryHasNestedNodes_ShouldBindThemLoosely()
    {
        var dictionary = YamlSerializer.Deserialize<Hashtable>("outer:\n  inner: 1\nitems:\n  - a\n  - b\n")!;

        Assert.AreEqual(2, dictionary.Count);
        Assert.IsNotNull(dictionary["outer"]);
        Assert.IsNotNull(dictionary["items"]);
    }

    /// <summary>
    /// Verifies that binding a mapping into a non-instantiable dictionary type throws, rather than failing later
    /// with a less specific error.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenUntypedDictionaryTypeIsNotInstantiable_ShouldThrowExactly()
    {
        _ = Assert.ThrowsExactly<YamlSerializationException>(() =>
        {
            _ = YamlSerializer.Deserialize<IDictionary>("name: Ada\n");
        });
    }

    /// <summary>
    /// Verifies that a scalar where a mapping is required throws rather than binding a partial result.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenUntypedDictionaryReceivesScalar_ShouldThrowExactly()
    {
        _ = Assert.ThrowsExactly<YamlSerializationException>(() =>
        {
            _ = YamlSerializer.Deserialize<Hashtable>("just-a-scalar");
        });
    }
}
