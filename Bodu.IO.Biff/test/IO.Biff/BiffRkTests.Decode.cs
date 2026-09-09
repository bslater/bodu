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
}
