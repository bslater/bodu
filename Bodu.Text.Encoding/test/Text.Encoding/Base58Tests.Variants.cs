// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base58Tests.Variants.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base58Tests
{

    /// <summary>
    /// Verifies that the Bitcoin/Flickr variant rejects the visually ambiguous characters <c>0</c>, <c>O</c>,
    /// <c>I</c>, and <c>l</c>.
    /// </summary>
    /// <param name="excluded">An ambiguous character that is not in the Bitcoin alphabet.</param>
    [DataTestMethod]
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
        var input = Ascii("Hello");
        var rippleEncoded = Base58.Encode(input, Base58Variant.Ripple);

        try
        {
            var decodedAsBitcoin = Base58.Decode(rippleEncoded, Base58Variant.BitcoinFlickr);
            Assert.IsFalse(decodedAsBitcoin.SequenceEqual(input),
                "Decoding Ripple-encoded data as Bitcoin/Flickr should not produce the original bytes.");
        }
        catch (FormatException)
        {
            // Acceptable - the Ripple alphabet may use a character that is not in Bitcoin/Flickr.
        }
    }
    /// <summary>
    /// Verifies that the Bitcoin/Flickr and Ripple variants produce different encoded strings for the same input
    /// (because the alphabet ordering differs).
    /// </summary>
    [TestMethod]
    public void Encode_WhenSameInputDifferentVariants_ShouldProduceDifferentOutputs()
    {
        var input = Ascii("Hello");

        var bitcoin = Base58.Encode(input, Base58Variant.BitcoinFlickr);
        var ripple = Base58.Encode(input, Base58Variant.Ripple);

        Assert.AreNotEqual(bitcoin, ripple);
    }

}
