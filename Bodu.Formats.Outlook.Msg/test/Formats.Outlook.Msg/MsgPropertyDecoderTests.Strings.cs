// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MsgPropertyDecoderTests.Strings.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Compound;
using Bodu.Test;

namespace Bodu.Formats.Outlook.Msg;

public partial class MsgPropertyDecoderTests
{
    /// <summary>
    /// Verifies that code-page strings decode through the declared message code page regardless of the order the
    /// code-page entry appears in — the two-pass guarantee.
    /// </summary>
    /// <param name="testName">The scenario label.</param>
    /// <param name="codePage">The declared code page.</param>
    /// <param name="value">The string value to round-trip.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DataRow("Windows1252", 1252, "café résumé")]
    [DataRow("ShiftJis", 932, "こんにちは世界")]
    [DataRow("Windows1251", 1251, "Привет мир")]
    public void Decode_WhenString8WithCodePage_ShouldDecodeThroughDeclaredEncoding(string testName, int codePage, string value)
    {
        _ = testName;

        // The string entry deliberately precedes the code-page entry in stream order.
        MapiPropertyCollection properties = Decode(new MsgFixtureBuilder()
            .AddString8(MapiPropertyIds.Subject, value, codePage)
            .AddFixedEntry(0x3FFD0003, (ulong)(uint)codePage));

        Assert.AreEqual(value, properties.GetString(MapiPropertyIds.Subject));
    }

    /// <summary>
    /// Verifies that a writer-included trailing terminator is stripped from a decoded string.
    /// </summary>
    [TestMethod]
    public void Decode_WhenStreamIncludesTerminator_ShouldStripIt()
    {
        byte[] bytes = System.Text.Encoding.Unicode.GetBytes("Hello\0");
        MapiPropertyCollection properties = Decode(new MsgFixtureBuilder()
            .AddEntryWithoutStream(0x0037001F, (uint)bytes.Length)
            .AddRawStream("__substg1.0_0037001F", bytes));

        Assert.AreEqual("Hello", properties.GetString(MapiPropertyIds.Subject));
    }

    /// <summary>
    /// Verifies that an odd-length UTF-16 payload is truncated under compatible validation and throws under strict
    /// validation.
    /// </summary>
    [TestMethod]
    public void Decode_WhenUnicodePayloadOddLength_ShouldTruncateOrThrowByValidationLevel()
    {
        byte[] odd = { 0x48, 0x00, 0x69 };
        var builder = new MsgFixtureBuilder()
            .AddEntryWithoutStream(0x0037001F, (uint)odd.Length + 2)
            .AddRawStream("__substg1.0_0037001F", odd);

        MapiPropertyCollection tolerant = Decode(builder);
        Assert.AreEqual("H", tolerant.GetString(MapiPropertyIds.Subject));

        _ = Assert.ThrowsExactly<OutlookMsgFormatException>(() =>
        {
            _ = Decode(builder, CompoundValidationLevel.Strict);
        });
    }

    /// <summary>
    /// Verifies that a message declaring no code page decodes code-page strings through the Windows-1252 fallback.
    /// </summary>
    [TestMethod]
    public void Decode_WhenNoCodePageDeclared_ShouldFallBackToWindows1252()
    {
        MapiPropertyCollection properties = Decode(new MsgFixtureBuilder()
            .AddString8(MapiPropertyIds.Subject, "café", 1252));

        Assert.AreEqual("café", properties.GetString(MapiPropertyIds.Subject));
    }
}
