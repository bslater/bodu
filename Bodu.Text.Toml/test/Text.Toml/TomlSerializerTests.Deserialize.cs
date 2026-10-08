// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializerTests.Deserialize.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Test.Assertions;
using Bodu.Test.IO;
using Bodu.Text.Serialization;
using Bodu.Text.Toml.Document;
using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Serialization;
using Bodu.Text.Toml.Writer;

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
    /// Verifies that reading a finite TOML float outside the <see cref="float" /> range throws
    /// <see cref="TomlSerializationException" /> rather than reading as an infinity, from just beyond the largest
    /// finite magnitude to far beyond it, positive and negative.
    /// </summary>
    /// <param name="toml">The TOML document line carrying the float outside the range.</param>
    [TestMethod]
    [DataRow("Value = 3.4028236e38\n", DisplayName = "just beyond float.MaxValue")]
    [DataRow("Value = -3.4028236e38\n", DisplayName = "just beyond float.MinValue")]
    [DataRow("Value = -1e300\n", DisplayName = "far beyond float.MinValue")]
    public void Deserialize_WhenFloatIsOutsideMemberRange_ForSingle_ShouldThrowTomlSerializationException(string toml)
    {
        _ = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<ValueModel<float>>(toml);
        });
    }

    /// <summary>
    /// Verifies that reading a finite TOML float outside the <see cref="Half" /> range throws
    /// <see cref="TomlSerializationException" /> rather than reading as an infinity, from just beyond the largest
    /// finite magnitude to beyond it, positive and negative.
    /// </summary>
    /// <param name="toml">The TOML document line carrying the float outside the range.</param>
    [TestMethod]
    [DataRow("Value = 65520.0\n", DisplayName = "just beyond Half.MaxValue")]
    [DataRow("Value = -65520.0\n", DisplayName = "just beyond Half.MinValue")]
    [DataRow("Value = 70000.0\n", DisplayName = "beyond Half.MaxValue")]
    public void Deserialize_WhenFloatIsOutsideMemberRange_ForHalf_ShouldThrowTomlSerializationException(string toml)
    {
        _ = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<ValueModel<Half>>(toml);
        });
    }

    /// <summary>
    /// Verifies that a TOML float that rounds to the largest finite <see cref="float" /> magnitude reads as that value:
    /// narrowing rounds to nearest, and only a value that rounds beyond the range is rejected.
    /// </summary>
    /// <param name="toml">The TOML document line carrying the float just inside the range.</param>
    /// <param name="expected">The expected value.</param>
    [TestMethod]
    [DataRow("Value = 3.4028235e38\n", float.MaxValue, DisplayName = "rounds to float.MaxValue")]
    [DataRow("Value = -3.4028235e38\n", float.MinValue, DisplayName = "rounds to float.MinValue")]
    public void Deserialize_WhenFloatRoundsToLargestFiniteValue_ForSingle_ShouldReadThatValue(string toml, float expected)
    {
        float actual = TomlSerializer.Deserialize<ValueModel<float>>(toml).Value;

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that a TOML float that rounds to the largest finite <see cref="Half" /> magnitude reads as that value:
    /// narrowing rounds to nearest, and only a value that rounds beyond the range is rejected.
    /// </summary>
    /// <param name="toml">The TOML document line carrying the float just inside the range.</param>
    /// <param name="expected">The expected value, widened to <see cref="double" />.</param>
    [TestMethod]
    [DataRow("Value = 65504.0\n", 65504.0, DisplayName = "Half.MaxValue")]
    [DataRow("Value = 65519.0\n", 65504.0, DisplayName = "rounds to Half.MaxValue")]
    [DataRow("Value = -65519.0\n", -65504.0, DisplayName = "rounds to Half.MinValue")]
    public void Deserialize_WhenFloatRoundsToLargestFiniteValue_ForHalf_ShouldReadThatValue(string toml, double expected)
    {
        Half actual = TomlSerializer.Deserialize<ValueModel<Half>>(toml).Value;

        Assert.AreEqual(expected, (double)actual);
    }

    /// <summary>
    /// Verifies that a TOML float too small for <see cref="float" /> reads as zero: underflow towards zero is rounding,
    /// not a range error.
    /// </summary>
    /// <param name="toml">The TOML document line carrying the float too small to represent.</param>
    [TestMethod]
    [DataRow("Value = 1e-50\n", DisplayName = "positive")]
    [DataRow("Value = -1e-50\n", DisplayName = "negative")]
    public void Deserialize_WhenFloatUnderflowsMemberType_ForSingle_ShouldReadZero(string toml)
    {
        float actual = TomlSerializer.Deserialize<ValueModel<float>>(toml).Value;

        Assert.AreEqual(0f, actual);
    }

    /// <summary>
    /// Verifies that a TOML float too small for <see cref="Half" /> reads as zero: underflow towards zero is rounding,
    /// not a range error.
    /// </summary>
    /// <param name="toml">The TOML document line carrying the float too small to represent.</param>
    [TestMethod]
    [DataRow("Value = 1e-10\n", DisplayName = "positive")]
    [DataRow("Value = -1e-10\n", DisplayName = "negative")]
    public void Deserialize_WhenFloatUnderflowsMemberType_ForHalf_ShouldReadZero(string toml)
    {
        Half actual = TomlSerializer.Deserialize<ValueModel<Half>>(toml).Value;

        Assert.AreEqual(0.0, (double)actual);
    }

    /// <summary>
    /// Verifies that TOML's <c>inf</c>, <c>-inf</c>, and <c>nan</c> read as the matching <see cref="float" /> values
    /// rather than being rejected as outside the range.
    /// </summary>
    /// <param name="toml">The TOML document line carrying the special float.</param>
    /// <param name="expected">The expected value.</param>
    [TestMethod]
    [DataRow("Value = inf\n", float.PositiveInfinity, DisplayName = "inf")]
    [DataRow("Value = -inf\n", float.NegativeInfinity, DisplayName = "-inf")]
    [DataRow("Value = nan\n", float.NaN, DisplayName = "nan")]
    public void Deserialize_WhenFloatIsInfinityOrNaN_ForSingle_ShouldReadMatchingValue(string toml, float expected)
    {
        float actual = TomlSerializer.Deserialize<ValueModel<float>>(toml).Value;

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that TOML's <c>inf</c>, <c>-inf</c>, and <c>nan</c> read as the matching <see cref="Half" /> values
    /// rather than being rejected as outside the range.
    /// </summary>
    /// <param name="toml">The TOML document line carrying the special float.</param>
    /// <param name="expected">The expected value, widened to <see cref="double" />.</param>
    [TestMethod]
    [DataRow("Value = inf\n", double.PositiveInfinity, DisplayName = "inf")]
    [DataRow("Value = -inf\n", double.NegativeInfinity, DisplayName = "-inf")]
    [DataRow("Value = nan\n", double.NaN, DisplayName = "nan")]
    public void Deserialize_WhenFloatIsInfinityOrNaN_ForHalf_ShouldReadMatchingValue(string toml, double expected)
    {
        Half actual = TomlSerializer.Deserialize<ValueModel<Half>>(toml).Value;

        Assert.AreEqual(expected, (double)actual);
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

    /// <summary>
    /// Verifies that a TOML integer given for a <see cref="double" /> member throws
    /// <see cref="TomlSerializationException" />, because the double converter accepts only a TOML float.
    /// </summary>
    /// <param name="toml">The document, which gives the member an integer.</param>
    [TestMethod]
    [DataRow("A = 0\n", DisplayName = "zero")]
    [DataRow("A = 1\n", DisplayName = "one")]
    public void Deserialize_WhenIntegerIsReadIntoDouble_ShouldThrowTomlSerializationException(string toml)
    {
        _ = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<DoubleMemberModel>(toml);
        });
    }

    /// <summary>
    /// Verifies that a type with a member of its own type reads each level of nested tables into its own instance and
    /// leaves the member <see langword="null" /> where the document has no table for it.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenModelIsRecursive_ShouldReadEachLevel()
    {
        RecursiveTableModel nested = TomlSerializer.Deserialize<RecursiveTableModel>("I=1\n[F]\nI=2\n[F.F]\nI=3\n");
        RecursiveTableModel flat = TomlSerializer.Deserialize<RecursiveTableModel>("I=1");

        Assert.AreEqual(1L, nested.I);
        Assert.IsNotNull(nested.F);
        Assert.AreEqual(2L, nested.F.I);
        Assert.IsNotNull(nested.F.F);
        Assert.AreEqual(3L, nested.F.F.I);
        Assert.IsNull(nested.F.F.F);
        Assert.AreEqual(1L, flat.I);
        Assert.IsNull(flat.F);
    }

    /// <summary>
    /// Verifies that a local time given for an <see cref="object" /> member is read as a <see cref="TomlElement" /> of
    /// the local-time kind that holds the time.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenObjectMemberHoldsLocalTime_ShouldReadElement()
    {
        ObjectMemberModel model = TomlSerializer.Deserialize<ObjectMemberModel>("A = 17:45:00\n");

        Assert.IsInstanceOfType<TomlElement>(model.A);
        var element = (TomlElement)model.A!;
        Assert.AreEqual(TomlValueKind.LocalTime, element.ValueKind);
        Assert.AreEqual(new TimeOnly(17, 45, 0), element.GetTimeOnly());
    }

    /// <summary>
    /// Verifies that a number whose kind or range does not suit the member it is given for throws
    /// <see cref="TomlSerializationException" /> rather than being converted: a float for an unsigned or a signed
    /// integer member, an integer for a <see cref="double" /> member, and a negative integer for an unsigned member.
    /// </summary>
    /// <param name="toml">The document, which gives one member the mismatched number.</param>
    [TestMethod]
    [DataRow("u = 1e300\n", DisplayName = "float into uint")]
    [DataRow("i = 1e300\n", DisplayName = "float into int")]
    [DataRow("f = 9223372036854775806\n", DisplayName = "integer into double")]
    [DataRow("u = -1\n", DisplayName = "negative integer into uint")]
    public void Deserialize_WhenNumberKindOrRangeMismatchesMember_ShouldThrowTomlSerializationException(string toml)
    {
        _ = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<NumberKindModel>(toml);
        });
    }

    /// <summary>
    /// Verifies that a number outside the range of the member it is given for throws
    /// <see cref="TomlSerializationException" />: an integer too large for a <see cref="byte" /> or an
    /// <see cref="sbyte" />, and a float too large for a <see cref="float" />.
    /// </summary>
    /// <param name="toml">The document, which gives one member a number outside its range.</param>
    [TestMethod]
    [DataRow("u8 = 300\n", DisplayName = "integer into byte")]
    [DataRow("i8 = 300\n", DisplayName = "integer into sbyte")]
    [DataRow("f32 = 1e300\n", DisplayName = "float beyond the float range")]
    public void Deserialize_WhenNumberOverflowsMemberType_ShouldThrowTomlSerializationException(string toml)
    {
        _ = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<NarrowNumberModel>(toml);
        });
    }

    /// <summary>
    /// Verifies that nested types sharing member names with the type that holds them bind each table's keys at their
    /// own level, and that the members of a nested type the document does not mention stay empty.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenNestedTypesShareMemberNames_ShouldBindEachLevel()
    {
        SharedNamesRootModel model = TomlSerializer.Deserialize<SharedNamesRootModel>("name = \"123\"\n[inter2]\nname = \"inter2\"\nage = 222");

        Assert.AreEqual("123", model.Name);
        Assert.AreEqual("inter2", model.Inter2.Name);
        Assert.AreEqual(222, model.Inter2.Age);
        Assert.AreEqual(string.Empty, model.Inter2.InterStruct2.Test);
        Assert.AreEqual(string.Empty, model.Inter2.InterStruct2.Name);
        Assert.AreEqual(0, model.Inter2.InterStruct2.Age);
    }

    /// <summary>
    /// Verifies that properties declared on an abstract base class are bound, as well as those the derived class
    /// declares.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenPropertiesAreDeclaredOnBaseClass_ShouldBindThem()
    {
        DerivedSectionsModel model = TomlSerializer.Deserialize<DerivedSectionsModel>("Junk = \"Whatever\"\n[A]\nx = 1\n[B]\nx = 2\n[C]\nx = 3\n");

        Assert.AreEqual("Whatever", model.Junk);
        Assert.IsNotNull(model.A);
        Assert.AreEqual(1, model.A.X);
        Assert.IsNotNull(model.B);
        Assert.AreEqual(2, model.B.X);
        Assert.IsNotNull(model.C);
        Assert.AreEqual(3, model.C.X);
    }

    /// <summary>
    /// Verifies that a converter whose <c>Read</c> takes the value without moving the reader, the documented converter
    /// shape, leaves the reader where the next member is bound.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenConverterReadsValueWithoutAdvancing_ShouldBindNextMember()
    {
        UppercaseNameModel model = TomlSerializer.Deserialize<UppercaseNameModel>("Name = \"a\"\nTitle = \"b\"\n");

        Assert.AreEqual("A", model.Name);
        Assert.AreEqual("b", model.Title);
    }

    /// <summary>
    /// Verifies that a sub-table header that extends a table after another table has been opened is bound, under the
    /// snake-case policy and with the get-only, populated members of the fix's own test.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenSubTableHeaderComesLater_ShouldBindIt()
    {
        const string toml =
            "[msbuild]\nproject = \"HelloWorld.csproj\"\n\n[github]\nuser = \"u\"\nrepo = \"r\"\n\n[msbuild.properties]\nPublishReadyToRun = false\n";
        var options = new TomlSerializerOptions
        {
            PropertyNamingPolicy = NamingPolicy.SnakeCaseLower,
            PreferredObjectCreationHandling = ObjectCreationHandling.Populate,
        };

        OutOfOrderRootModel model = TomlSerializer.Deserialize<OutOfOrderRootModel>(toml, options);

        Assert.AreEqual("HelloWorld.csproj", model.MSBuild.Project);
        Assert.AreEqual("u", model.GitHub.User);
        Assert.AreEqual("r", model.GitHub.Repo);
        Assert.IsTrue(model.MSBuild.Properties.TryGetValue("PublishReadyToRun", out object? publish), "The properties sub-table was not bound.");
        Assert.IsInstanceOfType<TomlElement>(publish);
        Assert.IsFalse(((TomlElement)publish!).GetBoolean());
    }

    /// <summary>
    /// Verifies that invalid UTF-8 read from a stream is reported at the byte offset, line and byte column of the
    /// malformed sequence.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenStreamHasInvalidUtf8_ShouldReportByteOffset()
    {
        byte[] toml = [.. "a = \""u8, 0xC3, 0x28, (byte)'"'];
        using var source = new MemoryStream(toml);

        TomlFormatException ex = Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = TomlSerializer.Deserialize<Dictionary<string, string>>(source);
        });

        Assert.AreEqual(5, ex.Offset);
        Assert.AreEqual(1, ex.LineNumber);
        Assert.AreEqual(6, ex.ColumnNumber);
    }

    /// <summary>
    /// A model with a single <see cref="double" /> member.
    /// </summary>
    private sealed class DoubleMemberModel
    {
        /// <summary>
        /// Gets or sets the floating-point value.
        /// </summary>
        /// <value>The value.</value>
        public double A { get; set; }
    }

    /// <summary>
    /// A model with a member of its own type, so each nested table is read into a new level.
    /// </summary>
    private sealed class RecursiveTableModel
    {
        /// <summary>
        /// Gets or sets the integer at this level.
        /// </summary>
        /// <value>The integer.</value>
        public long I { get; set; }

        /// <summary>
        /// Gets or sets the next level.
        /// </summary>
        /// <value>The next level, or <see langword="null" /> when the document has none.</value>
        public RecursiveTableModel? F { get; set; }
    }

    /// <summary>
    /// A model with a single <see cref="object" />-typed member.
    /// </summary>
    private sealed class ObjectMemberModel
    {
        /// <summary>
        /// Gets or sets the value, which reads as a <see cref="TomlElement" />.
        /// </summary>
        /// <value>The value, or <see langword="null" />.</value>
        public object? A { get; set; }
    }

    /// <summary>
    /// A model whose members are an unsigned integer, a signed integer and a <see cref="double" />.
    /// </summary>
    private sealed class NumberKindModel
    {
        /// <summary>
        /// Gets or sets the unsigned integer.
        /// </summary>
        /// <value>The unsigned integer.</value>
        [PropertyName("u")]
        public uint U { get; set; }

        /// <summary>
        /// Gets or sets the signed integer.
        /// </summary>
        /// <value>The signed integer.</value>
        [PropertyName("i")]
        public int I { get; set; }

        /// <summary>
        /// Gets or sets the floating-point value.
        /// </summary>
        /// <value>The floating-point value.</value>
        [PropertyName("f")]
        public double F { get; set; }
    }

    /// <summary>
    /// A model whose members are narrower than the 64-bit values TOML stores.
    /// </summary>
    private sealed class NarrowNumberModel
    {
        /// <summary>
        /// Gets or sets the unsigned byte.
        /// </summary>
        /// <value>The unsigned byte.</value>
        [PropertyName("u8")]
        public byte U8 { get; set; }

        /// <summary>
        /// Gets or sets the signed byte.
        /// </summary>
        /// <value>The signed byte.</value>
        [PropertyName("i8")]
        public sbyte I8 { get; set; }

        /// <summary>
        /// Gets or sets the single-precision value.
        /// </summary>
        /// <value>The single-precision value.</value>
        [PropertyName("f32")]
        public float F32 { get; set; }
    }

    /// <summary>
    /// The outer type of three nested types that share member names.
    /// </summary>
    private sealed class SharedNamesRootModel
    {
        /// <summary>
        /// Gets or sets the outer name.
        /// </summary>
        /// <value>The name.</value>
        [PropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the middle type.
        /// </summary>
        /// <value>The middle type.</value>
        [PropertyName("inter2")]
        public SharedNamesMiddleModel Inter2 { get; set; } = new();
    }

    /// <summary>
    /// The middle type of three nested types that share member names.
    /// </summary>
    private sealed class SharedNamesMiddleModel
    {
        /// <summary>
        /// Gets or sets the middle name.
        /// </summary>
        /// <value>The name.</value>
        [PropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the middle age.
        /// </summary>
        /// <value>The age.</value>
        [PropertyName("age")]
        public int Age { get; set; }

        /// <summary>
        /// Gets or sets the inner type, which the document does not mention.
        /// </summary>
        /// <value>The inner type.</value>
        public SharedNamesLeafModel InterStruct2 { get; set; } = new();
    }

    /// <summary>
    /// The inner type of three nested types that share member names.
    /// </summary>
    private sealed class SharedNamesLeafModel
    {
        /// <summary>
        /// Gets or sets a member only the inner type has.
        /// </summary>
        /// <value>The value; empty by default.</value>
        public string Test { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the inner name.
        /// </summary>
        /// <value>The name; empty by default.</value>
        [PropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the inner age.
        /// </summary>
        /// <value>The age.</value>
        [PropertyName("age")]
        public int Age { get; set; }
    }

    /// <summary>
    /// A table with a single integer member.
    /// </summary>
    private sealed class XSectionModel
    {
        /// <summary>
        /// Gets or sets the integer.
        /// </summary>
        /// <value>The integer.</value>
        [PropertyName("x")]
        public int X { get; set; }
    }

    /// <summary>
    /// An abstract base class that declares two table members.
    /// </summary>
    private abstract class BaseSectionsModel
    {
        /// <summary>
        /// Gets or sets the first table the base class declares.
        /// </summary>
        /// <value>The table, or <see langword="null" />.</value>
        public XSectionModel? A { get; set; }

        /// <summary>
        /// Gets or sets the second table the base class declares.
        /// </summary>
        /// <value>The table, or <see langword="null" />.</value>
        public XSectionModel? B { get; set; }
    }

    /// <summary>
    /// A class derived from <see cref="BaseSectionsModel" /> that declares a string and a table member of its own.
    /// </summary>
    private sealed class DerivedSectionsModel
        : BaseSectionsModel
    {
        /// <summary>
        /// Gets or sets the string the derived class declares.
        /// </summary>
        /// <value>The string.</value>
        public string Junk { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the table the derived class declares.
        /// </summary>
        /// <value>The table, or <see langword="null" />.</value>
        public XSectionModel? C { get; set; }
    }

    /// <summary>
    /// A model whose first member is read by <see cref="UppercaseStringConverter" />.
    /// </summary>
    private sealed class UppercaseNameModel
    {
        /// <summary>
        /// Gets or sets the name, read in upper case.
        /// </summary>
        /// <value>The name.</value>
        [Converter(typeof(UppercaseStringConverter))]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the title, read by the built-in converter.
        /// </summary>
        /// <value>The title.</value>
        public string Title { get; set; } = string.Empty;
    }

    /// <summary>
    /// A converter that reads a string in upper case, taking the value without moving the reader.
    /// </summary>
    private sealed class UppercaseStringConverter
        : TomlConverter<string>
    {
        /// <inheritdoc />
        public override string Read(ref TomlDocumentReader reader, Type typeToConvert, TomlSerializerOptions options) =>
            reader.GetString().ToUpperInvariant();

        /// <inheritdoc />
        public override void Write(Utf8TomlWriter writer, string value, TomlSerializerOptions options) =>
            writer.WriteString(value);
    }

    /// <summary>
    /// The root of the out-of-order sub-table model, whose table members are get-only and populated.
    /// </summary>
    private sealed class OutOfOrderRootModel
    {
        /// <summary>
        /// Gets the build table, populated in place.
        /// </summary>
        /// <value>The build table.</value>
        [PropertyName("msbuild")]
        public OutOfOrderMsBuildModel MSBuild { get; } = new();

        /// <summary>
        /// Gets the repository table, populated in place.
        /// </summary>
        /// <value>The repository table.</value>
        [PropertyName("github")]
        public OutOfOrderGitHubModel GitHub { get; } = new();
    }

    /// <summary>
    /// The build table of the out-of-order sub-table model.
    /// </summary>
    private sealed class OutOfOrderMsBuildModel
    {
        /// <summary>
        /// Gets or sets the project file.
        /// </summary>
        /// <value>The project file.</value>
        public string Project { get; set; } = string.Empty;

        /// <summary>
        /// Gets the build properties, populated in place from the sub-table that comes later.
        /// </summary>
        /// <value>The build properties.</value>
        public Dictionary<string, object> Properties { get; } = [];
    }

    /// <summary>
    /// The repository table of the out-of-order sub-table model.
    /// </summary>
    private sealed class OutOfOrderGitHubModel
    {
        /// <summary>
        /// Gets or sets the user.
        /// </summary>
        /// <value>The user.</value>
        public string User { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the repository.
        /// </summary>
        /// <value>The repository.</value>
        public string Repo { get; set; } = string.Empty;
    }
}
