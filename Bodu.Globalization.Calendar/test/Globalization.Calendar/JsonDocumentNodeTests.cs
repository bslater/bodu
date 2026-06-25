// ---------------------------------------------------------------------------------------------------------------
// <copyright file="JsonDocumentNodeTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the JSON-backed <see cref="IDocumentNode" /> adapter, focusing on non-scalar scalar reads and the
/// key-value-map projection.
/// </summary>
[TestClass]
public class JsonDocumentNodeTests
{
    private static JsonDocumentNode Node(string json) =>
        new(JsonDocument.Parse(json).RootElement);

    /// <summary>
    /// Verifies that scalar reads coerce primitive tokens to text and return <see langword="null" /> for object,
    /// array, and null-valued properties.
    /// </summary>
    [TestMethod]
    public void Scalar_ShouldCoercePrimitivesAndRejectNonScalars()
    {
        JsonDocumentNode node = Node("""{"str":"hi","num":5,"yes":true,"no":false,"obj":{"a":1},"arr":[1,2],"nul":null}""");

        Assert.AreEqual("hi", node.Scalar("str"));
        Assert.AreEqual("5", node.Scalar("num"));
        Assert.AreEqual("true", node.Scalar("yes"));
        Assert.AreEqual("false", node.Scalar("no"));

        Assert.IsNull(node.Scalar("obj"));
        Assert.IsNull(node.Scalar("arr"));
        Assert.IsNull(node.Scalar("nul"));
        Assert.IsNull(node.Scalar("absent"));
    }

    /// <summary>
    /// Verifies that the key-value-map projection reads each property of a JSON object as a key/value pair, rendering
    /// non-string values as text.
    /// </summary>
    [TestMethod]
    public void KeyValueList_ShouldProjectObjectProperties()
    {
        JsonDocumentNode node = Node("""{"params":{"k1":"v1","k2":2}}""");

        IReadOnlyList<(string Key, string Value)> pairs = node.KeyValueList("Parameters", "Parameter", "name", "value", "params");

        Assert.HasCount(2, pairs);
        Assert.Contains(p => p is { Key: "k1", Value: "v1" }, pairs);
        Assert.Contains(p => p is { Key: "k2", Value: "2" }, pairs);
    }

    /// <summary>
    /// Verifies that the key-value-map projection yields nothing when the property is absent or not an object.
    /// </summary>
    [TestMethod]
    public void KeyValueList_WhenPropertyMissingOrNotObject_ShouldBeEmpty()
    {
        JsonDocumentNode node = Node("""{"params":[1,2]}""");

        Assert.IsEmpty(node.KeyValueList("Parameters", "Parameter", "name", "value", "params"));
        Assert.IsEmpty(node.KeyValueList("Parameters", "Parameter", "name", "value", "absent"));
    }
}
