// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base45Tests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Behavioural tests for <see cref="Base45" /> (RFC 9285). Partial files split coverage by member or subject contract
/// per the repository test convention. The cross-encoding round-trip and known-answer surface is exercised by
/// <see cref="Bodu.Text.Encoding.Contracts.Base45ContractTests" />.
/// </summary>
[TestClass]
public sealed partial class Base45Tests
{
    /// <summary>
    /// Verifies that RFC 9285 §4.3 encoding example 1 — the string <c>"AB"</c> — encodes to <c>"BB8"</c>.
    /// </summary>
    [TestMethod]
    public void Encode_WhenRfcExample1_ShouldReturnBB8()
    {
        Assert.AreEqual("BB8", Base45.Encode("AB"u8.ToArray()));
    }

    /// <summary>
    /// Verifies that RFC 9285 §4.3 encoding example 2 — the string <c>"Hello!!"</c> — encodes to
    /// <c>"%69 VD92EX0"</c>, including the embedded space symbol.
    /// </summary>
    [TestMethod]
    public void Encode_WhenRfcExample2_ShouldReturnExpectedWithSpace()
    {
        Assert.AreEqual("%69 VD92EX0", Base45.Encode("Hello!!"u8.ToArray()));
    }

    /// <summary>
    /// Verifies that RFC 9285 §4.3 encoding example 3 — the string <c>"base-45"</c> — encodes to
    /// <c>"UJCLQE7W581"</c>.
    /// </summary>
    [TestMethod]
    public void Encode_WhenRfcExample3_ShouldReturnExpected()
    {
        Assert.AreEqual("UJCLQE7W581", Base45.Encode("base-45"u8.ToArray()));
    }

    /// <summary>
    /// Verifies that RFC 9285 §4.4 decoding example — the string <c>"QED8WEX0"</c> — decodes to the ASCII bytes for
    /// <c>"ietf!"</c>.
    /// </summary>
    [TestMethod]
    public void Decode_WhenRfcDecodingExample_ShouldReturnIetfBang()
    {
        CollectionAssert.AreEqual("ietf!"u8.ToArray(), Base45.Decode("QED8WEX0"));
    }

    /// <summary>
    /// Verifies that encoding an empty payload yields an empty string.
    /// </summary>
    [TestMethod]
    public void Encode_WhenEmpty_ShouldReturnEmptyString()
    {
        Assert.AreEqual(string.Empty, Base45.Encode(Array.Empty<byte>()));
    }

    /// <summary>
    /// Verifies that decoding an empty string yields an empty byte array.
    /// </summary>
    [TestMethod]
    public void Decode_WhenEmpty_ShouldReturnEmptyArray()
    {
        Assert.AreEqual(0, Base45.Decode(string.Empty).Length);
    }
}
