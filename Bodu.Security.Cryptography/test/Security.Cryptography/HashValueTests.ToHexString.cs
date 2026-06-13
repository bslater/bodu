// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashValueTests.ToHexString.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography;

public sealed partial class HashValueTests
{
    /// <summary>
    /// Verifies that <see cref="HashValue.ToHexString" /> formats every byte as lowercase hexadecimal.
    /// </summary>
    [TestMethod]
    public void ToHexString_WhenValueIsNonEmpty_ShouldProduceLowercaseHex()
    {
        var hash = HashValue.FromBytes([0xDE, 0xAD, 0xBE, 0xEF, 0x00, 0x0F]);

        Assert.AreEqual("deadbeef000f", hash.ToHexString());
    }

    /// <summary>
    /// Verifies that <see cref="HashValue.ToHexString" /> returns an empty string for the empty value.
    /// </summary>
    [TestMethod]
    public void ToHexString_WhenValueIsEmpty_ShouldReturnEmptyString()
    {
        Assert.AreEqual(string.Empty, default(HashValue).ToHexString());
    }

    /// <summary>
    /// Verifies that formatting with <see cref="HashValue.ToHexString" /> and re-parsing with
    /// <see cref="HashValue.ParseHex" /> round-trips the value.
    /// </summary>
    /// <param name="kat">The hexadecimal vector under test.</param>
    [TestMethod]
    [DynamicData(nameof(ValidHexVectors), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ToHexString_WhenRoundTrippedThroughParseHex_ShouldPreserveValue(ValidKat<string, byte[]> kat)
    {
        var original = HashValue.FromBytes(kat.Expected);

        var roundTripped = HashValue.ParseHex(original.ToHexString());

        Assert.AreEqual(original, roundTripped);
    }

    /// <summary>
    /// Verifies that <see cref="HashValue.ToString" /> returns the same lowercase hexadecimal text as
    /// <see cref="HashValue.ToHexString" />.
    /// </summary>
    [TestMethod]
    public void ToString_WhenValueIsNonEmpty_ShouldMatchToHexString()
    {
        var hash = HashValue.FromBytes([0x01, 0xFF]);

        Assert.AreEqual(hash.ToHexString(), hash.ToString());
    }
}
