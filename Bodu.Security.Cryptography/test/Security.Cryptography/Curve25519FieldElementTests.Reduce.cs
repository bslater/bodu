// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Curve25519FieldElementTests.Reduce.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class Curve25519FieldElementTests
{
    /// <summary>
    /// Verifies that negating a loose subtraction result after <see cref="Curve25519FieldElement.Reduce" /> yields
    /// the correct additive inverse. Without the intermediate reduction the second subtraction underflows, which is
    /// the failure mode originally hit by Ed25519 point decompression.
    /// </summary>
    [TestMethod]
    public void Reduce_WhenNegatingLooseSubtractionResult_ShouldYieldAdditiveInverse()
    {
        var a = Curve25519FieldElement.FromBytes(
            Convert.FromHexString("d75a980182b10ab7d54bfed3c964073a0ee172f3daa62325af021a68f707511a"));

        // u = a² − 1 as a loose (unreduced) value, then −u via Reduce; u + (−u) must be zero.
        var u = Curve25519FieldElement.Subtract(
            Curve25519FieldElement.Square(a), Curve25519FieldElement.One);
        var negated = Curve25519FieldElement.Subtract(
            Curve25519FieldElement.Zero, Curve25519FieldElement.Reduce(u));

        Assert.IsTrue(Curve25519FieldElement.Reduce(Curve25519FieldElement.Add(Curve25519FieldElement.Reduce(u), negated)).IsZeroConstantTime());
    }

    /// <summary>
    /// Verifies that reducing seeded elements with limbs anywhere below 2^63 preserves their value modulo p and brings
    /// every limb below 2^52.
    /// </summary>
    [TestMethod]
    public void Reduce_WhenLimbsAreLarge_ShouldPreserveTheValueModuloP()
    {
        var random = new Random(0x2551_9003);

        for (int i = 0; i < 10_000; i++)
        {
            Curve25519FieldElement value = RandomElement(random, 1UL << 63);

            var reduced = Curve25519FieldElement.Reduce(value);

            if (ValueModPrime(reduced) != ValueModPrime(value) || !IsLooselyReduced(reduced))
                Assert.Fail($"{ToBigInteger(value):X} reduced to {ToBigInteger(reduced):X}.");
        }
    }
}
