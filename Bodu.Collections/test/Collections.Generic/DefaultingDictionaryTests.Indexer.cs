// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DefaultingDictionaryTests.Indexer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class DefaultingDictionaryTests
{
    /// <summary>
    /// Verifies that reading a missing key invokes the factory once, stores the result, and returns it.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenKeyMissing_ShouldInvokeFactoryStoreAndReturn()
    {
        var invocations = 0;
        var dictionary = new DefaultingDictionary<string, int>(key =>
        {
            invocations++;
            return key.Length;
        });

        var value = dictionary["four"];

        Assert.AreEqual(4, value);
        Assert.AreEqual(1, invocations);
        Assert.IsTrue(dictionary.ContainsKey("four"));
        Assert.AreEqual(1, dictionary.Count);
    }

    /// <summary>
    /// Verifies that reading a stored key returns the stored value without invoking the factory.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenKeyStored_ShouldNotInvokeFactory()
    {
        var invocations = 0;
        var dictionary = new DefaultingDictionary<string, int>(_ =>
        {
            invocations++;
            return -1;
        });
        dictionary.Add("a", 42);

        Assert.AreEqual(42, dictionary["a"]);
        Assert.AreEqual(0, invocations);
    }

    /// <summary>
    /// Verifies that repeated reads of the same missing key invoke the factory only on the first read.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenKeyReadTwice_ShouldInvokeFactoryOnce()
    {
        var invocations = 0;
        var dictionary = new DefaultingDictionary<string, int>(_ =>
        {
            invocations++;
            return 7;
        });

        _ = dictionary["a"];
        _ = dictionary["a"];

        Assert.AreEqual(1, invocations);
    }

    /// <summary>
    /// Verifies that a factory returning <see langword="null" /> for a reference-typed value stores the
    /// <see langword="null" /> as-is: the key becomes present and the factory is not invoked for it again.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenFactoryReturnsNull_ShouldStoreNullAsIs()
    {
        var invocations = 0;
        var dictionary = new DefaultingDictionary<string, string?>(_ =>
        {
            invocations++;
            return null;
        });

        var value = dictionary["a"];

        Assert.IsNull(value);
        Assert.AreEqual(1, invocations);
        Assert.IsTrue(dictionary.ContainsKey("a"));

        _ = dictionary["a"];

        Assert.AreEqual(1, invocations);
    }

    /// <summary>
    /// Verifies that the setter stores the assigned value without invoking the factory.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenSet_ShouldStoreValueWithoutInvokingFactory()
    {
        var invocations = 0;
        var dictionary = new DefaultingDictionary<string, int>(_ =>
        {
            invocations++;
            return -1;
        });

        dictionary["a"] = 5;

        Assert.AreEqual(5, dictionary["a"]);
        Assert.AreEqual(0, invocations);
    }
}
