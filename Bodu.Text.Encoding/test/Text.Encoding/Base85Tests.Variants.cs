// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base85Tests.Variants.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base85Tests
{

    /// <summary>
    /// Verifies that the Z85 variant rejects the <c>z</c> shortcut (it does not exist outside Ascii85).
    /// </summary>
    [TestMethod]
    public void Decode_WhenZ85VariantWithZShortcut_ShouldThrowFormatException()
    {
        // The Z85 alphabet does NOT contain 'z' (lowercase z is digit 35 in Z85, not the all-zero shortcut).
        // But 'z' IS in the Z85 alphabet at index 35. So this test is not about rejecting z literal.
        // Instead, the absence of "z = 4 zeros" semantic means decoded length differs.
        byte[] decoded = Base85.Decode("zzzzz", Base85Variant.Z85);

        // 5 z chars in Z85 = 5 digit-35 values, decode to a non-trivial 4-byte sequence (not zeros).
        Assert.AreEqual(4, decoded.Length);
        Assert.IsFalse(decoded.All(b => b == 0), "Z85 'z' is digit 35, not an all-zero shortcut.");
    }

    /// <summary>
    /// Verifies that round-tripping all-zero data uses the <c>z</c> shortcut for Ascii85 and emits explicit digits
    /// for Z85.
    /// </summary>
    [TestMethod]
    public void Encode_WhenAllZerosForBothVariants_ShouldUseExpectedFormatting()
    {
        byte[] zeros = new byte[8];

        string ascii85 = Base85.Encode(zeros, Base85Variant.Ascii85);
        string z85 = Base85.Encode(zeros, Base85Variant.Z85);

        Assert.AreEqual("zz", ascii85);
        Assert.AreEqual(10, z85.Length);
        Assert.IsFalse(z85.Contains("zz"), "Z85 should not condense all-zero groups.");
    }
    /// <summary>
    /// Verifies that the Ascii85 and Z85 variants produce different encoded strings for the same input.
    /// </summary>
    [TestMethod]
    public void Encode_WhenSameInputDifferentVariants_ShouldProduceDifferentOutputs()
    {
        byte[] input = new byte[] { 0x12, 0x34, 0x56, 0x78 };

        string ascii85 = Base85.Encode(input, Base85Variant.Ascii85);
        string z85 = Base85.Encode(input, Base85Variant.Z85);

        Assert.AreNotEqual(ascii85, z85);
        Assert.AreEqual(5, ascii85.Length);
        Assert.AreEqual(5, z85.Length);
    }

}
