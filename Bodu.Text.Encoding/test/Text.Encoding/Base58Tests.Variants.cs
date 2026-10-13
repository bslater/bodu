// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base58Tests.Variants.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base58Tests
{

    /// <summary>
    /// Verifies that the Bitcoin variant rejects the visually ambiguous characters <c>0</c>, <c>O</c>,
    /// <c>I</c>, and <c>l</c>.
    /// </summary>
    /// <param name="excluded">An ambiguous character that is not in the Bitcoin alphabet.</param>
    [TestMethod]
    [DataRow('0')]
    [DataRow('O')]
    [DataRow('I')]
    [DataRow('l')]
    public void Decode_WhenBitcoinFlickrVariantWithExcludedCharacter_ShouldThrowExactly(char excluded)
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base58.Decode("2" + excluded + "2");
        });
    }

    /// <summary>
    /// Verifies that the Ripple variant decodes its own encoded form via the BitcoinFlickr alphabet would fail
    /// (alphabets diverge).
    /// </summary>
    [TestMethod]
    public void Decode_WhenRippleEncodedDecodedAsBitcoin_ShouldProduceDifferentValueOrThrow()
    {
        byte[] input = Ascii("Hello");
        string rippleEncoded = Base58.Encode(input, Base58Variant.Ripple);

        try
        {
            byte[] decodedAsBitcoin = Base58.Decode(rippleEncoded, Base58Variant.BitcoinFlickr);
            Assert.IsFalse(decodedAsBitcoin.SequenceEqual(input),
                "Decoding Ripple-encoded data as Bitcoin should not produce the original bytes.");
        }
        catch (FormatException)
        {
            // Acceptable - the Ripple alphabet may use a character that is not in Bitcoin.
        }
    }
    /// <summary>
    /// Verifies that the Bitcoin and Ripple variants produce different encoded strings for the same input
    /// (because the alphabet ordering differs).
    /// </summary>
    [TestMethod]
    public void Encode_WhenSameInputDifferentVariants_ShouldProduceDifferentOutputs()
    {
        byte[] input = Ascii("Hello");

        string bitcoin = Base58.Encode(input, Base58Variant.BitcoinFlickr);
        string ripple = Base58.Encode(input, Base58Variant.Ripple);

        Assert.AreNotEqual(bitcoin, ripple);
    }

    /// <summary>
    /// Verifies that the Flickr variant encodes with Flickr's alphabet, which puts the lower-case letters before the
    /// upper-case ones: the value 57, the alphabet's last symbol, encodes as <c>Z</c>, and the value 3392387861 as
    /// <c>6aLSHT</c>.
    /// </summary>
    /// <param name="hexInput">The input bytes, as hexadecimal.</param>
    /// <param name="expected">The expected Flickr Base58 text.</param>
    [TestMethod]
    [DataRow("39", "Z")]
    [DataRow("0039", "1Z")]
    [DataRow("09", "a")]
    [DataRow("3a", "21")]
    [DataRow("ca33bb15", "6aLSHT")]
    public void Encode_WhenFlickrVariant_ShouldUseLowerCaseFirstAlphabet(string hexInput, string expected) =>
        Assert.AreEqual(expected, Base58.Encode(Convert.FromHexString(hexInput), Base58Variant.Flickr));

    /// <summary>
    /// Verifies that the Flickr variant decodes what it encodes, for payloads with and without leading zero bytes.
    /// </summary>
    [TestMethod]
    public void Decode_WhenFlickrVariant_ShouldRoundTripEncodedPayloads()
    {
        var random = new Random(0xF11C);

        for (var length = 0; length <= 40; length++)
        {
            var payload = new byte[length];
            random.NextBytes(payload);
            if (length > 2)
                payload[0] = 0;

            string encoded = Base58.Encode(payload, Base58Variant.Flickr);

            CollectionAssert.AreEqual(payload, Base58.Decode(encoded, Base58Variant.Flickr), $"length {length}");
        }
    }

    /// <summary>
    /// Verifies that the Flickr and Bitcoin variants encode the same bytes to different text once a letter appears,
    /// because the two alphabets order the letter cases differently.
    /// </summary>
    [TestMethod]
    public void Encode_WhenSameInputFlickrAndBitcoinVariants_ShouldProduceDifferentOutputs()
    {
        byte[] input = Ascii("Hello");

        string bitcoin = Base58.Encode(input, Base58Variant.BitcoinFlickr);
        string flickr = Base58.Encode(input, Base58Variant.Flickr);

        Assert.AreNotEqual(bitcoin, flickr);
    }

}
