// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SecretBytesTests.ToString.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class SecretBytesTests
{
    /// <summary>
    /// Verifies that <see cref="SecretBytes.ToString" /> reports only the length and never the secret content.
    /// </summary>
    [TestMethod]
    public void ToString_WhenCalled_ShouldReportLengthWithoutContent()
    {
        using var secret = SecretBytes.CopyFrom([0xDE, 0xAD, 0xBE, 0xEF]);

        string text = secret.ToString();

        Assert.AreEqual("SecretBytes (Length = 4)", text);
        Assert.IsFalse(text.Contains("deadbeef", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("DEADBEEF", StringComparison.Ordinal));
    }
}
