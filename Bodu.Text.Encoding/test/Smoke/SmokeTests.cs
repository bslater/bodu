// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SmokeTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
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
        byte[] original = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };

        string encoded = Base16.Encode(original);
        byte[] decoded = Base16.Decode(encoded);

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
        byte[] original = System.Text.Encoding.ASCII.GetBytes("foobar");

        string encoded = Base32.Encode(original);
        byte[] decoded = Base32.Decode(encoded);

        Assert.AreEqual("MZXW6YTBOI======", encoded);
        CollectionAssert.AreEqual(original, decoded);
    }
}
