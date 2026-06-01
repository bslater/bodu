// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SkipjackTests.CreateDecryptor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public sealed partial class SkipjackTests
{
    /// <summary>
    /// Verifies that <see cref="Skipjack.CreateDecryptor(byte[], byte[])" /> with an out-of-range
    /// IV length throws <see cref="CryptographicException" /> for every wrong size.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(7)]
    [DataRow(9)]
    [DataRow(16)]
    public void CreateDecryptor_WhenIVLengthIsInvalid_ShouldThrowExactly(int ivLength)
    {
        using Skipjack algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = algorithm.CreateDecryptor(new byte[10], new byte[ivLength]);
        });
    }
}
