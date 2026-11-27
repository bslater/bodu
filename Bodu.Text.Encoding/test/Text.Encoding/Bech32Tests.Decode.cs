// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Bech32Tests.Decode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Bech32Tests
{
    /// <summary>
    /// Verifies that <see cref="Bech32.Decode(string, out string, out byte[], out Bech32Encoding)" /> throws for a null
    /// string.
    /// </summary>
    [TestMethod]
    public void Decode_WhenNullString_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            Bech32.Decode((string)null!, out _, out _, out _);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Bech32.TryDecode(ReadOnlySpan{char}, out string, out byte[], out Bech32Encoding)" />
    /// returns <see langword="false" /> for the BIP-173 invalid test vectors.
    /// </summary>
    /// <param name="source">The malformed encoded string.</param>
    [TestMethod]
    [DataRow("pzry9x0s0muk")]   // no separator character
    [DataRow("1pzry9x0s0muk")] // empty human-readable part
    [DataRow("1qzzfhee")]      // empty human-readable part
    [DataRow("x1b4n0q5v")]     // invalid data character
    [DataRow("li1dgmt3")]      // too short for a checksum
    [DataRow("A1G7SGD8")]      // checksum computed with uppercase HRP
    [DataRow("a12uel5m")]      // single-symbol checksum corruption
    public void TryDecode_WhenInvalidVector_ShouldReturnFalse(string source)
    {
        bool success = Bech32.TryDecode(source.AsSpan(), out string? hrp, out byte[]? data, out _);

        Assert.IsFalse(success);
        Assert.IsNull(hrp);
        Assert.IsNull(data);
    }

    /// <summary>
    /// Verifies that a string mixing upper-case and lower-case characters is rejected per BIP-173.
    /// </summary>
    [TestMethod]
    public void Decode_WhenMixedCase_ShouldThrowFormatException()
    {
        FormatException ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            Bech32.Decode("a12UEL5L", out _, out _, out _);
        });

        Assert.IsTrue(ex.Message.Contains("mix", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Verifies that an input longer than the 90-character maximum is rejected.
    /// </summary>
    [TestMethod]
    public void Decode_WhenLongerThanMaximum_ShouldThrowFormatException()
    {
        string tooLong = "an84characterslonghumanreadablepartthatcontainsthenumber1andtheexcludedcharactersbio1569pvx";
        Assert.IsGreaterThan(90, tooLong.Length);

        Assert.ThrowsExactly<FormatException>(() =>
        {
            Bech32.Decode(tooLong, out _, out _, out _);
        });
    }

    /// <summary>
    /// Verifies that an input without the <c>1</c> separator is rejected.
    /// </summary>
    [TestMethod]
    public void Decode_WhenNoSeparator_ShouldThrowFormatException()
    {
        FormatException ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            Bech32.Decode("pzry9x0s0muk", out _, out _, out _);
        });

        Assert.IsTrue(ex.Message.Contains("separator", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that an input whose data part is shorter than the six-symbol checksum is rejected.
    /// </summary>
    [TestMethod]
    public void Decode_WhenTooShortForChecksum_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            Bech32.Decode("li1dgmt3", out _, out _, out _);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Bech32.IsValid(ReadOnlySpan{char})" /> agrees with the decoder on both a valid and an
    /// invalid input.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenValidAndInvalid_ShouldReflectDecodeOutcome()
    {
        Assert.IsTrue(Bech32.IsValid("a12uel5l".AsSpan()));
        Assert.IsFalse(Bech32.IsValid("a12uel5m".AsSpan()));
    }

    /// <summary>
    /// Verifies that <see cref="Bech32.IsValid(ReadOnlySpan{char})" /> rejects the BIP-173 vector
    /// <c>bc1qw508d6qejxtdg4y5r3zarvary0c5xw7kv8f3t4</c> with its <c>k</c> written as U+212A KELVIN SIGN, which
    /// invariant lowercasing maps to <c>k</c> but which is outside the data charset.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenDataPartHasKelvinSign_ShouldReturnFalse() =>
        Assert.IsFalse(Bech32.IsValid("bc1qw508d6qejxtdg4y5r3zarvary0c5xw7Kv8f3t4".AsSpan()));

    /// <summary>
    /// Verifies that <see cref="Bech32.TryDecode(ReadOnlySpan{char}, out string, out byte[], out Bech32Encoding)" />
    /// returns <see langword="false" /> for the BIP-173 vector <c>bc1qw508d6qejxtdg4y5r3zarvary0c5xw7kv8f3t4</c> with
    /// its <c>k</c> written as U+212A KELVIN SIGN.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenDataPartHasKelvinSign_ShouldReturnFalse()
    {
        bool success = Bech32.TryDecode("bc1qw508d6qejxtdg4y5r3zarvary0c5xw7Kv8f3t4".AsSpan(), out string? hrp, out byte[]? data, out _);

        Assert.IsFalse(success);
        Assert.IsNull(hrp);
        Assert.IsNull(data);
    }

    /// <summary>
    /// Verifies that <see cref="Bech32.TryDecode(ReadOnlySpan{char}, out string, out byte[], out Bech32Encoding)" />
    /// returns <see langword="false" /> for a string whose human-readable part <c>kc</c> is written with U+212A KELVIN
    /// SIGN, which is outside the US-ASCII range 33 to 126 that BIP-173 allows there.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenHrpHasKelvinSign_ShouldReturnFalse()
    {
        string encoded = "K" + Bech32.Encode("kc", [0, 1, 2])[1..];

        bool success = Bech32.TryDecode(encoded.AsSpan(), out string? hrp, out byte[]? data, out _);

        Assert.IsFalse(success);
        Assert.IsNull(hrp);
        Assert.IsNull(data);
    }

    /// <summary>
    /// Verifies that <see cref="Bech32.Decode(string, out string, out byte[], out Bech32Encoding)" /> throws
    /// <see cref="FormatException" /> for a string with a U+212A KELVIN SIGN in its data part.
    /// </summary>
    [TestMethod]
    public void Decode_WhenDataPartHasKelvinSign_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            Bech32.Decode("bc1qw508d6qejxtdg4y5r3zarvary0c5xw7Kv8f3t4", out _, out _, out _);
        });
    }

    /// <summary>
    /// Verifies that
    /// <see cref="Bech32.TryDecodeToBytes(ReadOnlySpan{char}, out string, out byte[], out Bech32Encoding)" /> returns
    /// <see langword="false" /> for a string whose 8-bit payload round-trips when written in ASCII but whose
    /// human-readable part is written with U+212A KELVIN SIGN.
    /// </summary>
    [TestMethod]
    public void TryDecodeToBytes_WhenHrpHasKelvinSign_ShouldReturnFalse()
    {
        string encoded = "K" + Bech32.EncodeFromBytes("kc", [0xDE, 0xAD])[1..];

        bool success = Bech32.TryDecodeToBytes(encoded.AsSpan(), out string? hrp, out byte[]? data, out _);

        Assert.IsFalse(success);
        Assert.IsNull(hrp);
        Assert.IsNull(data);
    }
}
