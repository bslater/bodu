// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8BencodeReaderTests.Skip.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Bencode.Reader;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies the <see cref="Utf8BencodeReader.Skip" /> behaviour: that scalars are unaffected, that container starts
/// advance to their matching end token, and that reading continues correctly from the post-skip position.
/// </summary>
public partial class Utf8BencodeReaderTests
{
    /// <summary>
    /// Verifies that <see cref="Utf8BencodeReader.Skip" /> on an integer token is a no-op that leaves the reader on
    /// the same token with its value still readable.
    /// </summary>
    [TestMethod]
    public void Skip_WhenOnInteger_ShouldRemainOnSameToken()
    {
        var bytes = Bytes("li1e3:abce");
        var reader = new Utf8BencodeReader(bytes);
        Assert.IsTrue(reader.Read()); // StartList
        Assert.IsTrue(reader.Read()); // Integer 1
        var consumed = reader.BytesConsumed;

        reader.Skip();

        Assert.AreEqual(BencodeTokenType.Integer, reader.TokenType);
        Assert.AreEqual(1L, reader.GetInt64());
        Assert.AreEqual(consumed, reader.BytesConsumed);
        Assert.AreEqual(1, reader.CurrentDepth);

        // Continued reading is unaffected by the no-op skip.
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.ByteString, reader.TokenType);
        Assert.AreEqual("abc", reader.GetString());
    }

    /// <summary>
    /// Verifies that <see cref="Utf8BencodeReader.Skip" /> on a byte-string token is a no-op that leaves the reader
    /// on the same token with its content still readable.
    /// </summary>
    [TestMethod]
    public void Skip_WhenOnByteString_ShouldRemainOnSameToken()
    {
        var bytes = Bytes("l3:abci1ee");
        var reader = new Utf8BencodeReader(bytes);
        Assert.IsTrue(reader.Read()); // StartList
        Assert.IsTrue(reader.Read()); // ByteString abc
        var consumed = reader.BytesConsumed;

        reader.Skip();

        Assert.AreEqual(BencodeTokenType.ByteString, reader.TokenType);
        Assert.AreEqual("abc", reader.GetString());
        Assert.AreEqual(consumed, reader.BytesConsumed);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.Integer, reader.TokenType);
        Assert.AreEqual(1L, reader.GetInt64());
    }

    /// <summary>
    /// Verifies that <see cref="Utf8BencodeReader.Skip" /> before any token has been read is a no-op that does not
    /// disturb the cursor.
    /// </summary>
    [TestMethod]
    public void Skip_WhenTokenIsNone_ShouldBeNoOp()
    {
        var bytes = Bytes("le");
        var reader = new Utf8BencodeReader(bytes);

        reader.Skip();

        Assert.AreEqual(BencodeTokenType.None, reader.TokenType);
        Assert.AreEqual(0, reader.BytesConsumed);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.StartList, reader.TokenType);
    }

    /// <summary>
    /// Verifies that <see cref="Utf8BencodeReader.Skip" /> on a list start advances the reader to the matching list
    /// end at the original depth.
    /// </summary>
    [TestMethod]
    public void Skip_WhenOnListStart_ShouldAdvanceToMatchingEnd()
    {
        var bytes = Bytes("lli1eee");
        var reader = new Utf8BencodeReader(bytes);
        Assert.IsTrue(reader.Read()); // outer StartList
        Assert.IsTrue(reader.Read()); // inner StartList
        Assert.AreEqual(2, reader.CurrentDepth);

        reader.Skip();

        Assert.AreEqual(BencodeTokenType.EndList, reader.TokenType);
        Assert.AreEqual(1, reader.CurrentDepth);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.EndList, reader.TokenType);
        Assert.AreEqual(0, reader.CurrentDepth);
        Assert.IsFalse(reader.Read());
    }

    /// <summary>
    /// Verifies that <see cref="Utf8BencodeReader.Skip" /> on a dictionary start advances past the whole dictionary
    /// subtree and that reading continues with the following sibling value.
    /// </summary>
    [TestMethod]
    public void Skip_WhenOnDictionaryStart_ShouldAdvancePastSubtree()
    {
        var bytes = Bytes("ld3:cow3:mooe6:afterxe");
        var reader = new Utf8BencodeReader(bytes);
        Assert.IsTrue(reader.Read()); // StartList
        Assert.IsTrue(reader.Read()); // StartDictionary
        Assert.AreEqual(2, reader.CurrentDepth);

        reader.Skip();

        Assert.AreEqual(BencodeTokenType.EndDictionary, reader.TokenType);
        Assert.AreEqual(1, reader.CurrentDepth);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.ByteString, reader.TokenType);
        Assert.AreEqual("afterx", reader.GetString());

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.EndList, reader.TokenType);
        Assert.IsFalse(reader.Read());
    }

    /// <summary>
    /// Verifies that <see cref="Utf8BencodeReader.Skip" /> on the root container consumes the entire document and a
    /// subsequent read reports end of document.
    /// </summary>
    [TestMethod]
    public void Skip_WhenOnRootContainer_ShouldConsumeEntireDocument()
    {
        var bytes = Bytes("d3:cow3:moo4:spam4:eggse");
        var reader = new Utf8BencodeReader(bytes);
        Assert.IsTrue(reader.Read()); // StartDictionary

        reader.Skip();

        Assert.AreEqual(BencodeTokenType.EndDictionary, reader.TokenType);
        Assert.AreEqual(0, reader.CurrentDepth);
        Assert.AreEqual(bytes.Length, reader.BytesConsumed);
        Assert.IsFalse(reader.Read());
    }

    /// <summary>
    /// Verifies that <see cref="Utf8BencodeReader.Skip" /> on a deeply nested container start unwinds to that
    /// container's matching end without overshooting the enclosing containers.
    /// </summary>
    [TestMethod]
    public void Skip_WhenOnDeeplyNestedContainer_ShouldUnwindToMatchingEnd()
    {
        // [ [ [ [ 9 ] ] ], "tail" ]
        var bytes = Bytes("lllli9eeee4:taile");
        var reader = new Utf8BencodeReader(bytes);
        Assert.IsTrue(reader.Read()); // depth 1
        Assert.IsTrue(reader.Read()); // depth 2
        Assert.AreEqual(2, reader.CurrentDepth);

        reader.Skip(); // skip the depth-2 subtree (the inner [[[9]]])

        Assert.AreEqual(BencodeTokenType.EndList, reader.TokenType);
        Assert.AreEqual(1, reader.CurrentDepth);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.ByteString, reader.TokenType);
        Assert.AreEqual("tail", reader.GetString());

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.EndList, reader.TokenType);
        Assert.AreEqual(0, reader.CurrentDepth);
        Assert.IsFalse(reader.Read());
    }

    /// <summary>
    /// Verifies that <see cref="Utf8BencodeReader.Skip" /> from mid-document over a dictionary value skips only that
    /// value and leaves the reader positioned to read the next key.
    /// </summary>
    [TestMethod]
    public void Skip_WhenOnDictionaryValueMidDocument_ShouldSkipOnlyThatValue()
    {
        // { "a": [1,2,3], "b": 7 }
        var bytes = Bytes("d1:ali1ei2ei3ee1:bi7ee");
        var reader = new Utf8BencodeReader(bytes);
        Assert.IsTrue(reader.Read()); // StartDictionary
        Assert.IsTrue(reader.Read()); // PropertyName a
        Assert.AreEqual("a", reader.GetString());
        Assert.IsTrue(reader.Read()); // StartList (value of a)
        Assert.AreEqual(BencodeTokenType.StartList, reader.TokenType);

        reader.Skip(); // skip the list value

        Assert.AreEqual(BencodeTokenType.EndList, reader.TokenType);
        Assert.AreEqual(1, reader.CurrentDepth);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.PropertyName, reader.TokenType);
        Assert.AreEqual("b", reader.GetString());

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.Integer, reader.TokenType);
        Assert.AreEqual(7L, reader.GetInt64());

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.EndDictionary, reader.TokenType);
        Assert.IsFalse(reader.Read());
    }

    /// <summary>
    /// Verifies that <see cref="Utf8BencodeReader.Skip" /> on a property name skips the property's scalar value,
    /// mirroring <see cref="System.Text.Json.Utf8JsonReader.Skip" />, leaving the reader positioned to read the next
    /// key.
    /// </summary>
    [TestMethod]
    public void Skip_WhenOnPropertyNameWithScalarValue_ShouldSkipPropertyValue()
    {
        // { "a": 1, "b": 2 }
        var bytes = Bytes("d1:ai1e1:bi2ee");
        var reader = new Utf8BencodeReader(bytes);
        Assert.IsTrue(reader.Read()); // StartDictionary
        Assert.IsTrue(reader.Read()); // PropertyName a
        Assert.AreEqual(BencodeTokenType.PropertyName, reader.TokenType);

        reader.Skip(); // skip the value of "a"

        Assert.AreEqual(BencodeTokenType.Integer, reader.TokenType);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.PropertyName, reader.TokenType);
        Assert.AreEqual("b", reader.GetString());

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(2L, reader.GetInt64());

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.EndDictionary, reader.TokenType);
        Assert.IsFalse(reader.Read());
    }

    /// <summary>
    /// Verifies that <see cref="Utf8BencodeReader.Skip" /> on a property name whose value is a container skips the
    /// entire value subtree, mirroring <see cref="System.Text.Json.Utf8JsonReader.Skip" />, leaving the reader on the
    /// value's end token.
    /// </summary>
    [TestMethod]
    public void Skip_WhenOnPropertyNameWithContainerValue_ShouldSkipValueSubtree()
    {
        // { "a": [1,2,3], "b": 7 }
        var bytes = Bytes("d1:ali1ei2ei3ee1:bi7ee");
        var reader = new Utf8BencodeReader(bytes);
        Assert.IsTrue(reader.Read()); // StartDictionary
        Assert.IsTrue(reader.Read()); // PropertyName a
        Assert.AreEqual(BencodeTokenType.PropertyName, reader.TokenType);

        reader.Skip(); // skip the list value of "a"

        Assert.AreEqual(BencodeTokenType.EndList, reader.TokenType);
        Assert.AreEqual(1, reader.CurrentDepth);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.PropertyName, reader.TokenType);
        Assert.AreEqual("b", reader.GetString());

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(7L, reader.GetInt64());

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.EndDictionary, reader.TokenType);
        Assert.IsFalse(reader.Read());
    }

    /// <summary>
    /// Verifies that <see cref="Utf8BencodeReader.TrySkip" /> behaves as <see cref="Utf8BencodeReader.Skip" /> and
    /// returns <see langword="true" />, the contract for a reader operating over a complete buffer.
    /// </summary>
    [TestMethod]
    public void TrySkip_WhenOnContainerStart_ShouldSkipSubtreeAndReturnTrue()
    {
        var bytes = Bytes("ld3:cow3:mooei7ee");
        var reader = new Utf8BencodeReader(bytes);
        Assert.IsTrue(reader.Read()); // StartList
        Assert.IsTrue(reader.Read()); // StartDictionary

        Assert.IsTrue(reader.TrySkip());

        Assert.AreEqual(BencodeTokenType.EndDictionary, reader.TokenType);
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(7L, reader.GetInt64());
    }

    /// <summary>
    /// Verifies that <see cref="Utf8BencodeReader.Skip" /> on a container end token is a no-op.
    /// </summary>
    [TestMethod]
    public void Skip_WhenOnContainerEnd_ShouldBeNoOp()
    {
        var bytes = Bytes("le");
        var reader = new Utf8BencodeReader(bytes);
        Assert.IsTrue(reader.Read()); // StartList
        Assert.IsTrue(reader.Read()); // EndList
        var consumed = reader.BytesConsumed;

        reader.Skip();

        Assert.AreEqual(BencodeTokenType.EndList, reader.TokenType);
        Assert.AreEqual(consumed, reader.BytesConsumed);
        Assert.IsFalse(reader.Read());
    }
}
