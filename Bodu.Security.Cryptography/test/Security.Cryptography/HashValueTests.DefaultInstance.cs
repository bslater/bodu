// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashValueTests.DefaultInstance.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class HashValueTests
{
    /// <summary>
    /// Verifies that <c>default(HashValue)</c> behaves as the empty value across every accessor rather than
    /// throwing on an unset backing field.
    /// </summary>
    [TestMethod]
    public void DefaultInstance_WhenAccessed_ShouldBehaveAsEmptyValue()
    {
        HashValue hash = default;

        Assert.AreEqual(0, hash.Length);
        Assert.IsTrue(hash.IsEmpty);
        Assert.IsTrue(hash.AsSpan().IsEmpty);
        Assert.AreEqual(0, hash.ToArray().Length);
        Assert.AreEqual(string.Empty, hash.ToHexString());
        Assert.AreEqual(string.Empty, hash.ToBase64String());
        Assert.AreEqual(string.Empty, hash.ToString());
    }
}
