// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AuthenticationTagTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides unit tests for the <see cref="AuthenticationTag" /> value type, with member-specific coverage split
/// across the partial files of this class.
/// </summary>
[TestClass]
public sealed partial class AuthenticationTagTests
{
    /// <summary>
    /// Verifies that <c>default(AuthenticationTag)</c> behaves as the empty value across every accessor.
    /// </summary>
    [TestMethod]
    public void DefaultInstance_WhenAccessed_ShouldBehaveAsEmptyValue()
    {
        AuthenticationTag tag = default;

        Assert.AreEqual(0, tag.Length);
        Assert.IsTrue(tag.IsEmpty);
        Assert.IsTrue(tag.AsSpan().IsEmpty);
        Assert.IsEmpty(tag.ToArray());
        Assert.AreEqual(string.Empty, tag.ToString());
    }

    /// <summary>
    /// Verifies that <see cref="AuthenticationTag.ToString" /> formats the value as lowercase hexadecimal.
    /// </summary>
    [TestMethod]
    public void ToString_WhenValueIsNonEmpty_ShouldProduceLowercaseHex()
    {
        var tag = AuthenticationTag.FromBytes([0xBE, 0xEF, 0x00]);

        Assert.AreEqual("beef00", tag.ToString());
    }
}
