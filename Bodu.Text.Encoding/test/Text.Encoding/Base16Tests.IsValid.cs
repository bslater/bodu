// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base16Tests.IsValid.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base16Tests
{

    /// <summary>
    /// Verifies that <see cref="Base16.IsHexDigit(char)" /> rejects non-hex characters.
    /// </summary>
    /// <param name="value">The character to test.</param>
    [DataTestMethod]
    [DataRow(' ')]
    [DataRow('g')]
    [DataRow('G')]
    [DataRow('!')]
    [DataRow('-')]
    public void IsHexDigit_WhenNotValid_ShouldReturnFalse(char value)
    {
        Assert.IsFalse(Base16.IsHexDigit(value));
    }

    /// <summary>
    /// Verifies that <see cref="Base16.IsHexDigit(char)" /> recognises all valid hex digit characters.
    /// </summary>
    /// <param name="value">The character to test.</param>
    [DataTestMethod]
    [DataRow('0')]
    [DataRow('9')]
    [DataRow('A')]
    [DataRow('F')]
    [DataRow('a')]
    [DataRow('f')]
    public void IsHexDigit_WhenValidDigit_ShouldReturnTrue(char value)
    {
        Assert.IsTrue(Base16.IsHexDigit(value));
    }

    /// <summary>
    /// Verifies that <see cref="Base16.IsValid(ReadOnlySpan{char}, BaseFormatStyles)" /> accepts a decorated input
    /// when the matching styles are set.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenAllowPrefixAndIgnoreWhitespace_ShouldAcceptDecoratedInput()
    {
        Assert.IsTrue(Base16.IsValid(
            "0x DE AD BE EF".AsSpan(),
            BaseFormatStyles.AllowPrefix | BaseFormatStyles.IgnoreWhitespace));
    }

    /// <summary>
    /// Verifies that <see cref="Base16.IsValid(ReadOnlySpan{char}, BaseFormatStyles)" /> returns <see langword="true" />
    /// for an empty input.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenEmpty_ShouldReturnTrue()
    {
        Assert.IsTrue(Base16.IsValid(ReadOnlySpan<char>.Empty));
    }

    /// <summary>
    /// Verifies that <see cref="Base16.IsValid(ReadOnlySpan{char}, BaseFormatStyles)" /> returns <see langword="false" />
    /// for non-hex characters.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenInvalidCharacter_ShouldReturnFalse()
    {
        Assert.IsFalse(Base16.IsValid("ab!d".AsSpan()));
    }
    /// <summary>
    /// Verifies that <see cref="Base16.IsValid(ReadOnlySpan{char}, BaseFormatStyles)" /> returns <see langword="true" />
    /// for a clean even-length hex string.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenStrictAndCleanEven_ShouldReturnTrue()
    {
        Assert.IsTrue(Base16.IsValid("DEADBEEF".AsSpan()));
    }

    /// <summary>
    /// Verifies that <see cref="Base16.IsValid(ReadOnlySpan{char}, BaseFormatStyles)" /> returns <see langword="false" />
    /// for an odd-length input.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenStrictAndOddLength_ShouldReturnFalse()
    {
        Assert.IsFalse(Base16.IsValid("abc".AsSpan()));
    }

}
