// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffRkTests.TryEncode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

public sealed partial class BiffRkTests
{
    /// <summary>
    /// Verifies that a value with an RK form encodes to a value that decodes back exactly.
    /// </summary>
    /// <param name="value">The number.</param>
    /// <param name="expectedRk">The expected RK encoding.</param>
    [TestMethod]
    [DataRow(0.0, 0x00000002u)]
    [DataRow(1.0, 0x00000006u)]
    [DataRow(-1.0, 0xFFFFFFFEu)]
    [DataRow(536870911.0, 0x7FFFFFFEu)]
    [DataRow(-536870912.0, 0x80000002u)]
    [DataRow(0.01, 0x00000007u)]
    [DataRow(12.34, (1234u << 2) | 0x03)]
    [DataRow(0.1, (10u << 2) | 0x03)]
    [DataRow(0.03, (3u << 2) | 0x03)]
    [DataRow(536870912.0, 0x41C00000u)]
    [DataRow(0.015, 0x3FF80001u)]
    [DataRow(1.5, 0x3FF80000u)]
    [DataRow(0.5, 0x3FE00000u)]
    public void TryEncode_WhenValueHasRkForm_ShouldEncodeAndRoundTrip(double value, uint expectedRk)
    {
        Assert.IsTrue(BiffRk.TryEncode(value, out uint rk));
        Assert.AreEqual(expectedRk, rk);
        Assert.AreEqual(value, BiffRk.Decode(rk));
    }

    /// <summary>
    /// Verifies that a value with no exact RK form is reported as such.
    /// </summary>
    /// <param name="value">The number.</param>
    [TestMethod]
    [DataRow(1e9)]
    [DataRow(Math.PI)]
    [DataRow(-536870913.0)]
    [DataRow(double.NegativeInfinity)]
    [DataRow(double.NaN)]
    [DataRow(double.PositiveInfinity)]
    [DataRow(1.23456789)]
    public void TryEncode_WhenValueHasNoRkForm_ShouldReturnFalse(double value)
    {
        Assert.IsFalse(BiffRk.TryEncode(value, out uint rk));
        Assert.AreEqual(0u, rk);
    }

    /// <summary>
    /// Verifies that every encodable value in a sweep round-trips through decode.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void TryEncode_WhenSweepingValues_ShouldRoundTripEveryEncodableValue()
    {
        int encoded = 0;
        for (int i = -100000; i <= 100000; i++)
        {
            double value = i / 100.0;
            if (BiffRk.TryEncode(value, out uint rk))
            {
                encoded++;
                Assert.AreEqual(value, BiffRk.Decode(rk), $"Value {value} did not round-trip.");
            }
        }

        Assert.AreEqual(200001, encoded, "Every hundredth in the sweep has an RK form.");
    }
}
