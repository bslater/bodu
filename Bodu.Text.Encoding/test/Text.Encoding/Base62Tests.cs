// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base62Tests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Behavioural tests for <see cref="Base62" /> (GMP-style <c>0-9 A-Z a-z</c> alphabet). Partial files split coverage by
/// member or subject contract per the repository test convention. The cross-encoding round-trip and known-answer
/// surface is exercised by <see cref="Bodu.Text.Encoding.Contracts.Base62ContractTests" />.
/// </summary>
[TestClass]
public sealed partial class Base62Tests
{
    /// <summary>
    /// Verifies that the alphabet orders digits, then upper-case, then lower-case letters — distinguishing the GMP
    /// convention from the alternative <c>A-Z a-z 0-9</c> ordering.
    /// </summary>
    /// <param name="value">The single-byte value.</param>
    /// <param name="expected">The expected single-character encoding.</param>
    [TestMethod]
    [DataRow(0, "0")]
    [DataRow(9, "9")]
    [DataRow(10, "A")]
    [DataRow(35, "Z")]
    [DataRow(36, "a")]
    [DataRow(61, "z")]
    public void Encode_WhenSingleByteValue_ShouldFollowGmpOrdering(int value, string expected)
    {
        Assert.AreEqual(expected, Base62.Encode(new byte[] { (byte)value }));
    }

    /// <summary>
    /// Verifies that leading zero bytes encode as leading <c>0</c> characters and are recovered on decode.
    /// </summary>
    [TestMethod]
    public void EncodeDecode_WhenLeadingZeroBytes_ShouldPreserveCount()
    {
        byte[] payload = [0x00, 0x00, 0x00, 0x2A];

        string encoded = Base62.Encode(payload);

        Assert.IsTrue(encoded.StartsWith("000", StringComparison.Ordinal));
        CollectionAssert.AreEqual(payload, Base62.Decode(encoded));
    }

    /// <summary>
    /// Verifies that an ASCII payload round-trips through encode and decode without loss.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenAsciiPayload_ShouldRecoverBytes()
    {
        byte[] payload = "Hello, Base62!"u8.ToArray();

        CollectionAssert.AreEqual(payload, Base62.Decode(Base62.Encode(payload)));
    }

    /// <summary>
    /// Verifies that decoding empty input yields an empty array and encoding empty input yields an empty string.
    /// </summary>
    [TestMethod]
    public void EncodeDecode_WhenEmpty_ShouldReturnEmpty()
    {
        Assert.AreEqual(string.Empty, Base62.Encode(Array.Empty<byte>()));
        Assert.AreEqual(0, Base62.Decode(string.Empty).Length);
    }

    /// <summary>
    /// Verifies that the <see cref="BinaryEncodings.Base62" /> singleton round-trips through the unified
    /// <see cref="IBinaryEncoding" /> surface and resolves by name.
    /// </summary>
    [TestMethod]
    public void BinaryEncodingsBase62_WhenRoundTrippedAndResolved_ShouldMatch()
    {
        IBinaryEncoding encoding = BinaryEncodings.Get("base62");
        byte[] payload = [0x00, 0xDE, 0xAD, 0xBE, 0xEF];

        Assert.AreEqual("base62", encoding.Name);
        CollectionAssert.AreEqual(payload, encoding.Decode(encoding.Encode(payload)));
    }
}
