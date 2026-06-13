// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base45Tests.IsValid.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base45Tests
{
    /// <summary>
    /// Verifies that <see cref="Base45.IsValid(ReadOnlySpan{char}, BaseFormatStyles)" /> accepts a well-formed input.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenWellFormed_ShouldReturnTrue()
    {
        Assert.IsTrue(Base45.IsValid("BB8".AsSpan()));
    }

    /// <summary>
    /// Verifies that <see cref="Base45.IsValid(ReadOnlySpan{char}, BaseFormatStyles)" /> accepts an empty input.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenEmpty_ShouldReturnTrue()
    {
        Assert.IsTrue(Base45.IsValid(ReadOnlySpan<char>.Empty));
    }

    /// <summary>
    /// Verifies that <see cref="Base45.IsValid(ReadOnlySpan{char}, BaseFormatStyles)" /> rejects malformed inputs —
    /// triplet overflow, an alien character, and an invalid length.
    /// </summary>
    /// <param name="text">The candidate text.</param>
    [TestMethod]
    [DataRow("GGW")]
    [DataRow("ab")]
    [DataRow("A")]
    [DataRow("::")]
    public void IsValid_WhenMalformed_ShouldReturnFalse(string text)
    {
        Assert.IsFalse(Base45.IsValid(text.AsSpan()));
    }

    /// <summary>
    /// Verifies that <see cref="BaseFormatStyles.IgnoreWhitespace" /> skips tab, carriage return, and line feed so the
    /// input decodes as if they were absent.
    /// </summary>
    [TestMethod]
    public void Decode_WhenIgnoreWhitespaceAndNewlines_ShouldDecodeAsIfAbsent()
    {
        CollectionAssert.AreEqual("AB"u8.ToArray(), Base45.Decode("B\tB\n8", BaseFormatStyles.IgnoreWhitespace));
    }

    /// <summary>
    /// Verifies that a line feed is rejected by default because it is not in the Base45 alphabet.
    /// </summary>
    [TestMethod]
    public void Decode_WhenNewlineAndNoIgnoreWhitespace_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base45.Decode("BB8\n");
        });
    }

    /// <summary>
    /// Verifies that the space character is treated as a Base45 symbol even under
    /// <see cref="BaseFormatStyles.IgnoreWhitespace" />, so <c>"%69 VD92EX0"</c> still decodes to <c>"Hello!!"</c>.
    /// </summary>
    [TestMethod]
    public void Decode_WhenIgnoreWhitespace_ShouldPreserveSpaceSymbol()
    {
        CollectionAssert.AreEqual("Hello!!"u8.ToArray(), Base45.Decode("%69 VD92EX0", BaseFormatStyles.IgnoreWhitespace));
    }

    /// <summary>
    /// Verifies that the <see cref="BinaryEncodings.Base45" /> singleton round-trips through the unified
    /// <see cref="IBinaryEncoding" /> surface.
    /// </summary>
    [TestMethod]
    public void BinaryEncodingsBase45_WhenRoundTripped_ShouldReturnOriginalBytes()
    {
        IBinaryEncoding encoding = BinaryEncodings.Base45;
        var payload = "base-45"u8.ToArray();

        var encoded = encoding.Encode(payload);

        Assert.AreEqual("UJCLQE7W581", encoded);
        CollectionAssert.AreEqual(payload, encoding.Decode(encoded));
    }

    /// <summary>
    /// Verifies that <see cref="BinaryEncodings.Get(string)" /> resolves the <c>"base45"</c> name to the Base45
    /// encoding.
    /// </summary>
    [TestMethod]
    public void BinaryEncodingsGet_WhenBase45Name_ShouldReturnBase45Encoding()
    {
        IBinaryEncoding encoding = BinaryEncodings.Get("base45");

        Assert.AreEqual("base45", encoding.Name);
    }
}
