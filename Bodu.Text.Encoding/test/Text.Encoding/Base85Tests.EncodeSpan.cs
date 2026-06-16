// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base85Tests.EncodeSpan.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Coverage-focused tests for the Span&lt;char&gt; overloads of <see cref="Base85.Encode" /> and
/// <see cref="Base85.TryEncode" /> — specifically the delimiter-aware fast path (destination ≥ worst-case bound), the
/// rented-scratch slow path (destination smaller than the worst-case bound but large enough for the actual output),
/// and the empty-input + delimiter behaviour.
/// </summary>
public sealed partial class Base85Tests
{

    private static readonly byte[] SampleBytes = System.Text.Encoding.ASCII.GetBytes("Hello world!");

    /// <summary>
    /// Verifies that the span-based <see cref="Base85.Encode(ReadOnlySpan{byte}, Span{char}, Base85Variant, BaseFormattingOptions)" />
    /// emits the Ascii85 <c>&lt;~ ... ~&gt;</c> delimiter pair on the fast path when the destination is at least the
    /// worst-case bound.
    /// </summary>
    [TestMethod]
    public void Encode_SpanWithDelimitersAndExactDestination_ShouldEmitDelimitedOutput()
    {
        int size = Base85.GetEncodedLength(SampleBytes, Base85Variant.Ascii85, BaseFormattingOptions.IncludePrefix);
        char[] destination = new char[size];

        int written = Base85.Encode(SampleBytes, destination, Base85Variant.Ascii85, BaseFormattingOptions.IncludePrefix);

        Assert.AreEqual(size, written);
        string actual = new(destination, 0, written);
        Assert.AreEqual(Base85.Encode(SampleBytes, Base85Variant.Ascii85, BaseFormattingOptions.IncludePrefix), actual);
        Assert.IsTrue(actual.StartsWith("<~", StringComparison.Ordinal));
        Assert.IsTrue(actual.EndsWith("~>", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that the span-based <see cref="Base85.Encode(ReadOnlySpan{byte}, Span{char}, Base85Variant, BaseFormattingOptions)" />
    /// routes through the rented-scratch slow path when the destination is smaller than the worst-case bound but large
    /// enough for the actual delimiter-wrapped output (which is shorter when the Ascii85 <c>z</c> shortcut applies).
    /// </summary>
    [TestMethod]
    public void Encode_SpanWithDelimitersAndShortcutShrinksOutput_ShouldUseRentedScratch()
    {
        // 8 all-zero bytes encode to two 'z' shortcuts → 2 chars of data + 4 chars of delimiters = 6 chars total.
        byte[] zeros = new byte[8];
        int worstCase = Base85.GetMaxEncodedLength(zeros.Length, Base85Variant.Ascii85, BaseFormattingOptions.IncludePrefix);
        int actualLength = Base85.GetEncodedLength(zeros, Base85Variant.Ascii85, BaseFormattingOptions.IncludePrefix);

        Assert.IsTrue(actualLength < worstCase, "Test premise: shortcut output must be shorter than worst case.");

        char[] destination = new char[actualLength];

        int written = Base85.Encode(zeros, destination, Base85Variant.Ascii85, BaseFormattingOptions.IncludePrefix);

        Assert.AreEqual(written, actualLength);
        Assert.AreEqual("<~zz~>", new string(destination, 0, written));
    }

    /// <summary>
    /// Verifies that the span-based <see cref="Base85.Encode(ReadOnlySpan{byte}, Span{char}, Base85Variant, BaseFormattingOptions)" />
    /// throws <see cref="ArgumentException" /> when the destination is too small even for the actual delimited output.
    /// </summary>
    [TestMethod]
    public void Encode_SpanWithDelimitersAndTooSmallDestination_ShouldThrowExactly()
    {
        byte[] zeros = new byte[8];
        char[] destination = new char[1];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Base85.Encode(zeros, destination, Base85Variant.Ascii85, BaseFormattingOptions.IncludePrefix);
        });
    }

    /// <summary>
    /// Verifies that the span-based <see cref="Base85.Encode(ReadOnlySpan{byte}, Span{char}, Base85Variant, BaseFormattingOptions)" />
    /// with an empty source and <see cref="BaseFormattingOptions.IncludePrefix" /> writes exactly the four-character
    /// delimiter pair <c>&lt;~~&gt;</c>.
    /// </summary>
    [TestMethod]
    public void Encode_SpanWithEmptyInputAndDelimiters_ShouldWriteFourDelimiterChars()
    {
        char[] destination = new char[4];

        int written = Base85.Encode([], destination, Base85Variant.Ascii85, BaseFormattingOptions.IncludePrefix);

        Assert.AreEqual(4, written);
        Assert.AreEqual("<~~>", new string(destination));
    }

    /// <summary>
    /// Verifies that the span-based <see cref="Base85.Encode(ReadOnlySpan{byte}, Span{char}, Base85Variant, BaseFormattingOptions)" />
    /// with an empty source and <see cref="BaseFormattingOptions.IncludePrefix" /> throws when the destination cannot
    /// fit the four-character delimiter pair.
    /// </summary>
    [TestMethod]
    public void Encode_SpanWithEmptyInputAndDelimitersAndTooSmallDestination_ShouldThrowExactly()
    {
        char[] destination = new char[3];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Base85.Encode([], destination, Base85Variant.Ascii85, BaseFormattingOptions.IncludePrefix);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base85.Encode(ReadOnlySpan{byte}, Span{char}, Base85Variant, BaseFormattingOptions)" />
    /// without delimiters routes through the rented-scratch slow path when the destination shrinks due to the
    /// Ascii85 <c>z</c> shortcut.
    /// </summary>
    [TestMethod]
    public void Encode_SpanWithoutDelimitersAndShortcutShrinksOutput_ShouldUseRentedScratch()
    {
        // 4 zero bytes encode to a single 'z' character (vs 5 chars worst case).
        byte[] zeros = new byte[4];
        int actualLength = Base85.GetEncodedLength(zeros, Base85Variant.Ascii85);
        char[] destination = new char[actualLength];

        int written = Base85.Encode(zeros, destination, Base85Variant.Ascii85);

        Assert.AreEqual(written, actualLength);
        Assert.AreEqual("z", new string(destination, 0, written));
    }

    /// <summary>
    /// Verifies that the span-based <see cref="Base85.TryEncode(ReadOnlySpan{byte}, Span{char}, out int, Base85Variant, BaseFormattingOptions)" />
    /// with delimiters and a destination at the worst-case bound emits the delimiter pair.
    /// </summary>
    [TestMethod]
    public void TryEncode_SpanWithDelimitersAndExactDestination_ShouldReturnTrueWithDelimitedOutput()
    {
        int size = Base85.GetEncodedLength(SampleBytes, Base85Variant.Ascii85, BaseFormattingOptions.IncludePrefix);
        char[] destination = new char[size];

        bool ok = Base85.TryEncode(SampleBytes, destination, out int written, Base85Variant.Ascii85, BaseFormattingOptions.IncludePrefix);

        Assert.IsTrue(ok);
        Assert.AreEqual(size, written);
        Assert.AreEqual(Base85.Encode(SampleBytes, Base85Variant.Ascii85, BaseFormattingOptions.IncludePrefix), new string(destination, 0, written));
    }

    /// <summary>
    /// Verifies that <see cref="Base85.TryEncode(ReadOnlySpan{byte}, Span{char}, out int, Base85Variant, BaseFormattingOptions)" />
    /// routes through the rented-scratch slow path when the destination is smaller than the worst-case bound but large
    /// enough for the actual delimited output.
    /// </summary>
    [TestMethod]
    public void TryEncode_SpanWithDelimitersAndShortcutShrinksOutput_ShouldReturnTrueViaRentedScratch()
    {
        byte[] zeros = new byte[8];
        int actualLength = Base85.GetEncodedLength(zeros, Base85Variant.Ascii85, BaseFormattingOptions.IncludePrefix);
        char[] destination = new char[actualLength];

        bool ok = Base85.TryEncode(zeros, destination, out int written, Base85Variant.Ascii85, BaseFormattingOptions.IncludePrefix);

        Assert.IsTrue(ok);
        Assert.AreEqual(written, actualLength);
        Assert.AreEqual("<~zz~>", new string(destination, 0, written));
    }

    /// <summary>
    /// Verifies that <see cref="Base85.TryEncode(ReadOnlySpan{byte}, Span{char}, out int, Base85Variant, BaseFormattingOptions)" />
    /// returns <see langword="false" /> with <c>charsWritten = 0</c> when the destination is too small for the
    /// delimited output.
    /// </summary>
    [TestMethod]
    public void TryEncode_SpanWithDelimitersAndTooSmallDestination_ShouldReturnFalseAndZeroCount()
    {
        byte[] payload = SampleBytes;
        char[] destination = new char[2];

        bool ok = Base85.TryEncode(payload, destination, out int written, Base85Variant.Ascii85, BaseFormattingOptions.IncludePrefix);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, written);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.TryEncode(ReadOnlySpan{byte}, Span{char}, out int, Base85Variant, BaseFormattingOptions)" />
    /// with an empty source and delimiters writes the four-character delimiter pair and returns true.
    /// </summary>
    [TestMethod]
    public void TryEncode_SpanWithEmptyInputAndDelimiters_ShouldWriteFourDelimiterChars()
    {
        char[] destination = new char[4];

        bool ok = Base85.TryEncode([], destination, out int written, Base85Variant.Ascii85, BaseFormattingOptions.IncludePrefix);

        Assert.IsTrue(ok);
        Assert.AreEqual(4, written);
        Assert.AreEqual("<~~>", new string(destination));
    }

    /// <summary>
    /// Verifies that <see cref="Base85.TryEncode(ReadOnlySpan{byte}, Span{char}, out int, Base85Variant, BaseFormattingOptions)" />
    /// with an empty source and delimiters returns false when the destination cannot fit the delimiter pair.
    /// </summary>
    [TestMethod]
    public void TryEncode_SpanWithEmptyInputAndDelimitersAndTooSmallDestination_ShouldReturnFalse()
    {
        char[] destination = new char[3];

        bool ok = Base85.TryEncode([], destination, out int written, Base85Variant.Ascii85, BaseFormattingOptions.IncludePrefix);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, written);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.TryEncode(ReadOnlySpan{byte}, Span{char}, out int, Base85Variant, BaseFormattingOptions)" />
    /// with Z85 variant and an unaligned input throws <see cref="ArgumentException" /> rather than returning false.
    /// </summary>
    [TestMethod]
    public void TryEncode_SpanWithZ85AndUnalignedInput_ShouldThrowExactly()
    {
        char[] destination = new char[16];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Base85.TryEncode([0x01, 0x02, 0x03], destination, out _, Base85Variant.Z85);
        });
    }

}
