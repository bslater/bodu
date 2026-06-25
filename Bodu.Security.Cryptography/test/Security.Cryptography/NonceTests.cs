// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonceTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides unit tests for the <see cref="Nonce" /> value type, with member-specific coverage split across the
/// partial files of this class.
/// </summary>
[TestClass]
public sealed partial class NonceTests
{
    /// <summary>
    /// Verifies that <c>default(Nonce)</c> behaves as the empty value across every accessor.
    /// </summary>
    [TestMethod]
    public void DefaultInstance_WhenAccessed_ShouldBehaveAsEmptyValue()
    {
        Nonce nonce = default;

        Assert.AreEqual(0, nonce.Length);
        Assert.IsTrue(nonce.IsEmpty);
        Assert.IsTrue(nonce.AsSpan().IsEmpty);
        Assert.IsEmpty(nonce.ToArray());
        Assert.AreEqual(string.Empty, nonce.ToString());
    }

    /// <summary>
    /// Verifies that <see cref="Nonce.ToString" /> formats the value as lowercase hexadecimal.
    /// </summary>
    [TestMethod]
    public void ToString_WhenValueIsNonEmpty_ShouldProduceLowercaseHex()
    {
        var nonce = Nonce.FromBytes([0xDE, 0xAD, 0x0F]);

        Assert.AreEqual("dead0f", nonce.ToString());
    }
}
