// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ScryptTests.MaxDegreeOfParallelism.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class ScryptTests
{
    /// <summary>
    /// Verifies that the constructors without a bound confine every derivation to the calling thread.
    /// </summary>
    [TestMethod]
    public void MaxDegreeOfParallelism_WhenNotSpecified_ShouldBeOne()
    {
        Assert.AreEqual(1, new Scrypt(16, 1, 1).MaxDegreeOfParallelism);
        Assert.AreEqual(1, new Scrypt(new ScryptParameters { CostN = 16, BlockSizeR = 1, Parallelization = 1 }).MaxDegreeOfParallelism);
    }

    /// <summary>
    /// Verifies that the constructors with a bound report the bound they were given.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">The bound.</param>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(1)]
    [DataRow(4)]
    public void MaxDegreeOfParallelism_WhenSpecified_ShouldReportTheBound(int maxDegreeOfParallelism)
    {
        Assert.AreEqual(maxDegreeOfParallelism, new Scrypt(16, 1, 1, maxDegreeOfParallelism).MaxDegreeOfParallelism);
        Assert.AreEqual(
            maxDegreeOfParallelism,
            new Scrypt(new ScryptParameters { CostN = 16, BlockSizeR = 1, Parallelization = 1 }, maxDegreeOfParallelism).MaxDegreeOfParallelism);
    }

    /// <summary>
    /// Verifies that an instance allowed several threads derives RFC 7914's second key - sixteen units of 1 MiB, enough
    /// to divide - the same through every derivation member.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">The bound.</param>
    [TestMethod]
    [DataRow(2)]
    [DataRow(4)]
    [DataRow(-1)]
    public void MaxDegreeOfParallelism_WhenAboveOne_ShouldNotChangeTheDerivedKey(int maxDegreeOfParallelism)
    {
        const string Expected =
            "fdbabe1c9d3472007856e7190d01e9fe7c6ad7cbc8237830e77376634b373162" +
            "2eaf30d92e22a3886ff109279d9830dac727afb94a83ee6d8360cbdfa2cc0640";
        var scrypt = new Scrypt(1024, 8, 16, maxDegreeOfParallelism);

        byte[] bytes = scrypt.GetBytes(Ascii("password"), Ascii("NaCl"), 64);
        byte[] destination = new byte[64];
        scrypt.DeriveKey(Ascii("password"), Ascii("NaCl"), destination);

        Assert.AreEqual(Expected, Convert.ToHexString(bytes).ToLowerInvariant());
        Assert.AreEqual(Expected, Convert.ToHexString(destination).ToLowerInvariant());
    }

    /// <summary>
    /// Verifies that a hash produced on several threads verifies on the calling thread, and one produced on the calling
    /// thread verifies on several: the encoded hash never depends on the bound.
    /// </summary>
    [TestMethod]
    public void MaxDegreeOfParallelism_WhenHashing_ShouldNotChangeTheEncodedHash()
    {
        byte[] password = Ascii("hunter2");
        byte[] salt = Ascii("seasalt!");

        string threaded = new Scrypt(1024, 8, 4, 4).Hash(password, salt, 32);
        string sequential = new Scrypt(1024, 8, 4).Hash(password, salt, 32);

        Assert.AreEqual(sequential, threaded);
        Assert.IsTrue(Scrypt.Verify(threaded, password));
        Assert.IsTrue(Scrypt.Verify(sequential, password, 4));
    }
}
