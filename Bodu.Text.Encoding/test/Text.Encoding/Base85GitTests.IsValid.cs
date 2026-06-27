// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base85GitTests.IsValid.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base85GitTests
{
    /// <summary>The 85-character Git Base85 alphabet, mirrored for test assertions.</summary>
    private const string GitAlphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz!#$%&()*+-;<=>?@^_`{|}~";

    /// <summary>
    /// Verifies that the Git Base85 alphabet contains exactly 85 distinct characters.
    /// </summary>
    [TestMethod]
    public void GitAlphabet_ShouldContainExactlyEightyFiveDistinctCharacters()
    {
        Assert.AreEqual(85, GitAlphabet.Length);
        Assert.AreEqual(85, new HashSet<char>(GitAlphabet).Count);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.IsBase85Digit(char, Base85Variant)" /> accepts every Git alphabet character.
    /// </summary>
    [TestMethod]
    public void IsBase85Digit_WhenGitAlphabetCharacter_ShouldReturnTrue()
    {
        foreach (char c in GitAlphabet)
            Assert.IsTrue(Base85.IsBase85Digit(c, Base85Variant.GitCompact), $"'{c}' should be a Git digit.");
    }

    /// <summary>
    /// Verifies that <see cref="Base85.IsBase85Digit(char, Base85Variant)" /> rejects characters outside the Git
    /// alphabet.
    /// </summary>
    /// <param name="value">A character not in the Git alphabet.</param>
    [TestMethod]
    [DataRow(' ')]
    [DataRow('"')]
    [DataRow('\'')]
    [DataRow(',')]
    [DataRow('.')]
    [DataRow('/')]
    [DataRow(':')]
    [DataRow('[')]
    [DataRow('\\')]
    [DataRow(']')]
    public void IsBase85Digit_WhenNotGitAlphabetCharacter_ShouldReturnFalse(char value)
    {
        Assert.IsFalse(Base85.IsBase85Digit(value, Base85Variant.GitCompact));
    }

    /// <summary>
    /// Verifies that <c>z</c> is treated as an ordinary Git digit rather than the Adobe all-zero shortcut, so a
    /// full four-character-plus-tail group containing <c>z</c> validates.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenGitInputContainsZ_ShouldTreatAsDigit()
    {
        Assert.IsTrue(Base85.IsValid("zzzzz".AsSpan(), Base85Variant.GitCompact));
    }

    /// <summary>
    /// Verifies that the Git variant does not honour the Adobe <c>&lt;~ ... ~&gt;</c> delimiter convention: because
    /// <c>&lt;</c>, <c>~</c>, and <c>&gt;</c> are ordinary Git alphabet digits, <see cref="BaseFormatStyles.AllowPrefix" />
    /// does not strip them and they decode as payload rather than framing.
    /// </summary>
    [TestMethod]
    public void Decode_WhenAdobeDelimitersForGit_ShouldNotStripAndTreatAsData()
    {
        byte[] withDelimiters = Base85.Decode("<~0RjUA~>".AsSpan(), Base85Variant.GitCompact, BaseFormatStyles.AllowPrefix);
        byte[] withoutPrefixStyle = Base85.Decode("<~0RjUA~>".AsSpan(), Base85Variant.GitCompact);
        byte[] payloadOnly = Base85.Decode("0RjUA".AsSpan(), Base85Variant.GitCompact);

        // AllowPrefix has no effect for Git — the delimiter characters are decoded as data either way ...
        CollectionAssert.AreEqual(withoutPrefixStyle, withDelimiters);

        // ... so the result differs from decoding the bare payload.
        CollectionAssert.AreNotEqual(payloadOnly, withDelimiters);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.IsValid(ReadOnlySpan{char}, Base85Variant, BaseFormatStyles)" /> rejects a
    /// terminal partial group of a single character.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenSingleTrailingCharacterForGit_ShouldReturnFalse()
    {
        Assert.IsFalse(Base85.IsValid("0RjUA0".AsSpan(), Base85Variant.GitCompact));
    }

    /// <summary>
    /// Verifies that <see cref="Base85.IsValid(ReadOnlySpan{char}, Base85Variant, BaseFormatStyles)" /> agrees with
    /// <see cref="Base85.TryDecode(ReadOnlySpan{char}, Span{byte}, out int, Base85Variant, BaseFormatStyles)" /> for
    /// Git compact inputs.
    /// </summary>
    /// <param name="encoded">The candidate Git compact text.</param>
    [TestMethod]
    [DataRow("")]
    [DataRow("00000")]
    [DataRow("|NsC0")]
    [DataRow("0R")]
    [DataRow("0RjUA")]
    [DataRow("0")]
    [DataRow("0RjUA0")]
    [DataRow("0R j")]
    public void IsValid_ShouldAgreeWithTryDecodeForGit(string encoded)
    {
        Span<byte> destination = new byte[encoded.Length + 4];
        bool tryDecode = Base85.TryDecode(encoded.AsSpan(), destination, out _, Base85Variant.GitCompact);

        bool isValid = Base85.IsValid(encoded.AsSpan(), Base85Variant.GitCompact);

        Assert.AreEqual(isValid, tryDecode, $"IsValid disagreed with TryDecode for '{encoded}'.");
    }
}
