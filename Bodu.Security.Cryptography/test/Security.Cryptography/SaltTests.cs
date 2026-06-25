// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SaltTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides unit tests for the <see cref="Salt" /> value type, with member-specific coverage split across the
/// partial files of this class.
/// </summary>
[TestClass]
public sealed partial class SaltTests
{
    /// <summary>
    /// Verifies that <c>default(Salt)</c> behaves as the empty value across every accessor.
    /// </summary>
    [TestMethod]
    public void DefaultInstance_WhenAccessed_ShouldBehaveAsEmptyValue()
    {
        Salt salt = default;

        Assert.AreEqual(0, salt.Length);
        Assert.IsTrue(salt.IsEmpty);
        Assert.IsTrue(salt.AsSpan().IsEmpty);
        Assert.IsEmpty(salt.ToArray());
        Assert.AreEqual(string.Empty, salt.ToString());
    }

    /// <summary>
    /// Verifies that <see cref="Salt.ToString" /> formats the value as lowercase hexadecimal.
    /// </summary>
    [TestMethod]
    public void ToString_WhenValueIsNonEmpty_ShouldProduceLowercaseHex()
    {
        var salt = Salt.FromBytes([0xCA, 0xFE, 0x01]);

        Assert.AreEqual("cafe01", salt.ToString());
    }
}
