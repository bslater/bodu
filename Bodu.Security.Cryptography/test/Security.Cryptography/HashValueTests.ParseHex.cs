// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashValueTests.ParseHex.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography;

public sealed partial class HashValueTests
{
    /// <summary>
    /// Verifies that <see cref="HashValue.ParseHex" /> decodes well-formed hexadecimal text to the expected bytes.
    /// </summary>
    /// <param name="kat">The hexadecimal vector under test.</param>
    [TestMethod]
    [DynamicData(nameof(ValidHexVectors), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ParseHex_WhenTextIsWellFormed_ShouldDecodeExpectedBytes(ValidKat<string, byte[]> kat)
    {
        var hash = HashValue.ParseHex(kat.Input);

        CollectionAssert.AreEqual(kat.Expected, hash.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="HashValue.ParseHex" /> throws <see cref="FormatException" /> for malformed
    /// hexadecimal text.
    /// </summary>
    /// <param name="kat">The malformed vector under test.</param>
    [TestMethod]
    [DynamicData(nameof(InvalidHexVectors), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ParseHex_WhenTextIsMalformed_ShouldThrowFormatException(InvalidKat<string> kat)
    {
        _ = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = HashValue.ParseHex(kat.Input);
        });
    }

    /// <summary>
    /// Verifies that <see cref="HashValue.ParseHex" /> throws <see cref="ArgumentNullException" /> when the text is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ParseHex_WhenTextIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = HashValue.ParseHex(null!);
        });

        Assert.AreEqual("text", ex.ParamName);
    }
}
