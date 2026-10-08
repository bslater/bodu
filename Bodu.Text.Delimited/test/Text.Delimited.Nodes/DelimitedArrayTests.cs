// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedArrayTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

using Bodu.Text.Delimited.Nodes;
using Bodu.Text.Delimited.Reader;

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
    /// Verifies that under <see cref="DelimitedDuplicateHeaderBehavior.TakeFirst" /> a record keeps the first value of
    /// a duplicated column under its name, and the later value under an empty name, rather than folding both into one
    /// field that holds the last value.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDuplicateHeaderTakeFirst_ShouldKeepTheFirstValueUnderTheName()
    {
        DelimitedArray document = DelimitedNode.Parse(
            "a,b,a\n1,2,3\n"u8, new DelimitedReaderOptions { DuplicateHeaderBehavior = DelimitedDuplicateHeaderBehavior.TakeFirst });

        DelimitedObject record = document[0].AsObject();
        CollectionAssert.AreEqual(new[] { "a", "b", string.Empty }, record.Keys.ToArray());
        Assert.AreEqual("1", record["a"].AsValue().Value);
        Assert.AreEqual("3", record[string.Empty].AsValue().Value);
    }

    /// <summary>
    /// Verifies that under <see cref="DelimitedDuplicateHeaderBehavior.TakeLast" /> a record keeps the last value of a
    /// duplicated column under its name, and the earlier value under an empty name.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDuplicateHeaderTakeLast_ShouldKeepTheLastValueUnderTheName()
    {
        DelimitedArray document = DelimitedNode.Parse(
            "a,b,a\n1,2,3\n"u8, new DelimitedReaderOptions { DuplicateHeaderBehavior = DelimitedDuplicateHeaderBehavior.TakeLast });

        DelimitedObject record = document[0].AsObject();
        CollectionAssert.AreEqual(new[] { string.Empty, "b", "a" }, record.Keys.ToArray());
        Assert.AreEqual("3", record["a"].AsValue().Value);
        Assert.AreEqual("1", record[string.Empty].AsValue().Value);
    }

    /// <summary>
    /// Verifies that both duplicate-header policies treat a name that three columns share alike: the winning column
    /// keeps the name, the other two share the empty name, and the record keeps the last of those two under it.
    /// </summary>
    /// <param name="policy">The duplicate-header policy.</param>
    /// <param name="expectedKeys">The record's keys in order, joined with <c>|</c>.</param>
    /// <param name="expectedNamed">The value kept under the name <c>a</c>.</param>
    /// <param name="expectedEmpty">The value kept under the empty name.</param>
    [TestMethod]
    [DataRow(DelimitedDuplicateHeaderBehavior.TakeFirst, "a|", "1", "3")]
    [DataRow(DelimitedDuplicateHeaderBehavior.TakeLast, "|a", "3", "2")]
    public void Parse_WhenANameRepeatsThreeTimes_ShouldKeepTheWinnerUnderTheNameAndTheOthersUnderAnEmptyName(
        DelimitedDuplicateHeaderBehavior policy,
        string expectedKeys,
        string expectedNamed,
        string expectedEmpty)
    {
        DelimitedArray document = DelimitedNode.Parse("a,a,a\n1,2,3\n"u8, new DelimitedReaderOptions { DuplicateHeaderBehavior = policy });

        DelimitedObject record = document[0].AsObject();
        Assert.AreEqual(expectedKeys, string.Join('|', record.Keys));
        Assert.AreEqual(expectedNamed, record["a"].AsValue().Value);
        Assert.AreEqual(expectedEmpty, record[string.Empty].AsValue().Value);
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
