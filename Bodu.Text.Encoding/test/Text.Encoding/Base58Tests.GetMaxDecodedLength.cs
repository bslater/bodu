// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base58Tests.GetMaxDecodedLength.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Verifies <see cref="Base58.GetMaxDecodedLength(int)" />, the documented upper bound on the decoded byte count.
/// </summary>
public sealed partial class Base58Tests
{
    /// <summary>
    /// Verifies that <see cref="Base58.GetMaxDecodedLength(int)" /> is at least the decoded length of input made only
    /// of the alphabet's first character, which decodes to one zero byte per character, for every variant and at every
    /// length to 64.
    /// </summary>
    [TestMethod]
    public void GetMaxDecodedLength_WhenEveryCharacterIsALeadingZero_ShouldBoundTheDecodedLength()
    {
        foreach (Base58Variant variant in Enum.GetValues<Base58Variant>())
        {
            for (int length = 1; length <= 64; length++)
            {
                byte[] decoded = Base58.Decode(LeadingZeroCharacters(variant, length), variant);

                Assert.IsGreaterThanOrEqualTo(decoded.Length, Base58.GetMaxDecodedLength(length), $"{variant} at length {length}.");
            }
        }
    }

    /// <summary>
    /// Verifies that <see cref="Base58.GetMaxDecodedLength(int)" /> returns <see cref="int.MaxValue" /> for
    /// <see cref="int.MaxValue" /> characters, since no character decodes to more than one byte, rather than
    /// overflowing.
    /// </summary>
    [TestMethod]
    public void GetMaxDecodedLength_WhenCountIsIntMaxValue_ShouldReturnTheCount()
    {
        int actual = Base58.GetMaxDecodedLength(int.MaxValue);

        Assert.AreEqual(int.MaxValue, actual);
    }
}
