// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base62Tests.Decode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base62Tests
{
    /// <summary>
    /// Verifies that <see cref="Base62.Decode(string, BaseFormatStyles)" /> throws for a null string.
    /// </summary>
    [TestMethod]
    public void Decode_WhenNullString_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Base62.Decode((string)null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base62.Encode(byte[])" /> throws for a null byte array.
    /// </summary>
    [TestMethod]
    public void Encode_WhenNullByteArray_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Base62.Encode((byte[])null!);
        });
    }

    /// <summary>
    /// Verifies that decoding a character outside the alphabet throws a <see cref="FormatException" /> whose message
    /// identifies the Base62 alphabet.
    /// </summary>
    [TestMethod]
    public void Decode_WhenCharacterNotInAlphabet_ShouldThrowFormatException()
    {
        FormatException ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base62.Decode("ab-cd");
        });

        Assert.IsTrue(ex.Message.Contains("Base62 alphabet", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that <see cref="BaseFormatStyles.IgnoreWhitespace" /> skips whitespace during decode.
    /// </summary>
    [TestMethod]
    public void Decode_WhenIgnoreWhitespace_ShouldSkipWhitespace()
    {
        byte[] expected = Base62.Decode("48");

        CollectionAssert.AreEqual(expected, Base62.Decode("4 8", BaseFormatStyles.IgnoreWhitespace));
    }

    /// <summary>
    /// Verifies that <see cref="Base62.IsValid(ReadOnlySpan{char}, BaseFormatStyles)" /> accepts alphabet characters
    /// and rejects others.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenAlphabetAndNonAlphabet_ShouldReflectMembership()
    {
        Assert.IsTrue(Base62.IsValid("0AaZz9".AsSpan()));
        Assert.IsFalse(Base62.IsValid("ab_cd".AsSpan()));
    }

    /// <summary>
    /// Verifies that <see cref="Base62.TryDecode(ReadOnlySpan{char}, Span{byte}, out int, BaseFormatStyles)" /> returns
    /// <see langword="false" /> when the destination buffer is too small.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenDestinationTooSmall_ShouldReturnFalse()
    {
        Span<byte> destination = new byte[1];

        bool success = Base62.TryDecode("48".AsSpan(), destination, out int written);

        Assert.IsFalse(success);
        Assert.AreEqual(0, written);
    }

    /// <summary>
    /// Verifies that an input longer than <see cref="Base62.MaxDecodeInputLength" /> is rejected with
    /// <see cref="FormatException" /> rather than driving the O(n²) BigInteger decode as an
    /// algorithmic-complexity denial-of-service.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Decode_WhenInputExceedsMaxLength_ShouldThrowFormatException()
    {
        // '0' is the zero symbol of the Base62 alphabet, so the input is otherwise well-formed.
        string tooLong = new string('0', Base62.MaxDecodeInputLength + 1);

        Assert.ThrowsExactly<FormatException>(() => _ = Base62.Decode(tooLong));
    }
}
