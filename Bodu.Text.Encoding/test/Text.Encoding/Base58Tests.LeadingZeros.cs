// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base58Tests.LeadingZeros.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base58Tests
{

    /// <summary>
    /// Verifies that leading <c>1</c> characters decode back to leading zero bytes.
    /// </summary>
    /// <param name="leadingOneCount">The number of leading <c>1</c> characters.</param>
    [TestMethod]
    [DataRow(1)]
    [DataRow(3)]
    [DataRow(5)]
    [DataRow(8)]
    public void Decode_WhenLeadingOneCharacters_ShouldRecoverLeadingZeroBytes(int leadingOneCount)
    {
        string input = new string('1', leadingOneCount) + "2"; // '2' decodes to 0x01
        byte[] expected = new byte[leadingOneCount + 1];
        expected[leadingOneCount] = 0x01;

        byte[] actual = Base58.Decode(input);

        CollectionAssert.AreEqual(expected, actual);
    }
    /// <summary>
    /// Verifies that leading zero bytes are encoded as repeated <c>1</c> characters in the Bitcoin/Flickr alphabet.
    /// </summary>
    /// <param name="leadingZeroCount">The number of leading zero bytes.</param>
    [TestMethod]
    [DataRow(1)]
    [DataRow(3)]
    [DataRow(5)]
    [DataRow(8)]
    public void Encode_WhenLeadingZeros_ShouldEmitLeadingOneCharacters(int leadingZeroCount)
    {
        byte[] bytes = new byte[leadingZeroCount + 1];
        bytes[leadingZeroCount] = 0x01;

        string actual = Base58.Encode(bytes);

        Assert.HasCount(leadingZeroCount, actual.TakeWhile(c => c == '1'),
            $"Encoded form should start with {leadingZeroCount} '1' characters.");
        Assert.AreEqual('2', actual[leadingZeroCount], "Trailing data byte 0x01 should encode as '2'.");
    }

    /// <summary>
    /// Verifies that an all-zero input round-trips bit-perfectly and preserves length.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenAllZeroBytes_ShouldPreserveByteLength()
    {
        byte[] original = new byte[5];

        string encoded = Base58.Encode(original);
        byte[] decoded = Base58.Decode(encoded);

        Assert.AreEqual("11111", encoded);
        Assert.HasCount(5, decoded);
        CollectionAssert.AreEqual(original, decoded);
    }

}
