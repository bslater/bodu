// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PercentEncodingTests.RoundTrip.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class PercentEncodingTests
{
    /// <summary>
    /// Returns every defined component mode.
    /// </summary>
    /// <returns>A sequence of single-mode rows for <c>[DynamicData]</c>.</returns>
    public static IEnumerable<object[]> AllModes()
    {
        yield return new object[] { PercentEncodingMode.UriComponent };
        yield return new object[] { PercentEncodingMode.PathSegment };
        yield return new object[] { PercentEncodingMode.Query };
        yield return new object[] { PercentEncodingMode.FormUrlEncoded };
    }

    /// <summary>
    /// Verifies that every single byte value round-trips through encode and decode in every mode. Tagged Regression
    /// because of the exhaustive 256-value sweep across four modes.
    /// </summary>
    /// <param name="mode">The component mode.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(AllModes))]
    public void RoundTrip_ForEverySingleByteValue_ShouldRecover(PercentEncodingMode mode)
    {
        for (int value = 0; value <= 255; value++)
        {
            byte[] original = { (byte)value };

            string encoded = PercentEncoding.Encode(original, mode);
            byte[] decoded = PercentEncoding.Decode(encoded.AsSpan(), mode);

            CollectionAssert.AreEqual(original, decoded, $"Round trip failed for byte 0x{value:X2} (mode {mode}).");
        }
    }

    /// <summary>
    /// Verifies that random byte payloads of lengths 0 through 512 round-trip in every mode. Tagged Regression
    /// because of the length sweep across four modes.
    /// </summary>
    /// <param name="mode">The component mode.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(AllModes))]
    public void RoundTrip_ForRandomPayloads_ShouldRecover(PercentEncodingMode mode)
    {
        var random = new Random(0x2A17);
        for (int length = 0; length <= 512; length++)
        {
            byte[] original = new byte[length];
            random.NextBytes(original);

            string encoded = PercentEncoding.Encode(original, mode);
            byte[] decoded = PercentEncoding.Decode(encoded.AsSpan(), mode);

            CollectionAssert.AreEqual(original, decoded, $"Round trip failed for length={length} (mode {mode}).");
        }
    }

    /// <summary>
    /// Verifies that the span-based try-path round-trips through pre-sized buffers.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenSpanTryPath_ShouldRecoverOriginal()
    {
        byte[] original = { 0x00, 0x20, 0x2B, 0x25, 0x2F, 0xFF, 0x41 };
        char[] charBuffer = new char[PercentEncoding.GetMaxEncodedLength(original.Length)];

        bool encOk = PercentEncoding.TryEncode(original, charBuffer, out int charsWritten, PercentEncodingMode.FormUrlEncoded);
        byte[] byteBuffer = new byte[PercentEncoding.GetMaxDecodedLength(charsWritten)];
        bool decOk = PercentEncoding.TryDecode(charBuffer.AsSpan(0, charsWritten), byteBuffer, out int bytesWritten, PercentEncodingMode.FormUrlEncoded);

        Assert.IsTrue(encOk);
        Assert.IsTrue(decOk);
        CollectionAssert.AreEqual(original, byteBuffer.AsSpan(0, bytesWritten).ToArray());
    }
}
