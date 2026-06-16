// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Curve25519FieldElementTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for the <see cref="Curve25519FieldElement" /> GF(2^255 − 19) arithmetic struct.
/// </summary>
[TestClass]
public class Curve25519FieldElementTests
{
    /// <summary>
    /// The canonical little-endian encoding of the field prime p = 2^255 − 19.
    /// </summary>
    private const string PrimeHex = "edffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f";

    // ── FromBytes / ToBytes ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that a canonical encoding survives a <see cref="Curve25519FieldElement.FromBytes" /> /
    /// <see cref="Curve25519FieldElement.ToBytes" /> round-trip unchanged.
    /// </summary>
    [TestMethod]
    public void ToBytes_WhenValueIsCanonical_ShouldRoundTripFromBytes()
    {
        byte[] encoded = Convert.FromHexString("4a5d9d5ba4ce2de1728e3bf480350f25e07e21c947d19e3376f09b3c1e161742");

        var element = Curve25519FieldElement.FromBytes(encoded);
        byte[] roundTrip = new byte[Curve25519FieldElement.EncodedSizeInBytes];
        element.ToBytes(roundTrip);

        CollectionAssert.AreEqual(encoded, roundTrip);
    }

    /// <summary>
    /// Verifies that <see cref="Curve25519FieldElement.FromBytes" /> ignores bit 255 of the encoding, so two inputs
    /// differing only in the top bit decode to the same element.
    /// </summary>
    [TestMethod]
    public void FromBytes_WhenBit255IsSet_ShouldDecodeSameValueAsWithBitClear()
    {
        byte[] low = Convert.FromHexString("0200000000000000000000000000000000000000000000000000000000000000");
        byte[] high = (byte[])low.Clone();
        high[31] |= 0x80;

        byte[] fromLow = new byte[Curve25519FieldElement.EncodedSizeInBytes];
        byte[] fromHigh = new byte[Curve25519FieldElement.EncodedSizeInBytes];
        Curve25519FieldElement.FromBytes(low).ToBytes(fromLow);
        Curve25519FieldElement.FromBytes(high).ToBytes(fromHigh);

        CollectionAssert.AreEqual(fromLow, fromHigh);
    }

    /// <summary>
    /// Verifies that <see cref="Curve25519FieldElement.ToBytes" /> reduces non-canonical encodings of p, p + 1, and
    /// 2^255 − 1 to their canonical representatives 0, 1, and 18.
    /// </summary>
    [TestMethod]
    [DataRow("p reduces to zero", PrimeHex, "0000000000000000000000000000000000000000000000000000000000000000")]
    [DataRow("p+1 reduces to one", "eeffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f", "0100000000000000000000000000000000000000000000000000000000000000")]
    [DataRow("2^255-1 reduces to 18", "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f", "1200000000000000000000000000000000000000000000000000000000000000")]
    public void ToBytes_WhenValueIsNonCanonical_ShouldProduceCanonicalRepresentative(string testName, string inputHex, string expectedHex)
    {
        _ = testName;

        var element = Curve25519FieldElement.FromBytes(Convert.FromHexString(inputHex));
        byte[] actual = new byte[Curve25519FieldElement.EncodedSizeInBytes];
        element.ToBytes(actual);

        CollectionAssert.AreEqual(Convert.FromHexString(expectedHex), actual);
    }

    /// <summary>
    /// Verifies that <see cref="Curve25519FieldElement.FromBytes" /> throws <see cref="ArgumentException" /> when the
    /// source span is not exactly 32 bytes.
    /// </summary>
    [TestMethod]
    public void FromBytes_WhenSourceLengthIsInvalid_ShouldThrowArgumentException()
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Curve25519FieldElement.FromBytes(new byte[31]);
        });

        Assert.IsNotNull(ex);
    }

    // ── Arithmetic identities ─────────────────────────────────────────────────────────────────

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
    /// Verifies that multiplying an element by its <see cref="Curve25519FieldElement.Invert" /> result yields one.
    /// </summary>
    [TestMethod]
    public void Invert_WhenMultipliedByOriginal_ShouldYieldOne()
    {
        var element = Curve25519FieldElement.FromBytes(
            Convert.FromHexString("a546e36bf0527c9d3b16154b82465edd62144c0ac1fc5a18506a2244ba449ac4"));

        var product = Curve25519FieldElement.Multiply(element, Curve25519FieldElement.Invert(element));

        CollectionAssert.AreEqual(ToArray(Curve25519FieldElement.One), ToArray(product));
    }

    /// <summary>
    /// Verifies that subtracting an addend from a sum recovers the original element, exercising the 4p bias used by
    /// <see cref="Curve25519FieldElement.Subtract" />.
    /// </summary>
    [TestMethod]
    public void Subtract_WhenSubtractingPriorAddend_ShouldRecoverOriginalValue()
    {
        byte[] encodedA = Convert.FromHexString("4b66e9d4d1b4673c5ad22691957d6af5c11b6421e0ea01d42ca4169e7918ba0d");
        byte[] encodedB = Convert.FromHexString("e5210f12786811d3f4b7959d0538ae2c31dbe7106fc03c3efc4cd549c715a413");
        var a = Curve25519FieldElement.FromBytes(encodedA);
        var b = Curve25519FieldElement.FromBytes(encodedB);

        var recovered = Curve25519FieldElement.Subtract(Curve25519FieldElement.Add(a, b), b);

        CollectionAssert.AreEqual(encodedA, ToArray(recovered));
    }

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
    /// Verifies the fixed-exponent identity (z^(2^252 − 3))^8 · z^7 = z^(p + 2) = z^3, which exercises the
    /// <see cref="Curve25519FieldElement.Pow22523" /> addition chain end to end via Fermat's little theorem.
    /// </summary>
    [TestMethod]
    public void Pow22523_WhenRaisedToEighthTimesZSeventh_ShouldEqualZCubed()
    {
        var z = Curve25519FieldElement.FromBytes(
            Convert.FromHexString("de9edb7d7b7dc1b4d35b61c2ece435373f8343c85b78674dadfc7e146f882b4f"));

        // left = (z^(2^252-3))^8 * z^7 = z^(2^255-24+7) = z^(2^255-17) = z^((2^255-19)+2) = z^(p+2).
        var x = Curve25519FieldElement.Pow22523(z);
        var x8 = Curve25519FieldElement.Square(Curve25519FieldElement.Square(Curve25519FieldElement.Square(x)));
        var z2 = Curve25519FieldElement.Square(z);
        var z3 = Curve25519FieldElement.Multiply(z2, z);
        var z4 = Curve25519FieldElement.Square(z2);
        var z7 = Curve25519FieldElement.Multiply(z4, z3);
        var left = Curve25519FieldElement.Multiply(x8, z7);

        // By Fermat, z^p = z, so z^(p+2) = z^3.
        CollectionAssert.AreEqual(ToArray(z3), ToArray(left));
    }

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

    // ── Constant-time helpers ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="Curve25519FieldElement.ConditionalSwap" /> swaps both elements when the condition is
    /// 1 and leaves them untouched when the condition is 0.
    /// </summary>
    [TestMethod]
    public void ConditionalSwap_WhenConditionIsOneOrZero_ShouldSwapOrPreserveOperands()
    {
        var a = new Curve25519FieldElement(1, 2, 3, 4, 5);
        var b = new Curve25519FieldElement(6, 7, 8, 9, 10);

        Curve25519FieldElement.ConditionalSwap(ref a, ref b, 0);
        Assert.AreEqual(1UL, a.L0);
        Assert.AreEqual(6UL, b.L0);

        Curve25519FieldElement.ConditionalSwap(ref a, ref b, 1);
        Assert.AreEqual(6UL, a.L0);
        Assert.AreEqual(10UL, a.L4);
        Assert.AreEqual(1UL, b.L0);
        Assert.AreEqual(5UL, b.L4);
    }

    /// <summary>
    /// Verifies that <see cref="Curve25519FieldElement.ConditionalMove" /> copies the source when the condition is 1
    /// and preserves the destination when the condition is 0.
    /// </summary>
    [TestMethod]
    public void ConditionalMove_WhenConditionIsOneOrZero_ShouldCopyOrPreserveDestination()
    {
        var destination = new Curve25519FieldElement(1, 2, 3, 4, 5);
        var source = new Curve25519FieldElement(6, 7, 8, 9, 10);

        Curve25519FieldElement.ConditionalMove(ref destination, source, 0);
        Assert.AreEqual(1UL, destination.L0);

        Curve25519FieldElement.ConditionalMove(ref destination, source, 1);
        Assert.AreEqual(6UL, destination.L0);
        Assert.AreEqual(10UL, destination.L4);
    }

    /// <summary>
    /// Verifies that <see cref="Curve25519FieldElement.IsNegative" /> reports the least significant bit of the
    /// canonical encoding.
    /// </summary>
    [TestMethod]
    public void IsNegative_WhenCanonicalValueIsOddOrEven_ShouldReturnLsb()
    {
        Assert.IsTrue(Curve25519FieldElement.One.IsNegative());
        Assert.IsFalse(new Curve25519FieldElement(2, 0, 0, 0, 0).IsNegative());
        Assert.IsFalse(Curve25519FieldElement.Zero.IsNegative());
    }

    /// <summary>
    /// Verifies that <see cref="Curve25519FieldElement.IsZeroConstantTime" /> recognises both the trivial zero and
    /// the non-canonical encoding of p as zero, and rejects a non-zero element.
    /// </summary>
    [TestMethod]
    public void IsZeroConstantTime_WhenValueIsZeroModP_ShouldReturnTrue()
    {
        Assert.IsTrue(Curve25519FieldElement.Zero.IsZeroConstantTime());
        Assert.IsTrue(Curve25519FieldElement.FromBytes(Convert.FromHexString(PrimeHex)).IsZeroConstantTime());
        Assert.IsFalse(Curve25519FieldElement.One.IsZeroConstantTime());
    }

    /// <summary>
    /// Serializes a field element to its canonical 32-byte encoding for assertion comparisons.
    /// </summary>
    /// <param name="element">The element to serialize.</param>
    /// <returns>The canonical little-endian encoding.</returns>
    private static byte[] ToArray(in Curve25519FieldElement element)
    {
        byte[] encoded = new byte[Curve25519FieldElement.EncodedSizeInBytes];
        element.ToBytes(encoded);

        return encoded;
    }
}
