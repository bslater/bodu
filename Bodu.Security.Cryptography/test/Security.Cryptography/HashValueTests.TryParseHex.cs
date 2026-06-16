// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashValueTests.TryParseHex.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography;

public sealed partial class HashValueTests
{
    /// <summary>
    /// Verifies that <see cref="HashValue.TryParseHex" /> decodes well-formed hexadecimal text and returns
    /// <see langword="true" />.
    /// </summary>
    /// <param name="kat">The hexadecimal vector under test.</param>
    [TestMethod]
    [DynamicData(nameof(ValidHexVectors), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void TryParseHex_WhenTextIsWellFormed_ShouldReturnTrueAndDecode(ValidKat<string, byte[]> kat)
    {
        bool parsed = HashValue.TryParseHex(kat.Input, out var hash);

        Assert.IsTrue(parsed);
        CollectionAssert.AreEqual(kat.Expected, hash.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="HashValue.TryParseHex" /> returns <see langword="false" /> with the empty value for
    /// malformed hexadecimal text, without throwing.
    /// </summary>
    /// <param name="kat">The malformed vector under test.</param>
    [TestMethod]
    [DynamicData(nameof(InvalidHexVectors), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void TryParseHex_WhenTextIsMalformed_ShouldReturnFalseWithEmptyValue(InvalidKat<string> kat)
    {
        bool parsed = HashValue.TryParseHex(kat.Input, out var hash);

        Assert.IsFalse(parsed);
        Assert.IsTrue(hash.IsEmpty);
    }

    /// <summary>
    /// Verifies that <see cref="HashValue.TryParseHex" /> returns <see langword="false" /> with the empty value when
    /// the text is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void TryParseHex_WhenTextIsNull_ShouldReturnFalseWithEmptyValue()
    {
        bool parsed = HashValue.TryParseHex(null, out var hash);

        Assert.IsFalse(parsed);
        Assert.IsTrue(hash.IsEmpty);
    }
}
