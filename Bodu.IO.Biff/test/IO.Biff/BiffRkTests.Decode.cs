// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffRkTests.Decode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

public sealed partial class BiffRkTests
{
    /// <summary>
    /// Verifies that each RK form decodes to the number it encodes, using the values published in the Excel file
    /// format documentation.
    /// </summary>
    /// <param name="rk">The RK value.</param>
    /// <param name="expected">The expected number.</param>
    [TestMethod]
    [DataRow(0x3FF00000u, 1.0)]
    [DataRow(0x00000002u, 0.0)]
    [DataRow(0x00000006u, 1.0)]
    [DataRow(0x00000007u, 0.01)]
    [DataRow(0xFFFFFFFEu, -1.0)]
    [DataRow(0xBFF00000u, -1.0)]
    [DataRow(0x40240000u, 10.0)]
    [DataRow(0x40240001u, 0.1)]
    [DataRow(0x7FFFFFFEu, 536870911.0)]
    [DataRow(0x80000002u, -536870912.0)]
    public void Decode_WhenKnownValue_ShouldReturnExpected(uint rk, double expected)
    {
        Assert.AreEqual(expected, BiffRk.Decode(rk), 1e-12);
    }

    /// <summary>
    /// Verifies that the double form with only the sign bit decodes to negative zero and that the divided-by-100
    /// integer zero decodes to positive zero.
    /// </summary>
    [TestMethod]
    public void Decode_WhenZeroForms_ShouldDecodeSign()
    {
        Assert.IsTrue(double.IsNegative(BiffRk.Decode(0x80000000u)));
        Assert.AreEqual(0.0, BiffRk.Decode(0x00000003u));
        Assert.IsFalse(double.IsNegative(BiffRk.Decode(0x00000003u)));
        Assert.AreEqual(0.0, BiffRk.Decode(0x00000000u));
    }

    /// <summary>
    /// Verifies that the low two bits never contribute to the double form's mantissa.
    /// </summary>
    [TestMethod]
    public void Decode_WhenDoubleFormHasFlagBits_ShouldMaskThemFromMantissa()
    {
        Assert.AreEqual(BiffRk.Decode(0x3FF00000u), BiffRk.Decode(0x3FF00001u) * 100.0, 1e-12);
        Assert.AreEqual(1.0, BiffRk.Decode(0x3FF00000u));
    }

    /// <summary>
    /// Verifies that the double form can express NaN and infinity bit patterns.
    /// </summary>
    [TestMethod]
    public void Decode_WhenDoubleFormIsNonFinite_ShouldDecodeNonFinite()
    {
        Assert.IsTrue(double.IsPositiveInfinity(BiffRk.Decode(0x7FF00000u)));
        Assert.IsTrue(double.IsNegativeInfinity(BiffRk.Decode(0xFFF00000u)));
        Assert.IsTrue(double.IsNaN(BiffRk.Decode(0x7FF80000u)));
    }
}
