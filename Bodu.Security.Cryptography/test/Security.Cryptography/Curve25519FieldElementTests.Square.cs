// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Curve25519FieldElementTests.Square.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Security.Cryptography;

public partial class Curve25519FieldElementTests
{
    /// <summary>
    /// Verifies that squaring elements whose limbs sit at and around the operand bound, the limb width, and the prime
    /// yields their square modulo p.
    /// </summary>
    [TestMethod]
    public void Square_WhenLimbsAreAtTheirBounds_ShouldMatchModularArithmetic()
    {
        foreach (Curve25519FieldElement value in EdgeElements())
        {
            var square = Curve25519FieldElement.Square(value);

            BigInteger expected = BigInteger.Pow(ToBigInteger(value), 2) % s_prime;
            if (ValueModPrime(square) != expected)
                Assert.Fail($"{ToBigInteger(value):X} squared gave {ValueModPrime(square):X}, not {expected:X}.");
        }
    }

    /// <summary>
    /// Verifies that squaring elements whose limbs sit at and around their bounds returns a loosely reduced square,
    /// every limb below 2^52, which further arithmetic relies on.
    /// </summary>
    [TestMethod]
    public void Square_WhenLimbsAreAtTheirBounds_ShouldReturnLooselyReducedLimbs()
    {
        foreach (Curve25519FieldElement value in EdgeElements())
        {
            var square = Curve25519FieldElement.Square(value);

            if (!IsLooselyReduced(square))
                Assert.Fail($"{ToBigInteger(value):X} squared left a limb at or above 2^52.");
        }
    }

    /// <summary>
    /// Verifies that the dedicated square of seeded elements, with limbs anywhere below the operand bound, is limb for
    /// limb the element multiplied by itself.
    /// </summary>
    [TestMethod]
    public void Square_WhenLimbsAreRandomWithinTheirBounds_ShouldMatchMultiply()
    {
        var random = new Random(0x2551_9002);

        for (int i = 0; i < 10_000; i++)
        {
            Curve25519FieldElement value = RandomElement(random, OperandLimbBound);

            var square = Curve25519FieldElement.Square(value);
            var product = Curve25519FieldElement.Multiply(value, value);

            if (ValueModPrime(square) != ValueModPrime(product) || !IsLooselyReduced(square))
                Assert.Fail($"{ToBigInteger(value):X} squared gave {ToBigInteger(square):X}, not {ToBigInteger(product):X} loosely reduced.");
        }
    }
}
