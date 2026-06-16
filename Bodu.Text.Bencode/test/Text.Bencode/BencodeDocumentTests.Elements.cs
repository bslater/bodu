// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeDocumentTests.Elements.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Test.Assertions;
using Bodu.Text.Bencode.Document;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies the <see cref="BencodeElement" /> accessor surface: kind-specific getters reject elements of the wrong
/// kind, byte strings are exposed both as UTF-8 text and as raw bytes, and positional and named navigation skips
/// nested subtrees correctly.
/// </summary>
public partial class BencodeDocumentTests
{
    /// <summary>
    /// Verifies that <see cref="BencodeElement.GetInt64" /> throws <see cref="InvalidOperationException" /> when the
    /// element is a container. The byte-string case is covered by
    /// <see cref="GetInt64_WhenElementIsByteString_ShouldThrowInvalidOperationException" />.
    /// </summary>
    /// <param name="input">The Bencode document whose root is not an integer.</param>
    [TestMethod]
    [DataRow("le")]
    [DataRow("de")]
    public void GetInt64_WhenElementIsNotInteger_ShouldThrowInvalidOperationException(string input)
    {
        using var document = BencodeDocument.Parse(Bytes(input));
        BencodeElement root = document.RootElement;

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = root.GetInt64();
        });
    }

    /// <summary>
    /// Verifies that <see cref="BencodeElement.GetString" /> throws <see cref="InvalidOperationException" /> when the
    /// element is not a byte string.
    /// </summary>
    /// <param name="input">The Bencode document whose root is not a byte string.</param>
    [TestMethod]
    [DataRow("i1e")]
    [DataRow("le")]
    [DataRow("de")]
    public void GetString_WhenElementIsNotByteString_ShouldThrowInvalidOperationException(string input)
    {
        using var document = BencodeDocument.Parse(Bytes(input));
        BencodeElement root = document.RootElement;

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = root.GetString();
        });
    }

    /// <summary>
    /// Verifies that <see cref="BencodeElement.GetBytes" /> throws <see cref="InvalidOperationException" /> when the
    /// element is not a byte string.
    /// </summary>
    /// <param name="input">The Bencode document whose root is not a byte string.</param>
    [TestMethod]
    [DataRow("i1e")]
    [DataRow("le")]
    [DataRow("de")]
    public void GetBytes_WhenElementIsNotByteString_ShouldThrowInvalidOperationException(string input)
    {
        using var document = BencodeDocument.Parse(Bytes(input));
        BencodeElement root = document.RootElement;

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = root.GetBytes();
        });
    }

    /// <summary>
    /// Verifies that <see cref="BencodeElement.GetArrayLength" /> throws <see cref="InvalidOperationException" /> when
    /// the element is not an array.
    /// </summary>
    /// <param name="input">The Bencode document whose root is not an array.</param>
    [TestMethod]
    [DataRow("i1e")]
    [DataRow("4:spam")]
    [DataRow("de")]
    public void GetArrayLength_WhenElementIsNotArray_ShouldThrowInvalidOperationException(string input)
    {
        using var document = BencodeDocument.Parse(Bytes(input));
        BencodeElement root = document.RootElement;

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = root.GetArrayLength();
        });
    }

    /// <summary>
    /// Verifies that <see cref="BencodeElement.EnumerateArray" /> throws <see cref="InvalidOperationException" /> when
    /// the element is not an array.
    /// </summary>
    [TestMethod]
    public void EnumerateArray_WhenElementIsNotArray_ShouldThrowInvalidOperationException()
    {
        using var document = BencodeDocument.Parse(Bytes("de"));
        BencodeElement root = document.RootElement;

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = root.EnumerateArray();
        });
    }

    /// <summary>
    /// Verifies that <see cref="BencodeElement.EnumerateObject" /> throws <see cref="InvalidOperationException" />
    /// when the element is not an object.
    /// </summary>
    [TestMethod]
    public void EnumerateObject_WhenElementIsNotObject_ShouldThrowInvalidOperationException()
    {
        using var document = BencodeDocument.Parse(Bytes("le"));
        BencodeElement root = document.RootElement;

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = root.EnumerateObject();
        });
    }

    /// <summary>
    /// Verifies that the positional indexer throws <see cref="InvalidOperationException" /> when the element is not
    /// an array.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenElementIsNotArray_ShouldThrowInvalidOperationException()
    {
        using var document = BencodeDocument.Parse(Bytes("de"));
        BencodeElement root = document.RootElement;

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = root[0];
        });
    }

    /// <summary>
    /// Verifies that <see cref="BencodeElement.TryGetProperty(string, out BencodeElement)" /> throws
    /// <see cref="InvalidOperationException" /> when the element is not an object.
    /// </summary>
    [TestMethod]
    public void TryGetProperty_WhenElementIsNotObject_ShouldThrowInvalidOperationException()
    {
        using var document = BencodeDocument.Parse(Bytes("le"));
        BencodeElement root = document.RootElement;

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = root.TryGetProperty("a", out _);
        });
    }

    /// <summary>
    /// Verifies that <see cref="BencodeElement.GetProperty(string)" /> throws <see cref="ArgumentNullException" />
    /// with <c>ParamName</c> <c>propertyName</c> when the property name is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void GetProperty_WhenPropertyNameIsNull_ShouldThrowArgumentNullException()
    {
        using var document = BencodeDocument.Parse(Bytes("de"));
        BencodeElement root = document.RootElement;

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = root.GetProperty(null!);
        }, "propertyName");
    }

    /// <summary>
    /// Verifies that <see cref="BencodeElement.TryGetProperty(string, out BencodeElement)" /> throws
    /// <see cref="ArgumentNullException" /> with <c>ParamName</c> <c>propertyName</c> when the property name is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void TryGetProperty_WhenPropertyNameIsNull_ShouldThrowArgumentNullException()
    {
        using var document = BencodeDocument.Parse(Bytes("de"));
        BencodeElement root = document.RootElement;

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = root.TryGetProperty(null!, out _);
        }, "propertyName");
    }

    /// <summary>
    /// Verifies that the positional indexer throws <see cref="ArgumentOutOfRangeException" /> when the index is
    /// negative, reporting the indexer's <c>index</c> parameter name.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenIndexIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        using var document = BencodeDocument.Parse(Bytes("li1ee"));
        BencodeElement root = document.RootElement;

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(() =>
        {
            _ = root[-1];
        }, "index");
    }

    /// <summary>
    /// Verifies that <see cref="BencodeElement.ToString" /> renders the decimal value for an integer, the UTF-8 text
    /// for a byte string, and the kind name for containers.
    /// </summary>
    /// <param name="input">The Bencode document, expressed as Latin-1 text.</param>
    /// <param name="expected">The expected rendering of the root element.</param>
    [TestMethod]
    [DataRow("i42e", "42")]
    [DataRow("i-42e", "-42")]
    [DataRow("4:spam", "spam")]
    [DataRow("0:", "")]
    [DataRow("le", "Array")]
    [DataRow("de", "Object")]
    public void ToString_WhenElementKindVaries_ShouldRenderKindSpecificText(string input, string expected)
    {
        using var document = BencodeDocument.Parse(Bytes(input));

        Assert.AreEqual(expected, document.RootElement.ToString());
    }

    /// <summary>
    /// Verifies that the 64-bit integer extremes round-trip through a parsed document.
    /// </summary>
    /// <param name="input">The Bencode integer document.</param>
    /// <param name="expected">The expected decoded value.</param>
    [TestMethod]
    [DataRow("i9223372036854775807e", long.MaxValue)]
    [DataRow("i-9223372036854775808e", long.MinValue)]
    public void GetInt64_WhenElementIsInt64Extreme_ShouldReturnOriginalValue(string input, long expected)
    {
        using var document = BencodeDocument.Parse(Bytes(input));

        Assert.AreEqual(expected, document.RootElement.GetInt64());
    }

    /// <summary>
    /// Verifies that <see cref="BencodeElement.GetBytes" /> preserves byte-string content that is not valid UTF-8
    /// text.
    /// </summary>
    [TestMethod]
    public void GetBytes_WhenByteStringIsNotValidUtf8_ShouldReturnRawBytes()
    {
        byte[] payload = [0x00, 0x01, 0x7F, 0x80, 0xFF];
        byte[] source = [.. Bytes("5:"), .. payload];
        using var document = BencodeDocument.Parse(source);

        CollectionAssert.AreEqual(payload, document.RootElement.GetBytes());
    }

    /// <summary>
    /// Verifies that <see cref="BencodeElement.GetString" /> decodes a multi-byte UTF-8 byte string.
    /// </summary>
    [TestMethod]
    public void GetString_WhenByteStringContainsMultiByteUtf8_ShouldDecode()
    {
        byte[] source = [.. Bytes("6:"), .. Encoding.UTF8.GetBytes("héllo")];
        using var document = BencodeDocument.Parse(source);

        Assert.AreEqual("héllo", document.RootElement.GetString());
    }

    /// <summary>
    /// Verifies that <see cref="BencodeElement.GetProperty(string)" /> matches a key containing multi-byte UTF-8
    /// characters.
    /// </summary>
    [TestMethod]
    public void GetProperty_WhenKeyIsMultiByteUtf8_ShouldMatch()
    {
        byte[] source = [.. Bytes("d"), .. Bytes("2:"), .. Encoding.UTF8.GetBytes("é"), .. Bytes("i1ee")];
        using var document = BencodeDocument.Parse(source);

        Assert.AreEqual(1L, document.RootElement.GetProperty("é").GetInt64());
    }

    /// <summary>
    /// Verifies that <see cref="BencodeElement.GetArrayLength" /> reports zero for an empty array.
    /// </summary>
    [TestMethod]
    public void GetArrayLength_WhenArrayEmpty_ShouldReturnZero()
    {
        using var document = BencodeDocument.Parse(Bytes("le"));

        Assert.AreEqual(0, document.RootElement.GetArrayLength());
    }

    /// <summary>
    /// Verifies that positional indexing skips whole nested subtrees, so elements after a nested dictionary or list
    /// are located correctly.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenArrayContainsMixedKinds_ShouldSkipNestedSubtrees()
    {
        using var document = BencodeDocument.Parse(Bytes("li1ed1:a1:beli2ee4:spame"));
        BencodeElement root = document.RootElement;

        Assert.AreEqual(4, root.GetArrayLength());
        Assert.AreEqual(1L, root[0].GetInt64());
        Assert.AreEqual("b", root[1].GetProperty("a").GetString());
        Assert.AreEqual(2L, root[2][0].GetInt64());
        Assert.AreEqual("spam", root[3].GetString());
    }

    /// <summary>
    /// Verifies that each call to <see cref="BencodeElement.GetBytes" /> returns an independent copy, so mutating one
    /// result does not affect another.
    /// </summary>
    [TestMethod]
    public void GetBytes_WhenCalledTwice_ShouldReturnIndependentCopies()
    {
        using var document = BencodeDocument.Parse(Bytes("4:spam"));

        byte[] first = document.RootElement.GetBytes();
        first[0] = (byte)'X';
        byte[] second = document.RootElement.GetBytes();

        Assert.AreEqual((byte)'s', second[0]);
    }
}
