// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base58CheckTests.GetMaxDecodedLength.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Verifies <see cref="Base58Check.GetMaxDecodedLength(int)" />, the documented upper bound on the payload byte count.
/// </summary>
public sealed partial class Base58CheckTests
{
    /// <summary>
    /// Verifies that <see cref="Base58Check.GetMaxDecodedLength(int)" /> is at least the length of a zero-filled
    /// payload, whose zero bytes each encode to one character, for every variant and every payload length to 64.
    /// </summary>
    [TestMethod]
    public void GetMaxDecodedLength_WhenPayloadIsZeroFilled_ShouldBoundThePayloadLength()
    {
        foreach (Base58Variant variant in Enum.GetValues<Base58Variant>())
        {
            for (int payloadLength = 0; payloadLength <= 64; payloadLength++)
            {
                string encoded = Base58Check.Encode(new byte[payloadLength], variant);

                Assert.IsGreaterThanOrEqualTo(payloadLength, Base58Check.GetMaxDecodedLength(encoded.Length), $"{variant} with a {payloadLength}-byte payload.");
            }
        }
    }

    /// <summary>
    /// Verifies that <see cref="Base58Check.GetMaxDecodedLength(int)" /> returns the count less the four checksum
    /// bytes for <see cref="int.MaxValue" /> characters, rather than overflowing.
    /// </summary>
    [TestMethod]
    public void GetMaxDecodedLength_WhenCountIsIntMaxValue_ShouldReturnTheCountLessTheChecksum()
    {
        int actual = Base58Check.GetMaxDecodedLength(int.MaxValue);

        Assert.AreEqual(int.MaxValue - 4, actual);
    }
}
