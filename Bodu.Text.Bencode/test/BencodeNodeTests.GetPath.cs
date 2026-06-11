// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeNodeTests.GetPath.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode.Nodes;

/// <summary>
/// Verifies <see cref="BencodeNode.GetPath" />: root, object-key, array-index, and mixed paths, including the quoted
/// segment form for keys that are not simple identifiers.
/// </summary>
public partial class BencodeNodeTests
{
    /// <summary>
    /// Verifies that a node with no parent reports the root path.
    /// </summary>
    [TestMethod]
    public void GetPath_WhenNodeIsRoot_ShouldReturnDollar()
    {
        BencodeNode node = BencodeValue.Create(42);

        Assert.AreEqual("$", node.GetPath());
    }

    /// <summary>
    /// Verifies that a value nested under object keys reports dotted segments for simple keys.
    /// </summary>
    [TestMethod]
    public void GetPath_WhenNestedUnderSimpleKeys_ShouldReturnDottedPath()
    {
        var root = new BencodeObject
        {
            ["info"] = new BencodeObject { ["length"] = 1024 },
        };

        Assert.AreEqual("$.info.length", root["info"]!["length"]!.GetPath());
    }

    /// <summary>
    /// Verifies that an element of an array reports an index segment, including within a mixed object/array path.
    /// </summary>
    [TestMethod]
    public void GetPath_WhenNestedUnderArrayIndex_ShouldReturnIndexSegment()
    {
        var root = new BencodeObject
        {
            ["files"] = new BencodeArray(BencodeValue.Create("a"), BencodeValue.Create("b")),
        };

        Assert.AreEqual("$.files[1]", root["files"]![1]!.GetPath());
    }

    /// <summary>
    /// Verifies that a key containing characters outside ASCII letters, digits, and underscores is rendered as a
    /// quoted segment.
    /// </summary>
    [TestMethod]
    public void GetPath_WhenKeyIsNotSimpleIdentifier_ShouldReturnQuotedSegment()
    {
        var root = new BencodeObject
        {
            ["piece length"] = 16384,
        };

        Assert.AreEqual("$['piece length']", root["piece length"]!.GetPath());
    }

    /// <summary>
    /// Verifies that a quote within a key is escaped inside the quoted segment.
    /// </summary>
    [TestMethod]
    public void GetPath_WhenKeyContainsQuote_ShouldEscapeQuote()
    {
        var root = new BencodeObject
        {
            ["it's"] = 1,
        };

        Assert.AreEqual(@"$['it\'s']", root["it's"]!.GetPath());
    }
}
