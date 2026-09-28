// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GhashTests.MultiplyByX.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class GhashTests
{
    /// <summary>
    /// Verifies that <see cref="Ghash.MultiplyByX" /> reproduces RFC 8452 Appendix A's <c>mulX_GHASH</c> examples,
    /// including the POLYVAL-to-GHASH key conversion of the worked example.
    /// </summary>
    /// <param name="input">The element, in hexadecimal.</param>
    /// <param name="expected">The published product, in hexadecimal.</param>
    [TestMethod]
    [DataRow("01000000000000000000000000000000", "00800000000000000000000000000000")]
    [DataRow("9c98c04df9387ded828175a92ba652d8", "4e4c6026fc9c3ef6c140bad495d3296c")]
    [DataRow("7b754bba26f8311d7642925847936225", "dcbaa5dd137c188ebb21492c23c9b112")]
    public void MultiplyByX_WhenGivenRfc8452Example_ShouldMatchPublishedValue(string input, string expected)
    {
        byte[] x = Convert.FromHexString(input);
        byte[] result = new byte[16];

        Ghash.MultiplyByX(x, result);

        Assert.AreEqual(expected, Convert.ToHexString(result).ToLowerInvariant());
    }

    /// <summary>
    /// Verifies that the product may be written over its input.
    /// </summary>
    [TestMethod]
    public void MultiplyByX_WhenResultIsTheInput_ShouldWriteTheProductInPlace()
    {
        byte[] x = Convert.FromHexString("9c98c04df9387ded828175a92ba652d8");

        Ghash.MultiplyByX(x, x);

        Assert.AreEqual("4e4c6026fc9c3ef6c140bad495d3296c", Convert.ToHexString(x).ToLowerInvariant());
    }
}
