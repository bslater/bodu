// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ScryptTests.Verify.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class ScryptTests
{
    /// <summary>
    /// Verifies that verification with a bound accepts the password an encoded hash was made from and rejects another,
    /// on the calling thread and on several.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">The bound.</param>
    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(-1)]
    public void Verify_WhenGivenBound_ShouldAcceptOnlyTheMatchingPassword(int maxDegreeOfParallelism)
    {
        byte[] password = Ascii("hunter2");
        string encoded = Scrypt.Hash(password, Ascii("seasalt"), 1024, 8, 3, 32);

        Assert.IsTrue(Scrypt.Verify(encoded, password, maxDegreeOfParallelism));
        Assert.IsFalse(Scrypt.Verify(encoded, Ascii("hunter3"), maxDegreeOfParallelism));
    }

    /// <summary>
    /// Verifies that verification rejects a bound of zero or below <c>-1</c>, naming the parameter, before decoding the
    /// encoded hash.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">The invalid bound.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(-2)]
    public void Verify_WhenBoundIsInvalid_ShouldThrowArgumentOutOfRangeException(int maxDegreeOfParallelism)
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Scrypt.Verify("not a PHC string", Ascii("hunter2"), maxDegreeOfParallelism);
        });

        Assert.AreEqual(nameof(maxDegreeOfParallelism), ex.ParamName);
    }

    /// <summary>
    /// Verifies that verification with a bound rejects a <see langword="null" /> encoded hash.
    /// </summary>
    [TestMethod]
    public void Verify_WhenEncodedIsNull_ForBound_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Scrypt.Verify(null!, Ascii("hunter2"), 2);
        });

        Assert.AreEqual("encoded", ex.ParamName);
    }
}
