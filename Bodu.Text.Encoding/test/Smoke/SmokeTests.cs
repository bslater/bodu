// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SmokeTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Encoding;

namespace Bodu.Smoke;

/// <summary>
/// Provides smoke-tier coverage for the primary public types in <c>Bodu.Text.Encoding</c>. Each test exercises one
/// happy-path entry point so that catastrophic breakage is caught by the smallest possible build run.
/// </summary>
[TestClass]
public sealed class SmokeTests
{

    /// <summary>
    /// Verifies that <see cref="Base16.Encode(byte[], BaseFormattingOptions)" /> followed by
    /// <see cref="Base16.Decode(string, BaseFormatStyles)" /> round-trips the canonical reference input.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Base16_EncodeDecode_ShouldRoundTripCanonicalReference()
    {
        var original = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };

        var encoded = Base16.Encode(original);
        var decoded = Base16.Decode(encoded);

        Assert.AreEqual("deadbeef", encoded);
        CollectionAssert.AreEqual(original, decoded);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.Encode(byte[], Base32Variant, BaseFormattingOptions)" /> followed by
    /// <see cref="Base32.Decode(string, Base32Variant, BaseFormatStyles)" /> reproduces the RFC 4648 §10 reference
    /// vector for the input <c>"foobar"</c>.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Base32_EncodeDecode_ShouldMatchRfc4648ReferenceVector()
    {
        var original = System.Text.Encoding.ASCII.GetBytes("foobar");

        var encoded = Base32.Encode(original);
        var decoded = Base32.Decode(encoded);

        Assert.AreEqual("MZXW6YTBOI======", encoded);
        CollectionAssert.AreEqual(original, decoded);
    }

    /// <summary>
    /// Verifies that <see cref="Base58.Encode(byte[], Base58Variant)" /> followed by
    /// <see cref="Base58.Decode(string, Base58Variant, BaseFormatStyles)" /> round-trips the ASCII representation of
    /// <c>"Hello"</c> using the canonical Bitcoin/Flickr alphabet.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Base58_EncodeDecode_ShouldRoundTripBitcoinFlickrReferenceVector()
    {
        var original = System.Text.Encoding.ASCII.GetBytes("Hello");

        var encoded = Base58.Encode(original);
        var decoded = Base58.Decode(encoded);

        Assert.AreEqual("9Ajdvzr", encoded);
        CollectionAssert.AreEqual(original, decoded);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.Encode(byte[], Base64Variant, BaseFormattingOptions)" /> followed by
    /// <see cref="Base64.Decode(string, Base64Variant, BaseFormatStyles)" /> reproduces the RFC 4648 §10 reference
    /// vector for the input <c>"foobar"</c>.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Base64_EncodeDecode_ShouldMatchRfc4648ReferenceVector()
    {
        var original = System.Text.Encoding.ASCII.GetBytes("foobar");

        var encoded = Base64.Encode(original);
        var decoded = Base64.Decode(encoded);

        Assert.AreEqual("Zm9vYmFy", encoded);
        CollectionAssert.AreEqual(original, decoded);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.Encode(byte[], Base85Variant)" /> uses the Ascii85 <c>z</c> shortcut for an
    /// all-zero 4-byte input and that <see cref="Base85.Decode(string, Base85Variant, BaseFormatStyles)" /> reverses
    /// it.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Base85_EncodeDecode_ShouldHandleZShortcut()
    {
        var original = new byte[] { 0, 0, 0, 0 };

        var encoded = Base85.Encode(original);
        var decoded = Base85.Decode(encoded);

        Assert.AreEqual("z", encoded);
        CollectionAssert.AreEqual(original, decoded);
    }

}
