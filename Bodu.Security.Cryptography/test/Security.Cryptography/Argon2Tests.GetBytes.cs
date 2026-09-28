// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2Tests.GetBytes.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Security.Cryptography;

public partial class Argon2Tests
{
    /// <summary>
    /// Verifies that deriving with a salt shorter than 8 bytes throws an <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void GetBytes_WhenSaltTooShort_ShouldThrowArgumentException()
    {
        var argon2 = new Argon2id(new Argon2Parameters { MemoryKiB = 64, Iterations = 1, Parallelism = 1 });

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = argon2.GetBytes(Encoding.UTF8.GetBytes("pw"), Repeat(0x01, 4));
        });

        Assert.AreEqual("salt", ex.ParamName);
    }
}
