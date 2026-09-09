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

    /// <summary>
    /// Verifies that negative zero, which the integer form cannot preserve, is encoded through the double form and
    /// decodes back with its sign.
    /// </summary>
    [TestMethod]
    public void TryEncode_WhenNegativeZero_ShouldUseDoubleFormAndPreserveSign()
    {
        Assert.IsTrue(BiffRk.TryEncode(-0.0, out uint rk));

        Assert.AreEqual(0x80000000u, rk);
        Assert.IsTrue(double.IsNegative(BiffRk.Decode(rk)));
    }

    /// <summary>
    /// Verifies that values beyond the 30-bit integer range with a zero low mantissa use the double form.
    /// </summary>
    /// <param name="value">The number.</param>
    /// <param name="expectedRk">The expected encoding.</param>
    [TestMethod]
    [DataRow(1099511627776.0, 0x42700000u)]
    [DataRow(-1099511627776.0, 0xC2700000u)]
    public void TryEncode_WhenLargeExactDouble_ShouldUseDoubleForm(double value, uint expectedRk)
    {
        Assert.IsTrue(BiffRk.TryEncode(value, out uint rk));

        Assert.AreEqual(expectedRk, rk);
        Assert.AreEqual(value, BiffRk.Decode(rk));
    }

    /// <summary>
    /// Verifies that negative hundredths use the negative integer form with the divided-by-100 flag.
    /// </summary>
    /// <param name="value">The number.</param>
    /// <param name="expectedRk">The expected encoding.</param>
    [TestMethod]
    [DataRow(-12.34, unchecked((uint)(-1234 << 2)) | 0x03u)]
    [DataRow(-0.01, 0xFFFFFFFFu)]
    [DataRow(-5368709.12, 0x80000003u)]
    public void TryEncode_WhenNegativeHundredths_ShouldUseIntegerFormDividedByHundred(double value, uint expectedRk)
    {
        Assert.IsTrue(BiffRk.TryEncode(value, out uint rk));

        Assert.AreEqual(expectedRk, rk);
        Assert.AreEqual(value, BiffRk.Decode(rk));
    }

    /// <summary>
    /// Verifies that values just outside each form's range fall through to the next form or are rejected.
    /// </summary>
    /// <param name="value">The number.</param>
    /// <param name="expected">Whether an RK form exists.</param>
    [TestMethod]
    [DataRow(536870912.0, true)]
    [DataRow(536870913.0, false)]
    [DataRow(-536870913.0, false)]
    [DataRow(5368709.11, true)]
    [DataRow(5368709.13, false)]
    [DataRow(0.001, false)]
    [DataRow(1.005, false)]
    public void TryEncode_WhenAtFormBoundaries_ShouldReportExactly(double value, bool expected)
    {
        Assert.AreEqual(expected, BiffRk.TryEncode(value, out uint rk));
        if (expected)
            Assert.AreEqual(value, BiffRk.Decode(rk));
        else
            Assert.AreEqual(0u, rk);
    }

    /// <summary>
    /// Verifies that decoding a sample of integer-form patterns and re-encoding yields the same pattern, so the
    /// integer forms are canonical.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void TryEncode_WhenSweepingIntegerForms_ShouldBeCanonical()
    {
        for (long high = -(1 << 29); high <= (1 << 29) - 1; high += 4099)
        {
            uint rk = ((uint)(int)high << 2) | 0x02;
            double value = BiffRk.Decode(rk);

            Assert.IsTrue(BiffRk.TryEncode(value, out uint encoded));
            Assert.AreEqual(rk, encoded, $"Integer {high} did not re-encode canonically.");
        }
    }
}
