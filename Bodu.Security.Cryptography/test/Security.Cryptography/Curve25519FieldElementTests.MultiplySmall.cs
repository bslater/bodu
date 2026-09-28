// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Curve25519FieldElementTests.MultiplySmall.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Security.Cryptography;

public partial class Curve25519FieldElementTests
{
    /// <summary>
    /// Verifies that <see cref="Curve25519FieldElement.MultiplySmall" /> matches a full multiplication by the same
    /// constant.
    /// </summary>
    [TestMethod]
    public void MultiplySmall_WhenComparedToFullMultiply_ShouldProduceSameResult()
    {
        var element = Curve25519FieldElement.FromBytes(
            Convert.FromHexString("8520f0098930a754748b7ddcb43ef75a0dbf3a0d26381af4eba4a98eaa9b4e6a"));
        var factor = new Curve25519FieldElement(121665, 0, 0, 0, 0);

        var viaSmall = Curve25519FieldElement.MultiplySmall(element, 121665);
        var viaFull = Curve25519FieldElement.Multiply(element, factor);

        CollectionAssert.AreEqual(ToArray(viaFull), ToArray(viaSmall));
    }

    /// <summary>
    /// Verifies that scaling elements whose limbs sit at and around their bounds by the ladder constant and by the
    /// largest factor yields their product modulo p, loosely reduced.
    /// </summary>
    /// <param name="factor">The factor.</param>
    [TestMethod]
    [DataRow(121665U)]
    [DataRow(uint.MaxValue)]
    public void MultiplySmall_WhenLimbsAreAtTheirBounds_ShouldMatchModularArithmetic(uint factor)
    {
        foreach (Curve25519FieldElement value in EdgeElements())
        {
            var product = Curve25519FieldElement.MultiplySmall(value, factor);

            BigInteger expected = ToBigInteger(value) * factor % s_prime;
            if (ValueModPrime(product) != expected || !IsLooselyReduced(product))
                Assert.Fail($"{ToBigInteger(value):X} × {factor} gave {ToBigInteger(product):X}, not {expected:X} loosely reduced.");
        }
    }
}
