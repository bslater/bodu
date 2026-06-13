// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SignatureValueTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides unit tests for the <see cref="SignatureValue" /> value type, with member-specific coverage split
/// across the partial files of this class.
/// </summary>
[TestClass]
public sealed partial class SignatureValueTests
{
    /// <summary>
    /// Verifies that <c>default(SignatureValue)</c> behaves as the empty value with
    /// <see cref="SignatureFormat.Unknown" /> across every accessor.
    /// </summary>
    [TestMethod]
    public void DefaultInstance_WhenAccessed_ShouldBehaveAsEmptyValue()
    {
        SignatureValue signature = default;

        Assert.AreEqual(SignatureFormat.Unknown, signature.Format);
        Assert.AreEqual(0, signature.Length);
        Assert.IsTrue(signature.IsEmpty);
        Assert.IsTrue(signature.AsSpan().IsEmpty);
        Assert.AreEqual(0, signature.ToArray().Length);
        Assert.AreEqual(string.Empty, signature.ToHexString());
        Assert.AreEqual(string.Empty, signature.ToBase64String());
        Assert.AreEqual(string.Empty, signature.ToString());
    }

    /// <summary>
    /// Verifies that <see cref="SignatureValue.ToHexString" /> produces lowercase hexadecimal and that
    /// <see cref="SignatureValue.ToBase64String" /> matches <see cref="Convert.ToBase64String(byte[])" />.
    /// </summary>
    [TestMethod]
    public void ToHexAndBase64String_WhenValueIsNonEmpty_ShouldFormatBytes()
    {
        var bytes = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        var signature = SignatureValue.FromBytes(bytes, SignatureFormat.P1363);

        Assert.AreEqual("deadbeef", signature.ToHexString());
        Assert.AreEqual(Convert.ToBase64String(bytes), signature.ToBase64String());
        Assert.AreEqual(signature.ToHexString(), signature.ToString());
    }
}
