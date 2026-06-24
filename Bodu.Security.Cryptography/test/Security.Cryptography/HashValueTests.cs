// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashValueTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides unit tests for the <see cref="HashValue" /> value type, with member-specific coverage split across the
/// partial files of this class.
/// </summary>
[TestClass]
public sealed partial class HashValueTests
{
    /// <summary>
    /// Gets the well-formed hexadecimal vectors used by the parse and format tests: each row pairs hexadecimal text
    /// with the bytes it decodes to.
    /// </summary>
    /// <value>Rows of <see cref="ValidKat{TInput, TExpected}" /> wrapped for <see cref="DynamicDataAttribute" />.</value>
    public static IEnumerable<object[]> ValidHexVectors =>
        new[]
        {
            new ValidKat<string, byte[]>("Empty", string.Empty, []),
            new ValidKat<string, byte[]>("SingleByteLowercase", "ab", [0xAB]),
            new ValidKat<string, byte[]>("SingleByteUppercase", "AB", [0xAB]),
            new ValidKat<string, byte[]>("MixedCase", "DeadBeef", [0xDE, 0xAD, 0xBE, 0xEF]),
            new ValidKat<string, byte[]>("LeadingZeros", "0001", [0x00, 0x01]),
            new ValidKat<string, byte[]>(
                "Sha256SizedDigest",
                "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
                Convert.FromHexString("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855")),
        }.Select(kat => new object[] { kat });

    /// <summary>
    /// Gets the malformed hexadecimal vectors rejected by the parse tests.
    /// </summary>
    /// <value>Rows of <see cref="InvalidKat{TInput}" /> wrapped for <see cref="DynamicDataAttribute" />.</value>
    public static IEnumerable<object[]> InvalidHexVectors =>
        new[]
        {
            new InvalidKat<string>("OddLength", "abc", typeof(FormatException)),
            new InvalidKat<string>("NonHexCharacter", "zz", typeof(FormatException)),
            new InvalidKat<string>("EmbeddedWhitespace", "ab cd", typeof(FormatException)),
            new InvalidKat<string>("HexPrefix", "0xab", typeof(FormatException)),
            new InvalidKat<string>("UnicodeDigit", "ａｂ", typeof(FormatException)),
        }.Select(kat => new object[] { kat });
}
