// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializerTests.Deserialize.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Test.Assertions;
using Bodu.Test.IO;
using Bodu.Text.Toml.Document;

namespace Bodu.Text.Toml;

/// <summary>
/// Deserializes TOML text or bytes to a value.
/// </summary>
public partial class TomlSerializerTests
{
    /// <summary>
    /// Verifies that reading an integer that is outside the target type's range throws
    /// <see cref="TomlSerializationException" /> rather than silently wrapping, demonstrating the checked conversion.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenIntegerOutOfTargetRange_ShouldThrowTomlSerializationException()
    {
        TomlSerializationException ex = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<ValueModel<byte>>("Value = 999\n");
        });

        Assert.IsTrue(ex.Message.Contains("999", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that reading a negative integer into an unsigned target throws
    /// <see cref="TomlSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenNegativeIntoUnsigned_ShouldThrowTomlSerializationException()
    {
        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<ValueModel<ulong>>("Value = -1\n");
        });
    }

    /// <summary>
    /// Verifies that reading an integer literal that overflows the signed 64-bit range is rejected by the reader as a
    /// <see cref="TomlFormatException" />, because TOML stores integers as 64-bit signed.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenIntegerLiteralExceedsInt64_ShouldThrowTomlFormatException()
    {
        Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = TomlSerializer.Deserialize<ValueModel<long>>("Value = 9223372036854775808\n");
        });
    }

    /// <summary>
    /// Verifies that reading a negative TOML integer into a <see cref="UInt128" /> member throws
    /// <see cref="TomlSerializationException" />, demonstrating the checked conversion on the read path.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenNegativeIntoUInt128_ShouldThrowTomlSerializationException()
    {
        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<ValueModel<UInt128>>("Value = -1\n");
        });
    }

    /// <summary>
    /// Verifies that reading a multi-character string into a <see cref="char" /> member throws
    /// <see cref="TomlSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenCharStringTooLong_ShouldThrowTomlSerializationException()
    {
        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<ValueModel<char>>("Value = \"ab\"\n");
        });
    }

    /// <summary>
    /// Verifies that reading a string that is not a valid <c>D</c>-format GUID into a <see cref="Guid" /> member throws
    /// <see cref="TomlSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenGuidStringInvalid_ShouldThrowTomlSerializationException()
    {
        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<ValueModel<Guid>>("Value = \"not-a-guid\"\n");
        });
    }

    /// <summary>
    /// Verifies that reading a <see cref="Version" /> from a string with leading or trailing whitespace throws
    /// <see cref="TomlSerializationException" />, matching the strictness of the
    /// <see cref="System.Text.Json" /> converter.
    /// </summary>
    /// <param name="padded">The padded version text under test.</param>
    [TestMethod]
    [DataRow(" 1.2.3")]
    [DataRow("1.2.3 ")]
    public void Deserialize_WhenVersionStringPadded_ShouldThrowTomlSerializationException(string padded)
    {
        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<ValueModel<Version>>($"Value = \"{padded}\"\n");
        });
    }

    /// <summary>
    /// Verifies that reading a <see cref="Version" /> from a string that is not a parsable version throws
    /// <see cref="TomlSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenVersionStringInvalid_ShouldThrowTomlSerializationException()
    {
        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<ValueModel<Version>>("Value = \"not-a-version\"\n");
        });
    }

    /// <summary>
    /// Verifies that reading a <see cref="Version" /> from a non-string token throws
    /// <see cref="TomlSerializationException" />, because the converter requires a string.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenVersionFromInteger_ShouldThrowTomlSerializationException()
    {
        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<ValueModel<Version>>("Value = 1\n");
        });
    }

    /// <summary>
    /// Verifies that reading a <see cref="TimeSpan" /> from a string that does not match the constant format throws
    /// <see cref="TomlSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenTimeSpanStringInvalid_ShouldThrowTomlSerializationException()
    {
        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<ValueModel<TimeSpan>>("Value = \"not-a-timespan\"\n");
        });
    }

    /// <summary>
    /// Verifies that reading a <see cref="TimeSpan" /> from a non-string token throws
    /// <see cref="TomlSerializationException" />, because the converter requires the constant-format string.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenTimeSpanFromInteger_ShouldThrowTomlSerializationException()
    {
        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<ValueModel<TimeSpan>>("Value = 30\n");
        });
    }

    /// <summary>
    /// Verifies that reading a finite TOML float outside the <see cref="Half" /> range saturates to infinity rather than
    /// throwing, matching IEEE 754 narrowing and the behavior of the <see cref="float" /> converter.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenFloatExceedsHalfRange_ShouldSaturateToInfinity()
    {
        Half actual = TomlSerializer.Deserialize<ValueModel<Half>>("Value = 1e10\n").Value;

        Assert.IsTrue(Half.IsPositiveInfinity(actual));
    }

    /// <summary>
    /// Verifies that reading a <see cref="Half" /> from a non-float token throws
    /// <see cref="TomlSerializationException" />, mirroring the strictness of the <see cref="float" /> converter.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenHalfFromInteger_ShouldThrowTomlSerializationException()
    {
        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<ValueModel<Half>>("Value = 1\n");
        });
    }

    /// <summary>
    /// Verifies that a byte array written as a Base64 string is read back even under the default integer-array handling,
    /// because the reader accepts either form.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenByteArrayBase64UnderDefaultHandling_ShouldDecode()
    {
        byte[] actual = TomlSerializer.Deserialize<ValueModel<byte[]>>("Value = \"YWJj\"\n").Value;

        CollectionAssert.AreEqual(new byte[] { 0x61, 0x62, 0x63 }, actual);
    }

    /// <summary>
    /// Verifies that reading a byte array from an integer element outside the byte range throws
    /// <see cref="TomlSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenByteArrayElementOutOfRange_ShouldThrowTomlSerializationException()
    {
        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<ValueModel<byte[]>>("Value = [256]\n");
        });
    }

    /// <summary>
    /// Verifies that reading a byte array from a string that is not valid Base64 throws
    /// <see cref="TomlSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenByteArrayStringNotBase64_ShouldThrowTomlSerializationException()
    {
        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<ValueModel<byte[]>>("Value = \"!!!\"\n");
        });
    }

    /// <summary>
    /// Verifies that a memory-of-byte member written as a Base64 string reads back under the default integer-array
    /// handling, because the shared read path accepts either form.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenMemoryOfByteFromBase64UnderDefaultHandling_ShouldDecode()
    {
        Memory<byte> actual = TomlSerializer.Deserialize<ValueModel<Memory<byte>>>("Value = \"YWJj\"\n").Value;

        CollectionAssert.AreEqual(new byte[] { 0x61, 0x62, 0x63 }, actual.ToArray());
    }

    /// <summary>
    /// Verifies that reading a TOML value whose kind does not match the target scalar member throws
    /// <see cref="TomlSerializationException" /> across the representative type-mismatch cases.
    /// </summary>
    /// <param name="toml">The TOML document line carrying the mismatched value.</param>
    [TestMethod]
    [DataRow("Value = \"x\"\n", DisplayName = "string into int")]
    [DataRow("Value = 1.5\n", DisplayName = "float into int")]
    public void Deserialize_WhenValueKindMismatchForInt_ShouldThrowTomlSerializationException(string toml)
    {
        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<ValueModel<int>>(toml);
        });
    }

    /// <summary>
    /// Verifies that reading a TOML integer into a <see cref="string" /> member throws
    /// <see cref="TomlSerializationException" />, because the string converter requires a string token.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenIntegerIntoString_ShouldThrowTomlSerializationException()
    {
        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<ValueModel<string>>("Value = 5\n");
        });
    }

    /// <summary>
    /// Verifies that deserializing into an <see cref="object" />-typed member yields a <see cref="TomlElement" />
    /// carrying the matching <see cref="TomlValueKind" /> for each TOML value kind.
    /// </summary>
    /// <param name="toml">The TOML document line carrying the value.</param>
    /// <param name="kind">The expected value kind of the surfaced element.</param>
    [TestMethod]
    [DataRow("Value = \"x\"\n", TomlValueKind.String, DisplayName = "string")]
    [DataRow("Value = 5\n", TomlValueKind.Integer, DisplayName = "integer")]
    [DataRow("Value = 1.5\n", TomlValueKind.Float, DisplayName = "float")]
    [DataRow("Value = true\n", TomlValueKind.Boolean, DisplayName = "boolean")]
    [DataRow("Value = [1, 2]\n", TomlValueKind.Array, DisplayName = "array")]
    [DataRow("Value = { A = 1 }\n", TomlValueKind.Table, DisplayName = "table")]
    public void Deserialize_WhenObjectMember_ShouldSurfaceTomlElement(string toml, TomlValueKind kind)
    {
        object actual = TomlSerializer.Deserialize<ValueModel<object>>(toml).Value;

        Assert.IsInstanceOfType<TomlElement>(actual);
        Assert.AreEqual(kind, ((TomlElement)actual).ValueKind);
    }

    /// <summary>
    /// Verifies that deserializing a whole document into <see cref="object" /> yields a table-kind
    /// <see cref="TomlElement" /> exposing the document's properties.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenObjectRoot_ShouldSurfaceTableElement()
    {
        object actual = TomlSerializer.Deserialize<object>("A = 1\n");

        Assert.IsInstanceOfType<TomlElement>(actual);

        var element = (TomlElement)actual;
        Assert.AreEqual(TomlValueKind.Table, element.ValueKind);
        Assert.AreEqual(1L, element.GetProperty("A").GetInt64());
    }

    /// <summary>
    /// Verifies that a deserialized <see cref="TomlElement" /> remains readable after deserialization completes,
    /// because the serializer's internal backing document is never disposed.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenTomlElementMember_ShouldRemainReadableAfterwards()
    {
        TomlElement element = TomlSerializer.Deserialize<ValueModel<TomlElement>>("Value = [1, 2, 3]\n").Value;

        Assert.AreEqual(3, element.GetArrayLength());
        Assert.AreEqual(2L, element[1].GetInt64());
    }

    /// <summary>
    /// Verifies that <see cref="TomlSerializer.Deserialize{T}(Stream, TomlSerializerOptions?)" /> reads a value from a
    /// stream positioned at a canonical document.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenStreamSource_ShouldReturnValue()
    {
        using var source = new MemoryStream(Encoding.UTF8.GetBytes("Id = 7\nLabel = \"x\"\n"));

        StreamModel model = TomlSerializer.Deserialize<StreamModel>(source);

        Assert.AreEqual(7, model.Id);
        Assert.AreEqual("x", model.Label);
    }

    /// <summary>
    /// Verifies that <see cref="TomlSerializer.Deserialize{T}(Stream, TomlSerializerOptions?)" /> reads a value from a
    /// stream that does not support seeking.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenStreamIsNonSeekable_ShouldReturnValue()
    {
        using var source = new NonSeekableStream(Encoding.UTF8.GetBytes("Id = 7\nLabel = \"x\"\n"));

        StreamModel model = TomlSerializer.Deserialize<StreamModel>(source);

        Assert.AreEqual(7, model.Id);
        Assert.AreEqual("x", model.Label);
    }

    /// <summary>
    /// Verifies that <see cref="TomlSerializer.Deserialize{T}(string, TomlSerializerOptions?)" /> throws
    /// <see cref="ArgumentNullException" /> with <c>ParamName</c> <c>text</c> when the text is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenTextIsNull_ShouldThrowArgumentNullException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = TomlSerializer.Deserialize<GuardModel>((string)null!);
        }, "text");
    }

    /// <summary>
    /// Verifies that <see cref="TomlSerializer.Deserialize{T}(Stream, TomlSerializerOptions?)" /> throws
    /// <see cref="ArgumentNullException" /> with <c>ParamName</c> <c>source</c> when the stream is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenSourceIsNull_ForStreamOverload_ShouldThrowArgumentNullException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = TomlSerializer.Deserialize<GuardModel>((Stream)null!);
        }, "source");
    }

    /// <summary>
    /// Verifies that <see cref="TomlSerializer.Deserialize{T}(Stream, TomlSerializerOptions?)" /> throws
    /// <see cref="ArgumentException" /> with <c>ParamName</c> <c>source</c> when the stream does not support reading.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenSourceIsNotReadable_ShouldThrowArgumentException()
    {
        using var source = new NonReadableStream();

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentException>(() =>
        {
            _ = TomlSerializer.Deserialize<GuardModel>(source);
        }, "source");
    }

    /// <summary>
    /// Verifies that the read path accepts a TOML float, integer, or string token regardless of the configured
    /// handling, so a document produced under one setting reads under either.
    /// </summary>
    /// <param name="toml">The TOML document line carrying the decimal in one of the accepted token forms.</param>
    /// <param name="expected">The expected decimal value as invariant text.</param>
    [TestMethod]
    [DataRow("Value = 1.5\n", "1.5", DisplayName = "from float")]
    [DataRow("Value = 3\n", "3", DisplayName = "from integer")]
    [DataRow("Value = \"19.95\"\n", "19.95", DisplayName = "from string")]
    public void Deserialize_WhenDecimalFromAnyAcceptedToken_ShouldRead(string toml, string expected)
    {
        decimal actual = TomlSerializer.Deserialize<ValueModel<decimal>>(toml).Value;

        Assert.AreEqual(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), actual);
    }

    /// <summary>
    /// Verifies that reading a decimal from a string that is not an invariant-culture decimal throws
    /// <see cref="TomlSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenDecimalStringInvalid_ShouldThrowTomlSerializationException()
    {
        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<ValueModel<decimal>>("Value = \"not-a-decimal\"\n");
        });
    }

    /// <summary>
    /// Verifies that reading a decimal from a non-finite or out-of-range TOML float throws
    /// <see cref="TomlSerializationException" /> carrying the conversion <see cref="OverflowException" />.
    /// </summary>
    /// <param name="toml">The TOML document line carrying the unconvertible float.</param>
    [TestMethod]
    [DataRow("Value = nan\n", DisplayName = "nan")]
    [DataRow("Value = inf\n", DisplayName = "inf")]
    [DataRow("Value = 1e300\n", DisplayName = "out of range")]
    public void Deserialize_WhenDecimalFromUnconvertibleFloat_ShouldThrowTomlSerializationException(string toml)
    {
        TomlSerializationException ex = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<ValueModel<decimal>>(toml);
        });

        Assert.IsNotNull(ex.InnerException);
        Assert.IsInstanceOfType<OverflowException>(ex.InnerException);
    }

    /// <summary>
    /// Verifies that reading a decimal from a token that is neither a float, integer, nor string throws
    /// <see cref="TomlSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenDecimalFromBoolean_ShouldThrowTomlSerializationException()
    {
        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<ValueModel<decimal>>("Value = true\n");
        });
    }

}
