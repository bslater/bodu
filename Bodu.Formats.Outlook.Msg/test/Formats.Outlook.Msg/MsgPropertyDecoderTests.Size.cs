// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MsgPropertyDecoderTests.Size.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Compound;

namespace Bodu.Formats.Outlook.Msg;

public partial class MsgPropertyDecoderTests
{
    /// <summary>
    /// Verifies that a variable-length record whose declared size disagrees with its value stream is kept under
    /// compatible validation — the stream is the payload — but rejected under strict validation, closing the
    /// parser-differential a reader that ignores the declared size leaves open.
    /// </summary>
    [TestMethod]
    public void Decode_WhenDeclaredSizeDisagreesWithStream_ShouldKeepOrThrowByValidationLevel()
    {
        const uint SubjectTag = 0x0037001F;
        byte[] bytes = System.Text.Encoding.Unicode.GetBytes("Hello");
        var builder = new MsgFixtureBuilder()
            .AddEntryWithoutStream(SubjectTag, 999)
            .AddRawStream(MsgStreamNames.GetSubstgStreamName(SubjectTag), bytes);

        MapiPropertyCollection tolerant = Decode(builder);
        Assert.AreEqual("Hello", tolerant.GetString(MapiPropertyIds.Subject));

        _ = Assert.ThrowsExactly<OutlookMsgFormatException>(() =>
        {
            _ = Decode(builder, CompoundValidationLevel.Strict);
        });
    }

    /// <summary>
    /// Verifies that a record whose declared size matches its stream (plus the string terminator the format
    /// counts) decodes under strict validation — the cross-check must accept every well-formed writer.
    /// </summary>
    [TestMethod]
    public void Decode_WhenDeclaredSizeMatchesStream_ForStrict_ShouldDecode()
    {
        MapiPropertyCollection properties = Decode(
            new MsgFixtureBuilder().AddUnicode(MapiPropertyIds.Subject, "Hello").AddBinary(0x3701, [1, 2, 3]),
            CompoundValidationLevel.Strict);

        Assert.AreEqual("Hello", properties.GetString(MapiPropertyIds.Subject));
        Assert.AreEqual(3, properties.GetBinary(0x3701)!.Value.Length);
    }
}
