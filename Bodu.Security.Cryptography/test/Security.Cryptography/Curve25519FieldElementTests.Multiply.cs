// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Curve25519FieldElementTests.Multiply.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Security.Cryptography;

public partial class Curve25519FieldElementTests
{
    /// <summary>
    /// Verifies that multiplying an element by <see cref="Curve25519FieldElement.One" /> yields the same element.
    /// </summary>
    [TestMethod]
    public void Multiply_WhenMultipliedByOne_ShouldReturnSameValue()
    {
        byte[] encoded = Convert.FromHexString("e6db6867583030db3594c1a424b15f7c726624ec26b3353b10a903a6d0ab1c4c");
        var element = Curve25519FieldElement.FromBytes(encoded);

        var product = Curve25519FieldElement.Multiply(element, Curve25519FieldElement.One);

        CollectionAssert.AreEqual(encoded, ToArray(product));
    }

    /// <summary>
    /// Verifies that multiplying elements whose limbs sit at and around the operand bound, the limb width, and the prime
    /// yields their product modulo p.
    /// </summary>
    [TestMethod]
    public void Multiply_WhenLimbsAreAtTheirBounds_ShouldMatchModularArithmetic()
    {
        Curve25519FieldElement[] edges = EdgeElements();

        foreach (Curve25519FieldElement left in edges)
        {
            foreach (Curve25519FieldElement right in edges)
            {
                var product = Curve25519FieldElement.Multiply(left, right);

                BigInteger expected = ToBigInteger(left) * ToBigInteger(right) % s_prime;
                if (ValueModPrime(product) != expected)
                    Assert.Fail($"{ToBigInteger(left):X} × {ToBigInteger(right):X} gave {ValueModPrime(product):X}, not {expected:X}.");
            }
        }
    }

    /// <summary>
    /// Verifies that multiplying elements whose limbs sit at and around their bounds returns a loosely reduced product,
    /// every limb below 2^52, which further arithmetic relies on.
    /// </summary>
    [TestMethod]
    public void Multiply_WhenLimbsAreAtTheirBounds_ShouldReturnLooselyReducedLimbs()
    {
        Curve25519FieldElement[] edges = EdgeElements();

        foreach (Curve25519FieldElement left in edges)
        {
            foreach (Curve25519FieldElement right in edges)
            {
                var product = Curve25519FieldElement.Multiply(left, right);

                if (!IsLooselyReduced(product))
                    Assert.Fail($"{ToBigInteger(left):X} × {ToBigInteger(right):X} left a limb at or above 2^52.");
            }
        }
    }

    /// <summary>
    /// Verifies that multiplying seeded elements with limbs anywhere below the operand bound yields their product modulo
    /// p, with every limb of the result below 2^52.
    /// </summary>
    [TestMethod]
    public void Multiply_WhenLimbsAreRandomWithinTheirBounds_ShouldMatchModularArithmetic()
    {
        var random = new Random(0x2551_9001);

        for (int i = 0; i < 10_000; i++)
        {
            Curve25519FieldElement left = RandomElement(random, OperandLimbBound);
            Curve25519FieldElement right = RandomElement(random, OperandLimbBound);

            var product = Curve25519FieldElement.Multiply(left, right);

            BigInteger expected = ToBigInteger(left) * ToBigInteger(right) % s_prime;
            if (ValueModPrime(product) != expected || !IsLooselyReduced(product))
                Assert.Fail($"{ToBigInteger(left):X} × {ToBigInteger(right):X} gave {ToBigInteger(product):X}, not {expected:X} loosely reduced.");
        }
    }
}
