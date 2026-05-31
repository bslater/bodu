// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base85Tests.IsValid.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base85Tests
{

    /// <summary>
    /// Verifies that <see cref="Base85.IsBase85Digit" /> with the Z85 variant recognises Z85-specific characters.
    /// </summary>
    [TestMethod]
    public void IsBase85Digit_ForZ85Variant_ShouldRecogniseAlphabet()
    {
        Assert.IsTrue(Base85.IsBase85Digit('0', Base85Variant.Z85));
        Assert.IsTrue(Base85.IsBase85Digit('#', Base85Variant.Z85));
        Assert.IsFalse(Base85.IsBase85Digit('"', Base85Variant.Z85));
    }

    /// <summary>
    /// Verifies that <see cref="Base85.IsBase85Digit" /> recognises alphabet characters and rejects shortcuts.
    /// </summary>
    [TestMethod]
    public void IsBase85Digit_ShouldRecogniseAlphabet()
    {
        Assert.IsTrue(Base85.IsBase85Digit('!')); // Ascii85 alphabet[0]
        Assert.IsTrue(Base85.IsBase85Digit('u')); // Ascii85 alphabet[84]
        Assert.IsFalse(Base85.IsBase85Digit('z')); // 'z' is shortcut, not a digit
        Assert.IsFalse(Base85.IsBase85Digit('\x01'));
    }
    /// <summary>
    /// Verifies that <see cref="Base85.IsValid" /> returns <see langword="true" /> for canonical Ascii85 input.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenAscii85CanonicalInput_ShouldReturnTrue()
    {
        var encoded = Base85.Encode([0xDE, 0xAD, 0xBE, 0xEF, 0xCA, 0xFE]);

        Assert.IsTrue(Base85.IsValid(encoded.AsSpan()));
    }

    /// <summary>
    /// Regression: verifies that <see cref="Base85.IsValid" /> for Ascii85 accepts the canonical partial-group
    /// sizes (2, 3, 4 chars) which represent 1, 2, 3 bytes of tail.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenAscii85CanonicalPartialGroup_ShouldReturnTrue()
    {
        Assert.IsTrue(Base85.IsValid("!!".AsSpan()));    // 2 chars → 1 byte
        Assert.IsTrue(Base85.IsValid("!!!".AsSpan()));   // 3 chars → 2 bytes
        Assert.IsTrue(Base85.IsValid("!!!!".AsSpan()));  // 4 chars → 3 bytes
        Assert.IsTrue(Base85.IsValid("9jqo^".AsSpan()));  // 5 chars → 4 bytes (full group)
    }

    /// <summary>
    /// Verifies that <see cref="Base85.IsValid" /> with <see cref="BaseFormatStyles.IgnoreWhitespace" /> accepts an
    /// Ascii85 input containing embedded whitespace — drives the whitespace-skip branch.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenAscii85IgnoreWhitespaceAndInputContainsWhitespace_ShouldReturnTrue()
    {
        Assert.IsTrue(Base85.IsValid("9jq o^".AsSpan(), Base85Variant.Ascii85, BaseFormatStyles.IgnoreWhitespace));
    }

    /// <summary>
    /// Verifies that <see cref="Base85.IsValid" /> returns <see langword="false" /> for an input containing a
    /// character past the 128-entry lookup table range — exercises the <c>c &gt;= lookup.Length</c> branch
    /// independently of the <c>lookup[c] &lt; 0</c> branch.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenAscii85InputContainsCharAboveLookupRange_ShouldReturnFalse()
    {
        // 'ÿ' (U+00FF) is past the 128-entry Ascii85 lookup.
        Assert.IsFalse(Base85.IsValid("9jqÿo".AsSpan()));
    }

    /// <summary>
    /// Regression: verifies that <see cref="Base85.IsValid" /> now performs full structural validation. A trailing
    /// single character (partial group of 1) cannot represent any byte and must be rejected — previously IsValid
    /// reported true because only alphabet membership was checked.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenAscii85TrailingSingleChar_ShouldReturnFalse()
    {
        Assert.IsFalse(Base85.IsValid("9".AsSpan()));
        Assert.IsTrue(Base85.IsValid("9jqo^".AsSpan()), "5-char Ascii85 input is valid.");
    }

    /// <summary>
    /// Verifies that <see cref="Base85.IsValid" /> returns <see langword="true" /> for the <c>z</c> shortcut under
    /// Ascii85.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenAscii85ZShortcut_ShouldReturnTrue()
    {
        Assert.IsTrue(Base85.IsValid("zz".AsSpan()));
    }

    /// <summary>
    /// Regression: verifies that <see cref="Base85.IsValid" /> rejects a misplaced <c>z</c> shortcut. The shortcut
    /// may only appear at a 5-char group boundary.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenAscii85ZShortcutInsideGroup_ShouldReturnFalse()
    {
        Assert.IsFalse(Base85.IsValid("9jz".AsSpan()));
    }

    /// <summary>
    /// Verifies that <see cref="Base85.IsValid" /> returns <see langword="false" /> for an invalid character.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenInvalidCharacter_ShouldReturnFalse()
    {
        Assert.IsFalse(Base85.IsValid("\x01\x02".AsSpan()));
    }

    /// <summary>
    /// Verifies that <see cref="Base85.IsValid" /> for the Z85 variant ignores
    /// <see cref="BaseFormatStyles.AllowPrefix" /> — Z85 has no delimiter convention, so a <c>&lt;~</c> prefix is just
    /// invalid characters under Z85.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenZ85AndAllowPrefix_ShouldStillRejectDelimiters()
    {
        Assert.IsFalse(Base85.IsValid("<~HelloWorld~>".AsSpan(), Base85Variant.Z85, BaseFormatStyles.AllowPrefix));
    }

    /// <summary>
    /// Regression: verifies that <see cref="Base85.IsValid" /> for Z85 requires the symbol count to be a multiple
    /// of five (RFC 32 length rule).
    /// </summary>
    [TestMethod]
    public void IsValid_WhenZ85LengthNotMultipleOfFive_ShouldReturnFalse()
    {
        Assert.IsFalse(Base85.IsValid("HelloW".AsSpan(), Base85Variant.Z85));
        Assert.IsTrue(Base85.IsValid("HelloWorld".AsSpan(), Base85Variant.Z85));
    }

}
