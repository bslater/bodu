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
}
