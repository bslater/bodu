// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8BencodeWriterTests.WriteRawValue.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;
using Bodu.Text.Bencode.Writer;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies the <see cref="Utf8BencodeWriter.WriteRawValue" /> behaviour: verbatim emission of pre-encoded values at
/// the root and inside containers, validation of the supplied bytes, and participation in the writer's call-sequence
/// rules.
/// </summary>
public partial class Utf8BencodeWriterTests
{
    /// <summary>
    /// Verifies that a raw scalar written at the root is emitted verbatim.
    /// </summary>
    [TestMethod]
    public void WriteRawValue_WhenRawScalarAtRoot_ShouldEmitVerbatim()
    {
        string actual = Write(w => w.WriteRawValue("i42e"u8));

        Assert.AreEqual("i42e", actual);
    }

    /// <summary>
    /// Verifies that a raw container written as a dictionary value is emitted verbatim within the dictionary's
    /// canonical key ordering.
    /// </summary>
    [TestMethod]
    public void WriteRawValue_WhenRawContainerAsDictionaryValue_ShouldEmitWithinSortedDictionary()
    {
        string actual = Write(w =>
        {
            w.WriteStartDictionary();
            w.WritePropertyName("b");
            w.WriteRawValue("li1ei2ee"u8);
            w.WritePropertyName("a");
            w.WriteInteger(0);
            w.WriteEndDictionary();
        });

        Assert.AreEqual("d1:ai0e1:bli1ei2eee", actual);
    }

    /// <summary>
    /// Verifies that raw bytes that are not valid Bencode throw <see cref="BencodeFormatException" /> before
    /// anything is emitted.
    /// </summary>
    [TestMethod]
    public void WriteRawValue_WhenBytesMalformed_ShouldThrowBencodeFormatException()
    {
        var buffer = new ArrayBufferWriter<byte>();

        _ = Assert.ThrowsExactly<BencodeFormatException>(() =>
        {
            var writer = new Utf8BencodeWriter(buffer);
            writer.WriteRawValue("i42"u8);
        });

        Assert.AreEqual(0, buffer.WrittenCount);
    }

    /// <summary>
    /// Verifies that raw bytes carrying trailing data after the first complete value throw
    /// <see cref="BencodeFormatException" />, enforcing the single-value contract of the argument.
    /// </summary>
    [TestMethod]
    public void WriteRawValue_WhenBytesCarryTrailingData_ShouldThrowBencodeFormatException()
    {
        var buffer = new ArrayBufferWriter<byte>();

        _ = Assert.ThrowsExactly<BencodeFormatException>(() =>
        {
            var writer = new Utf8BencodeWriter(buffer);
            writer.WriteRawValue("i1ei2e"u8);
        });
    }

    /// <summary>
    /// Verifies that empty raw bytes throw <see cref="BencodeFormatException" />.
    /// </summary>
    [TestMethod]
    public void WriteRawValue_WhenBytesEmpty_ShouldThrowBencodeFormatException()
    {
        var buffer = new ArrayBufferWriter<byte>();

        _ = Assert.ThrowsExactly<BencodeFormatException>(() =>
        {
            var writer = new Utf8BencodeWriter(buffer);
            writer.WriteRawValue([]);
        });
    }

    /// <summary>
    /// Verifies that validation can be skipped explicitly, writing the bytes verbatim without parsing them.
    /// </summary>
    [TestMethod]
    public void WriteRawValue_WhenValidationSkipped_ShouldEmitBytesVerbatim()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8BencodeWriter(buffer);

        writer.WriteRawValue("i42"u8, skipInputValidation: true);

        Assert.AreEqual("i42", Encoding.Latin1.GetString(buffer.WrittenSpan));
    }

    /// <summary>
    /// Verifies that a raw value counts as the document's root value, so a second root value afterwards throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void WriteRawValue_WhenFollowedBySecondRootValue_ShouldThrowInvalidOperationException()
    {
        var buffer = new ArrayBufferWriter<byte>();

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var writer = new Utf8BencodeWriter(buffer);
            writer.WriteRawValue("i1e"u8);
            writer.WriteInteger(2);
        });
    }

    /// <summary>
    /// Verifies that a raw value written directly inside a dictionary before a property name throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void WriteRawValue_WhenInsideDictionaryWithoutPropertyName_ShouldThrowInvalidOperationException()
    {
        var buffer = new ArrayBufferWriter<byte>();

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var writer = new Utf8BencodeWriter(buffer);
            writer.WriteStartDictionary();
            writer.WriteRawValue("i1e"u8);
        });
    }

    /// <summary>
    /// Verifies that a raw value round-trips a slice recovered from a parsed document byte for byte, the
    /// info-dictionary re-emission scenario the member exists for.
    /// </summary>
    [TestMethod]
    public void WriteRawValue_WhenRawSliceFromParsedDocument_ShouldRoundTripExactBytes()
    {
        string source = Write(w =>
        {
            w.WriteStartDictionary();
            w.WritePropertyName("info");
            w.WriteStartDictionary();
            w.WritePropertyName("name");
            w.WriteString("file.txt");
            w.WritePropertyName("length");
            w.WriteInteger(42);
            w.WriteEndDictionary();
            w.WriteEndDictionary();
        });

        // Extract the encoded value of "info" (everything between the key and the final closing 'e').
        string infoSlice = source["d4:info".Length..^1];

        string actual = Write(w =>
        {
            w.WriteStartDictionary();
            w.WritePropertyName("info");
            w.WriteRawValue(Encoding.Latin1.GetBytes(infoSlice));
            w.WriteEndDictionary();
        });

        Assert.AreEqual(source, actual);
    }
}
