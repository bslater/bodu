// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MapiValueDecoderTests.TryDecodeVariableValue.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

#if OUTLOOK_PST
namespace Bodu.Formats.Outlook.Pst;
#else
namespace Bodu.Formats.Outlook.Msg;
#endif

public partial class MapiValueDecoderTests
{
    /// <summary>
    /// Verifies that a null payload is reported as undecodable rather than escaping as a null-reference failure —
    /// the decoder's documented contract is that it never throws.
    /// </summary>
    [TestMethod]
    public void TryDecodeVariableValue_WhenBytesNull_ShouldReturnFalse()
    {
        Assert.IsFalse(MapiValueDecoder.TryDecodeVariableValue(MapiPropertyType.Unicode, null!, Encoding.Unicode, strict: false, out object? value));
        Assert.IsNull(value);
    }

    /// <summary>
    /// Verifies that a null encoding is reported as undecodable for a code-page string rather than escaping as a
    /// null-reference failure.
    /// </summary>
    [TestMethod]
    public void TryDecodeVariableValue_WhenEncodingNull_ShouldReturnFalse()
    {
        Assert.IsFalse(MapiValueDecoder.TryDecodeVariableValue(MapiPropertyType.String8, [0x41], null!, strict: false, out object? value));
        Assert.IsNull(value);
    }

    /// <summary>
    /// Verifies that a UTF-8 code-page string carrying a leading byte-order mark decodes without the mark — the
    /// encoding's <c>GetString</c> does not strip it, so the decoder must.
    /// </summary>
    [TestMethod]
    public void TryDecodeVariableValue_WhenUtf8PayloadCarriesByteOrderMark_ShouldStripIt()
    {
        byte[] payload = [0xEF, 0xBB, 0xBF, (byte)'a', (byte)'b'];
        Encoding utf8 = MapiEncodingResolver.GetEncoding(65001, null);

        Assert.IsTrue(MapiValueDecoder.TryDecodeVariableValue(MapiPropertyType.String8, payload, utf8, strict: false, out object? value));
        Assert.AreEqual("ab", value);
    }
}
