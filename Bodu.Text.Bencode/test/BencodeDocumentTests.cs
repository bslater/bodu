// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeDocumentTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Text.Bencode.Document;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies the read-only Bencode document object model (<see cref="BencodeDocument" />, <see cref="BencodeElement" />,
/// and <see cref="BencodeProperty" />) that mirrors <see cref="System.Text.Json.JsonDocument" />.
/// </summary>
[TestClass]
public partial class BencodeDocumentTests
{
    /// <summary>
    /// Decodes the supplied Latin-1 text to bytes so binary content survives unchanged.
    /// </summary>
    /// <param name="text">The Latin-1 text to decode.</param>
    /// <returns>The decoded bytes.</returns>
    private static byte[] Bytes(string text) => Encoding.Latin1.GetBytes(text);

    /// <summary>
    /// A representative torrent-shaped dictionary used across several tests.
    /// </summary>
    private const string TorrentSource = "d8:announce17:http://tracker.tx4:infod6:lengthi1024e4:name4:testee";

    /// <summary>
    /// Verifies that parsing a dictionary document exposes its scalar and nested properties through the read-only
    /// element surface.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Parse_WhenInputIsDictionary_ShouldExposeReadableElements()
    {
        using BencodeDocument document = BencodeDocument.Parse(Encoding.UTF8.GetBytes(TorrentSource));

        BencodeElement root = document.RootElement;

        Assert.AreEqual(BencodeValueKind.Object, root.ValueKind);
        Assert.AreEqual("http://tracker.tx", root.GetProperty("announce").GetString());
        Assert.AreEqual(1024L, root.GetProperty("info").GetProperty("length").GetInt64());
    }

    /// <summary>
    /// Verifies that <see cref="BencodeElement.GetString" /> decodes a byte-string property as UTF-8 text.
    /// </summary>
    [TestMethod]
    public void GetString_WhenElementIsByteString_ShouldReturnDecodedText()
    {
        using BencodeDocument document = BencodeDocument.Parse(Encoding.UTF8.GetBytes(TorrentSource));

        BencodeElement name = document.RootElement.GetProperty("info").GetProperty("name");

        Assert.AreEqual(BencodeValueKind.ByteString, name.ValueKind);
        Assert.AreEqual("test", name.GetString());
    }

    /// <summary>
    /// Verifies that <see cref="BencodeElement.GetBytes" /> returns the raw byte-string content.
    /// </summary>
    [TestMethod]
    public void GetBytes_WhenElementIsByteString_ShouldReturnRawBytes()
    {
        using BencodeDocument document = BencodeDocument.Parse(Encoding.UTF8.GetBytes("4:spam"));

        byte[] bytes = document.RootElement.GetBytes();

        CollectionAssert.AreEqual(Encoding.UTF8.GetBytes("spam"), bytes);
    }

    /// <summary>
    /// Verifies that <see cref="BencodeElement.EnumerateObject" /> yields every key/value pair in stored order.
    /// </summary>
    [TestMethod]
    public void EnumerateObject_WhenElementIsObject_ShouldYieldPairsInOrder()
    {
        using BencodeDocument document = BencodeDocument.Parse(Encoding.UTF8.GetBytes("d3:cow3:moo4:spam4:eggse"));

        List<string> names = [];
        List<string> values = [];
        foreach (BencodeProperty property in document.RootElement.EnumerateObject())
        {
            names.Add(property.Name);
            values.Add(property.Value.GetString());
        }

        CollectionAssert.AreEqual(new[] { "cow", "spam" }, names);
        CollectionAssert.AreEqual(new[] { "moo", "eggs" }, values);
    }

    /// <summary>
    /// Verifies that <see cref="BencodeElement.EnumerateArray" /> yields each element of a mixed-kind list in order.
    /// </summary>
    [TestMethod]
    public void EnumerateArray_WhenElementIsArray_ShouldYieldElementsInOrder()
    {
        using BencodeDocument document = BencodeDocument.Parse(Encoding.UTF8.GetBytes("li1e3:twoe"));

        List<BencodeValueKind> kinds = [];
        List<string> rendered = [];
        foreach (BencodeElement element in document.RootElement.EnumerateArray())
        {
            kinds.Add(element.ValueKind);
            rendered.Add(element.ToString());
        }

        CollectionAssert.AreEqual(new[] { BencodeValueKind.Integer, BencodeValueKind.ByteString }, kinds);
        CollectionAssert.AreEqual(new[] { "1", "two" }, rendered);
    }

    /// <summary>
    /// Verifies that <see cref="BencodeElement.GetArrayLength" /> and the indexer read elements positionally.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenElementIsArray_ShouldReturnElementAtPosition()
    {
        using BencodeDocument document = BencodeDocument.Parse(Encoding.UTF8.GetBytes("li1e3:twoe"));

        BencodeElement array = document.RootElement;

        Assert.AreEqual(2, array.GetArrayLength());
        Assert.AreEqual(1L, array[0].GetInt64());
        Assert.AreEqual("two", array[1].GetString());
    }

    /// <summary>
    /// Verifies that <see cref="BencodeElement.GetInt64" /> on a byte-string element throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void GetInt64_WhenElementIsByteString_ShouldThrowInvalidOperationException()
    {
        using BencodeDocument document = BencodeDocument.Parse(Encoding.UTF8.GetBytes("4:spam"));

        BencodeElement element = document.RootElement;

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = element.GetInt64();
        });
    }

    /// <summary>
    /// Verifies that <see cref="BencodeElement.GetProperty(string)" /> on an array element throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void GetProperty_WhenElementIsArray_ShouldThrowInvalidOperationException()
    {
        using BencodeDocument document = BencodeDocument.Parse(Encoding.UTF8.GetBytes("li1ee"));

        BencodeElement element = document.RootElement;

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = element.GetProperty("missing");
        });
    }

    /// <summary>
    /// Verifies that <see cref="BencodeElement.GetProperty(string)" /> throws <see cref="KeyNotFoundException" /> when
    /// no property with the requested name exists.
    /// </summary>
    [TestMethod]
    public void GetProperty_WhenNameIsAbsent_ShouldThrowKeyNotFoundException()
    {
        using BencodeDocument document = BencodeDocument.Parse(Encoding.UTF8.GetBytes("d3:cow3:mooe"));

        BencodeElement root = document.RootElement;

        _ = Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            _ = root.GetProperty("pig");
        });
    }

    /// <summary>
    /// Verifies that <see cref="BencodeElement.TryGetProperty(string, out BencodeElement)" /> returns
    /// <see langword="false" /> for an absent name and <see langword="true" /> for a present one.
    /// </summary>
    [TestMethod]
    public void TryGetProperty_WhenNameIsAbsent_ShouldReturnFalse()
    {
        using BencodeDocument document = BencodeDocument.Parse(Encoding.UTF8.GetBytes("d3:cow3:mooe"));

        BencodeElement root = document.RootElement;

        Assert.IsFalse(root.TryGetProperty("pig", out BencodeElement absent));
        Assert.AreEqual(default, absent);
        Assert.IsTrue(root.TryGetProperty("cow", out BencodeElement present));
        Assert.AreEqual("moo", present.GetString());
    }

    /// <summary>
    /// Verifies that the array indexer throws <see cref="ArgumentOutOfRangeException" /> when the index is outside the
    /// array bounds.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenIndexOutOfRange_ShouldThrowArgumentOutOfRangeException()
    {
        using BencodeDocument document = BencodeDocument.Parse(Encoding.UTF8.GetBytes("li1ee"));

        BencodeElement array = document.RootElement;

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = array[5];
        });
    }

    /// <summary>
    /// Verifies that accessing an element after the owning document has been disposed throws
    /// <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void ValueKind_WhenDocumentDisposed_ShouldThrowObjectDisposedException()
    {
        BencodeDocument document = BencodeDocument.Parse(Encoding.UTF8.GetBytes("4:spam"));
        BencodeElement element = document.RootElement;
        document.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = element.ValueKind;
        });
    }

    /// <summary>
    /// Verifies that calling <see cref="BencodeDocument.Dispose" /> more than once does not throw.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        BencodeDocument document = BencodeDocument.Parse(Encoding.UTF8.GetBytes("i1e"));

        document.Dispose();
        document.Dispose();
    }

    /// <summary>
    /// Verifies that <see cref="BencodeDocument.Parse(byte[])" /> throws <see cref="ArgumentNullException" /> when the
    /// array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenArrayIsNull_ShouldThrowArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = BencodeDocument.Parse((byte[])null!);
        });
    }

    /// <summary>
    /// Verifies that parsing malformed bytes throws <see cref="BencodeFormatException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenInputIsMalformed_ShouldThrowBencodeFormatException()
    {
        _ = Assert.ThrowsExactly<BencodeFormatException>(() =>
        {
            using BencodeDocument document = BencodeDocument.Parse(Encoding.UTF8.GetBytes("i1"));
        });
    }

    /// <summary>
    /// Verifies that a negative integer round-trips out of a parsed document with its original value.
    /// </summary>
    [TestMethod]
    public void GetInt64_WhenElementIsNegativeInteger_ShouldReturnOriginalValue()
    {
        using BencodeDocument document = BencodeDocument.Parse(Encoding.UTF8.GetBytes("i-42e"));

        Assert.AreEqual(-42L, document.RootElement.GetInt64());
    }

    /// <summary>
    /// Verifies that values read out of a nested document equal the originals that were encoded, confirming the flat
    /// index navigates the structure faithfully.
    /// </summary>
    [TestMethod]
    public void RootElement_WhenDocumentIsNested_ShouldReadOriginalValues()
    {
        using BencodeDocument document = BencodeDocument.Parse(Encoding.UTF8.GetBytes("d4:listli1ei2ei3ee3:str5:helloe"));

        BencodeElement root = document.RootElement;

        BencodeElement list = root.GetProperty("list");
        Assert.AreEqual(3, list.GetArrayLength());
        Assert.AreEqual(1L, list[0].GetInt64());
        Assert.AreEqual(2L, list[1].GetInt64());
        Assert.AreEqual(3L, list[2].GetInt64());
        Assert.AreEqual("hello", root.GetProperty("str").GetString());
    }
}
