// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashValueTests.ToBase64String.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class HashValueTests
{
    /// <summary>
    /// Verifies that <see cref="HashValue.ToBase64String" /> matches
    /// <see cref="Convert.ToBase64String(byte[])" /> over the same bytes.
    /// </summary>
    [TestMethod]
    public void ToBase64String_WhenValueIsNonEmpty_ShouldMatchConvertToBase64String()
    {
        var bytes = new byte[] { 0x01, 0x02, 0x03, 0xFD, 0xFE, 0xFF };
        var hash = HashValue.FromBytes(bytes);

        Assert.AreEqual(Convert.ToBase64String(bytes), hash.ToBase64String());
    }

    /// <summary>
    /// Verifies that <see cref="HashValue.ToBase64String" /> returns an empty string for the empty value.
    /// </summary>
    [TestMethod]
    public void ToBase64String_WhenValueIsEmpty_ShouldReturnEmptyString()
    {
        Assert.AreEqual(string.Empty, default(HashValue).ToBase64String());
    }
}
