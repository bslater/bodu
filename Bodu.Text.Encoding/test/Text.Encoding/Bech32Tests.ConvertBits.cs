// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Bech32Tests.ConvertBits.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Bech32Tests
{
    /// <summary>
    /// Verifies that <see cref="Bech32.ConvertBits(ReadOnlySpan{byte}, int, int, bool)" /> repacks a single byte into
    /// the expected zero-padded 5-bit groups.
    /// </summary>
    [TestMethod]
    public void ConvertBits_When8To5WithPadding_ShouldProduceExpectedGroups()
    {
        byte[] result = Bech32.ConvertBits([0xFF], 8, 5, pad: true)!;

        CollectionAssert.AreEqual(new byte[] { 31, 28 }, result);
    }

    /// <summary>
    /// Verifies that an 8-bit payload round-trips through 5-bit groups and back without loss.
    /// </summary>
    [TestMethod]
    public void ConvertBits_When8To5To8_ShouldRoundTrip()
    {
        byte[] payload = [0x00, 0x7F, 0x80, 0xAB, 0xCD, 0xEF];

        byte[] groups = Bech32.ConvertBits(payload, 8, 5, pad: true)!;
        byte[]? restored = Bech32.ConvertBits(groups, 5, 8, pad: false);

        CollectionAssert.AreEqual(payload, restored);
    }

    /// <summary>
    /// Verifies that an input value exceeding the source bit width causes <see cref="Bech32.ConvertBits" /> to return
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ConvertBits_WhenValueExceedsFromBits_ShouldReturnNull()
    {
        Assert.IsNull(Bech32.ConvertBits([32], 5, 8, pad: false));
    }

    /// <summary>
    /// Verifies that a 5-bit sequence with a non-zero residue fails to repack into bytes when padding is disallowed.
    /// </summary>
    [TestMethod]
    public void ConvertBits_When5To8WithNonZeroResidue_ShouldReturnNull()
    {
        Assert.IsNull(Bech32.ConvertBits([31, 29], 5, 8, pad: false));
    }

    /// <summary>
    /// Verifies that <see cref="Bech32.ConvertBits" /> rejects a source bit width outside the range 1 to 8.
    /// </summary>
    [TestMethod]
    public void ConvertBits_WhenFromBitsOutOfRange_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Bech32.ConvertBits([0x00], 0, 5, pad: true);
        });
    }
}
