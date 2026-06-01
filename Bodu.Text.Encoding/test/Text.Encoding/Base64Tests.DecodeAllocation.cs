// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base64Tests.DecodeAllocation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base64Tests
{
    /// <summary>
    /// Verifies that <see cref="Base64.Decode(ReadOnlySpan{char}, Base64Variant, BaseFormatStyles)" /> returns a
    /// byte array of exactly the decoded byte count for inputs with one, two, or no trailing '=' padding chars.
    /// The test guards against regressions in the array-length computation when the implementation switches from
    /// max-then-trim to exact-length allocation.
    /// </summary>
    /// <param name="input">The Base64 input string.</param>
    /// <param name="expectedLength">The exact expected decoded byte count.</param>
    [TestMethod]
    [DataRow("AQ==", 1)]    // one input byte (0x01) → "AQ==" with two '=' chars
    [DataRow("AQI=", 2)]    // two input bytes (0x01, 0x02) → "AQI=" with one '=' char
    [DataRow("AQID", 3)]    // three input bytes (0x01, 0x02, 0x03) → "AQID" with no padding
    [DataRow("AQIDBA==", 4)]
    [DataRow("AQIDBAU=", 5)]
    [DataRow("AQIDBAUG", 6)]
    public void Decode_ShouldReturnArrayOfExactDecodedLength(string input, int expectedLength)
    {
        var decoded = Base64.Decode(input.AsSpan());

        Assert.AreEqual(expectedLength, decoded.Length);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.Decode(ReadOnlySpan{char}, Base64Variant, BaseFormatStyles)" /> returns a
    /// byte array of the exact decoded length for an unpadded URL-safe input — the normalizer re-pads internally,
    /// so the trim must still produce the right length.
    /// </summary>
    [TestMethod]
    public void Decode_WhenUrlSafeAndUnpadded_ShouldReturnArrayOfExactDecodedLength()
    {
        var decoded = Base64.Decode("AQ".AsSpan(), Base64Variant.UrlSafe);

        Assert.AreEqual(1, decoded.Length);
    }
}
