// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedArrayTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

using Bodu.Text.Delimited.Nodes;

namespace Bodu.Text.Delimited.Nodes;

/// <summary>
/// Contains tests for the mutable <see cref="DelimitedArray" /> / <see cref="DelimitedObject" /> DOM.
/// </summary>
[TestClass]
public class DelimitedArrayTests
{
    /// <summary>
    /// Verifies that parsing produces a document array of header-keyed record objects.
    /// </summary>
    [TestMethod]
    public void Parse_WhenHeaderMode_ShouldBuildArrayOfObjects()
    {
        DelimitedArray document = DelimitedNode.Parse("name,age\nAda,36\n"u8);

        Assert.AreEqual(1, document.Count);
        DelimitedObject record = document[0].AsObject();
        Assert.AreEqual("Ada", record["name"].AsValue().Value);
        Assert.AreEqual("36", record["age"].AsValue().Value);
    }

    /// <summary>
    /// Verifies that a parsed document writes back to CSV that round-trips.
    /// </summary>
    [TestMethod]
    public void WriteTo_WhenRoundTripped_ShouldPreserveRecords()
    {
        DelimitedArray document = DelimitedNode.Parse("name,age\nAda,36\nGrace,45\n"u8);

        string text = Encoding.UTF8.GetString(document.ToUtf8Bytes());

        Assert.AreEqual("name,age\r\nAda,36\r\nGrace,45\r\n", text);
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedNode.DeepClone" /> produces an independent copy.
    /// </summary>
    [TestMethod]
    public void DeepClone_WhenMutatingClone_ShouldNotAffectOriginal()
    {
        DelimitedArray document = DelimitedNode.Parse("a\n1\n"u8);
        var clone = (DelimitedArray)document.DeepClone();

        clone[0].AsObject()["a"].AsValue().Value = "2";

        Assert.AreEqual("1", document[0].AsObject()["a"].AsValue().Value);
        Assert.AreEqual("2", clone[0].AsObject()["a"].AsValue().Value);
    }
}
