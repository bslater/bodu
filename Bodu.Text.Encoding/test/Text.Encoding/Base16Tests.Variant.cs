// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base16Tests.Variant.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Encoding;

public sealed partial class Base16Tests
{
    /// <summary>
    /// Verifies that <see cref="Base16.Encode(ReadOnlySpan{byte}, Base16Variant, BaseFormattingOptions)" /> with
    /// <see cref="Base16Variant.Lower" /> produces the same lower-case output as the default option-based overload.
    /// </summary>
    [TestMethod]
    public void Encode_WhenVariantIsLower_ShouldMatchDefaultLowerCase()
    {
        var actual = Base16.Encode(CanonicalBytes.AsSpan(), Base16Variant.Lower);

        Assert.AreEqual(CanonicalHexLower, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.Encode(ReadOnlySpan{byte}, Base16Variant, BaseFormattingOptions)" /> with
    /// <see cref="Base16Variant.Upper" /> produces the same upper-case output as the
    /// <see cref="BaseFormattingOptions.UpperCase" /> option-based overload.
    /// </summary>
    [TestMethod]
    public void Encode_WhenVariantIsUpper_ShouldMatchUpperCaseOption()
    {
        var actual = Base16.Encode(CanonicalBytes.AsSpan(), Base16Variant.Upper);

        Assert.AreEqual(CanonicalHexUpper, actual);
        Assert.AreEqual(Base16.Encode(CanonicalBytes.AsSpan(), BaseFormattingOptions.UpperCase), actual);
    }

    /// <summary>
    /// Verifies that the variant takes precedence over <see cref="BaseFormattingOptions.UpperCase" />: a
    /// <see cref="Base16Variant.Lower" /> variant forces lower case even when the upper-case flag is also supplied.
    /// </summary>
    [TestMethod]
    public void Encode_WhenVariantLowerAndUpperCaseFlagSet_ShouldForceLowerCase()
    {
        var actual = Base16.Encode(CanonicalBytes.AsSpan(), Base16Variant.Lower, BaseFormattingOptions.UpperCase);

        Assert.AreEqual(CanonicalHexLower, actual);
    }

    /// <summary>
    /// Verifies that the variant takes precedence over the absence of <see cref="BaseFormattingOptions.UpperCase" />:
    /// a <see cref="Base16Variant.Upper" /> variant forces upper case even when no case flag is supplied.
    /// </summary>
    [TestMethod]
    public void Encode_WhenVariantUpperAndNoCaseFlag_ShouldForceUpperCase()
    {
        var actual = Base16.Encode(CanonicalBytes.AsSpan(), Base16Variant.Upper, BaseFormattingOptions.None);

        Assert.AreEqual(CanonicalHexUpper, actual);
    }

    /// <summary>
    /// Verifies that the variant overload preserves non-case formatting flags such as
    /// <see cref="BaseFormattingOptions.IncludePrefix" /> alongside the variant-selected case.
    /// </summary>
    [TestMethod]
    public void Encode_WhenVariantUpperWithPrefixFlag_ShouldApplyBothPrefixAndUpperCase()
    {
        var actual = Base16.Encode(CanonicalBytes.AsSpan(), Base16Variant.Upper, BaseFormattingOptions.IncludePrefix);

        Assert.AreEqual("0x" + CanonicalHexUpper, actual);
    }

    /// <summary>
    /// Verifies that the byte-array variant overload agrees with the span variant overload for the same input and
    /// variant.
    /// </summary>
    [TestMethod]
    public void Encode_ByteArrayVariantOverload_ShouldMatchSpanVariantOverload()
    {
        var fromArray = Base16.Encode(CanonicalBytes, Base16Variant.Upper);
        var fromSpan = Base16.Encode(CanonicalBytes.AsSpan(), Base16Variant.Upper);

        Assert.AreEqual(fromSpan, fromArray);
        Assert.AreEqual(CanonicalHexUpper, fromArray);
    }

    /// <summary>
    /// Verifies that the offset/count variant overload encodes the selected slice using the supplied variant.
    /// </summary>
    [TestMethod]
    public void Encode_OffsetCountVariantOverload_ShouldEncodeSliceWithVariant()
    {
        var actual = Base16.Encode(CanonicalBytes, 1, 2, Base16Variant.Upper);

        Assert.AreEqual("ADBE", actual);
    }

    /// <summary>
    /// Verifies that the span-destination variant overload writes the same characters as the string-returning variant
    /// overload and reports the written count.
    /// </summary>
    [TestMethod]
    public void Encode_SpanDestinationVariantOverload_ShouldMatchStringVariantOverload()
    {
        Span<char> destination = new char[CanonicalBytes.Length * 2];

        var written = Base16.Encode(CanonicalBytes.AsSpan(), destination, Base16Variant.Upper);

        Assert.AreEqual(CanonicalHexUpper.Length, written);
        Assert.AreEqual(CanonicalHexUpper, new string(destination));
    }

    /// <summary>
    /// Verifies that the <see cref="Base16.TryEncode(ReadOnlySpan{byte}, Span{char}, out int, Base16Variant, BaseFormattingOptions)" />
    /// variant overload succeeds and writes the variant-selected case when the destination is large enough.
    /// </summary>
    [TestMethod]
    public void TryEncode_VariantOverload_WhenDestinationLargeEnough_ShouldWriteUpperCase()
    {
        Span<char> destination = new char[CanonicalBytes.Length * 2];

        var result = Base16.TryEncode(CanonicalBytes.AsSpan(), destination, out var charsWritten, Base16Variant.Upper);

        Assert.IsTrue(result);
        Assert.AreEqual(CanonicalHexUpper.Length, charsWritten);
        Assert.AreEqual(CanonicalHexUpper, new string(destination));
    }

    /// <summary>
    /// Verifies that the <see cref="Base16.TryEncode(ReadOnlySpan{byte}, Span{char}, out int, Base16Variant, BaseFormattingOptions)" />
    /// variant overload returns <see langword="false" /> and writes nothing when the destination is too small.
    /// </summary>
    [TestMethod]
    public void TryEncode_VariantOverload_WhenDestinationTooSmall_ShouldReturnFalse()
    {
        Span<char> destination = new char[(CanonicalBytes.Length * 2) - 1];

        var result = Base16.TryEncode(CanonicalBytes.AsSpan(), destination, out var charsWritten, Base16Variant.Upper);

        Assert.IsFalse(result);
        Assert.AreEqual(0, charsWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.EncodeToUtf8(ReadOnlySpan{byte}, Base16Variant)" /> produces the same UTF-8
    /// bytes as encoding to a string with the matching variant and transcoding to ASCII.
    /// </summary>
    [TestMethod]
    public void EncodeToUtf8_VariantOverload_ShouldMatchStringVariantOutput()
    {
        var utf8 = Base16.EncodeToUtf8(CanonicalBytes.AsSpan(), Base16Variant.Upper);

        Assert.AreEqual(CanonicalHexUpper, System.Text.Encoding.ASCII.GetString(utf8));
    }

    /// <summary>
    /// Verifies that <see cref="Base16.TryEncodeToUtf8(ReadOnlySpan{byte}, Span{byte}, out int, Base16Variant, BaseFormattingOptions)" />
    /// writes the variant-selected case when the destination is large enough.
    /// </summary>
    [TestMethod]
    public void TryEncodeToUtf8_VariantOverload_ShouldWriteUpperCase()
    {
        Span<byte> destination = new byte[CanonicalBytes.Length * 2];

        var result = Base16.TryEncodeToUtf8(CanonicalBytes.AsSpan(), destination, out var bytesWritten, Base16Variant.Upper);

        Assert.IsTrue(result);
        Assert.AreEqual(CanonicalHexUpper.Length, bytesWritten);
        Assert.AreEqual(CanonicalHexUpper, System.Text.Encoding.ASCII.GetString(destination));
    }

    /// <summary>
    /// Verifies that the <see cref="IBufferWriter{T}" /> character variant overload writes the variant-selected case.
    /// </summary>
    [TestMethod]
    public void Encode_BufferWriterCharVariantOverload_ShouldWriteUpperCase()
    {
        ArrayBufferWriter<char> writer = new();

        var written = Base16.Encode(CanonicalBytes.AsSpan(), writer, Base16Variant.Upper);

        Assert.AreEqual(CanonicalHexUpper.Length, written);
        Assert.AreEqual(CanonicalHexUpper, new string(writer.WrittenSpan));
    }

    /// <summary>
    /// Verifies that the <see cref="IBufferWriter{T}" /> UTF-8 variant overload writes the variant-selected case.
    /// </summary>
    [TestMethod]
    public void EncodeToUtf8_BufferWriterVariantOverload_ShouldWriteUpperCase()
    {
        ArrayBufferWriter<byte> writer = new();

        var written = Base16.EncodeToUtf8(CanonicalBytes.AsSpan(), writer, Base16Variant.Upper);

        Assert.AreEqual(CanonicalHexUpper.Length, written);
        Assert.AreEqual(CanonicalHexUpper, System.Text.Encoding.ASCII.GetString(writer.WrittenSpan));
    }
}
