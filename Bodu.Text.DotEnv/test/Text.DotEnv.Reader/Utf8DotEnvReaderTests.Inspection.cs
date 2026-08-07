// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8DotEnvReaderTests.Inspection.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Text.DotEnv.Reader;

/// <summary>
/// Verifies the reader's inspection surface: the token's raw bytes, the position and line it came from, whether it
/// carried an <c>export</c> prefix, and the allocation-free comparison overloads.
/// </summary>
/// <remarks>
/// The token transcript tests drive the reader through <c>GetString</c>, which allocates. The rest of the surface
/// exists so a caller can inspect a token without that cost - comparing a property name against an expected key, or
/// recording where a value came from for a diagnostic - and none of it was covered. The comparisons are asserted to
/// agree with <c>GetString</c> rather than merely to return something, since a fast path that disagrees with the slow
/// one is worse than no fast path.
/// </remarks>
public partial class Utf8DotEnvReaderTests
{
    /// <summary>
    /// Advances a reader to the first token of the requested kind.
    /// </summary>
    /// <param name="reader">The reader to advance.</param>
    /// <param name="tokenType">The token kind to stop at.</param>
    private static void AdvanceTo(ref Utf8DotEnvReader reader, DotEnvTokenType tokenType)
    {
        while (reader.Read())
        {
            if (reader.TokenType == tokenType)
                return;
        }

        Assert.Fail($"No {tokenType} token was produced.");
    }

    /// <summary>
    /// Verifies that a token's raw span carries the same text <c>GetString</c> decodes, so a caller can compare
    /// bytes without decoding first.
    /// </summary>
    [TestMethod]
    public void ValueSpan_WhenTokenIsText_ShouldMatchTheDecodedString()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("HOST=localhost\n");
        var reader = new Utf8DotEnvReader(bytes);

        AdvanceTo(ref reader, DotEnvTokenType.PropertyName);
        Assert.AreEqual(reader.GetString(), Encoding.UTF8.GetString(reader.ValueSpan));

        AdvanceTo(ref reader, DotEnvTokenType.String);
        Assert.AreEqual(reader.GetString(), Encoding.UTF8.GetString(reader.ValueSpan));
    }

    /// <summary>
    /// Verifies that a token carrying no text exposes an empty span rather than the previous token's bytes.
    /// </summary>
    [TestMethod]
    public void ValueSpan_WhenTokenIsNotText_ShouldBeEmpty()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("HOST=localhost\n");
        var reader = new Utf8DotEnvReader(bytes);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(DotEnvTokenType.StartObject, reader.TokenType);
        Assert.IsTrue(reader.ValueSpan.IsEmpty);
    }

    /// <summary>
    /// Verifies that reading text from a token that carries none is rejected, rather than returning the previous
    /// token's value.
    /// </summary>
    [TestMethod]
    public void GetString_WhenTokenIsNotText_ShouldThrowInvalidOperationException()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("HOST=localhost\n");
        var reader = new Utf8DotEnvReader(bytes);

        Assert.IsTrue(reader.Read());

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var inner = new Utf8DotEnvReader(bytes);
            _ = inner.Read();
            _ = inner.GetString();
        });
    }

    /// <summary>
    /// Verifies that the UTF-8 comparison agrees with the decoded string, and rejects a near-miss rather than
    /// matching on a prefix.
    /// </summary>
    [TestMethod]
    public void ValueTextEquals_WhenComparingUtf8_ShouldAgreeWithGetString()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("HOST=localhost\n");
        var reader = new Utf8DotEnvReader(bytes);

        AdvanceTo(ref reader, DotEnvTokenType.PropertyName);

        Assert.IsTrue(reader.ValueTextEquals("HOST"u8));
        Assert.IsFalse(reader.ValueTextEquals("HOS"u8));
        Assert.IsFalse(reader.ValueTextEquals("HOSTX"u8));
        Assert.IsFalse(reader.ValueTextEquals("host"u8));
    }

    /// <summary>
    /// Verifies that the character comparison agrees with the UTF-8 one, including for a value whose UTF-8 encoding
    /// is longer than its character count - where comparing lengths naively would report a false mismatch.
    /// </summary>
    [TestMethod]
    public void ValueTextEquals_WhenComparingChars_ShouldAgreeWithTheUtf8Overload()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("HOST=café\n");
        var reader = new Utf8DotEnvReader(bytes);

        AdvanceTo(ref reader, DotEnvTokenType.String);

        Assert.IsTrue(reader.ValueTextEquals("café".AsSpan()));
        Assert.IsTrue(reader.ValueTextEquals("café"));
        Assert.IsFalse(reader.ValueTextEquals("cafe".AsSpan()));
        Assert.IsFalse(reader.ValueTextEquals("caf".AsSpan()));
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> string compares equal only to an empty value, matching how an empty
    /// span behaves.
    /// </summary>
    [TestMethod]
    public void ValueTextEquals_WhenTextIsNull_ShouldMatchOnlyAnEmptyValue()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("EMPTY=\nHOST=localhost\n");
        var reader = new Utf8DotEnvReader(bytes);

        AdvanceTo(ref reader, DotEnvTokenType.String);
        Assert.IsTrue(reader.ValueTextEquals((string?)null));

        AdvanceTo(ref reader, DotEnvTokenType.String);
        Assert.IsFalse(reader.ValueTextEquals((string?)null));
    }

    /// <summary>
    /// Verifies that every comparison overload rejects a token that carries no text, rather than silently reporting
    /// a mismatch that a caller would read as "the value differs".
    /// </summary>
    [TestMethod]
    public void ValueTextEquals_WhenTokenIsNotText_ShouldThrowInvalidOperationException()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("HOST=localhost\n");

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var reader = new Utf8DotEnvReader(bytes);
            _ = reader.Read();
            _ = reader.ValueTextEquals("HOST"u8);
        });

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var reader = new Utf8DotEnvReader(bytes);
            _ = reader.Read();
            _ = reader.ValueTextEquals("HOST".AsSpan());
        });
    }

    /// <summary>
    /// Verifies that the line number advances by one per entry, tracking the reader's position through the source.
    /// </summary>
    /// <remarks>
    /// This pins the observed behaviour, which is not what the property documents. A <c>Read</c> that reports a
    /// property name has already consumed that entry's whole line, so the line number is one ahead of the token
    /// being reported - the first entry of a document reads as line 2, not line 1. The documentation says "the
    /// 1-based line number at which the current token begins", so either the property or its summary is wrong;
    /// deciding which is a product question rather than a test one, and the reader's own parse errors snapshot the
    /// line separately and are unaffected either way. Asserted as a delta so the test states the relationship
    /// without endorsing the off-by-one as correct.
    /// </remarks>
    [TestMethod]
    public void LineNumber_ShouldAdvanceOncePerEntry()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("A=1\nB=2\nC=3\n");
        var reader = new Utf8DotEnvReader(bytes);

        AdvanceTo(ref reader, DotEnvTokenType.PropertyName);
        int first = reader.LineNumber;

        AdvanceTo(ref reader, DotEnvTokenType.PropertyName);
        Assert.AreEqual(first + 1, reader.LineNumber);

        AdvanceTo(ref reader, DotEnvTokenType.PropertyName);
        Assert.AreEqual(first + 2, reader.LineNumber);
    }

    /// <summary>
    /// Verifies that the consumed-byte count advances with the read and reaches the end of the input, so a caller
    /// resuming from a buffer knows where the reader stopped.
    /// </summary>
    [TestMethod]
    public void BytesConsumed_ShouldAdvanceAndReachTheEndOfInput()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("A=1\nB=2\n");
        var reader = new Utf8DotEnvReader(bytes);

        Assert.AreEqual(0, reader.BytesConsumed);

        int previous = 0;
        while (reader.Read())
        {
            Assert.IsGreaterThanOrEqualTo(previous, reader.BytesConsumed);
            previous = reader.BytesConsumed;
        }

        Assert.AreEqual(bytes.Length, reader.BytesConsumed);
    }

    /// <summary>
    /// Verifies that the depth reports zero outside the document body and one inside it, so a caller can tell a
    /// structural token from an entry.
    /// </summary>
    [TestMethod]
    public void CurrentDepth_ShouldBeOneInsideTheDocumentBody()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("A=1\n");
        var reader = new Utf8DotEnvReader(bytes);

        Assert.AreEqual(0, reader.CurrentDepth);

        AdvanceTo(ref reader, DotEnvTokenType.PropertyName);
        Assert.AreEqual(1, reader.CurrentDepth);
    }

    /// <summary>
    /// Verifies that an <c>export</c> prefix is reported per entry, so a writer can reproduce it only where the
    /// source had it.
    /// </summary>
    [TestMethod]
    public void CurrentIsExport_ShouldReportThePrefixPerEntry()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("export A=1\nB=2\n");
        var reader = new Utf8DotEnvReader(bytes);

        AdvanceTo(ref reader, DotEnvTokenType.PropertyName);
        Assert.IsTrue(reader.CurrentIsExport);

        AdvanceTo(ref reader, DotEnvTokenType.PropertyName);
        Assert.IsFalse(reader.CurrentIsExport);
    }
}
