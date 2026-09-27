// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2Tests.DeriveKey.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Security.Cryptography;

public partial class Argon2Tests
{
    /// <summary>
    /// Verifies that the instance and static one-shot surfaces produce identical tags for the same inputs.
    /// </summary>
    [TestMethod]
    public void DeriveKey_WhenInstanceAndStatic_ShouldProduceIdenticalTags()
    {
        var parameters = new Argon2Parameters { MemoryKiB = 64, Iterations = 2, Parallelism = 2 };
        byte[] password = Encoding.UTF8.GetBytes("correct horse battery staple");
        byte[] salt = Repeat(0x05, 16);

        byte[] instance = new Argon2id(parameters).GetBytes(password, salt);
        byte[] @static = Argon2id.DeriveKey(password, salt, parameters);

        CollectionAssert.AreEqual(instance, @static);
    }

    /// <summary>
    /// Verifies that Argon2id derives a tag of the configured length on a happy-path input.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void DeriveKey_WhenGivenSimpleInput_ShouldReturnTagOfConfiguredLength()
    {
        byte[] tag = Argon2id.DeriveKey(
            Encoding.UTF8.GetBytes("password"),
            Repeat(0x02, 16),
            new Argon2Parameters { MemoryKiB = 64, Iterations = 1, Parallelism = 1, TagLength = 32 });

        Assert.HasCount(32, tag);
    }

    /// <summary>
    /// Verifies that deriving into a destination of the tag length writes the tag <see cref="Argon2.GetBytes" />
    /// returns, for every variant, on the calling thread alone and with the lanes divided among threads.
    /// </summary>
    /// <param name="variant">The variant derived.</param>
    /// <param name="maxDegreeOfParallelism">The bound on the derivation's threads.</param>
    [TestMethod]
    [DataRow("d", 1)]
    [DataRow("i", 1)]
    [DataRow("id", 1)]
    [DataRow("id", 4)]
    public void DeriveKey_WhenDestinationIsTheTagLength_ShouldWriteTheTag(string variant, int maxDegreeOfParallelism)
    {
        Argon2 argon2 = Create(variant, ThreadedParameters(), maxDegreeOfParallelism);
        byte[] password = Repeat(0x31, 20);
        byte[] salt = Repeat(0x32, 16);
        byte[] destination = new byte[argon2.Parameters.TagLength];

        argon2.DeriveKey(password, salt, destination);

        CollectionAssert.AreEqual(argon2.GetBytes(password, salt), destination);
    }

    /// <summary>
    /// Verifies that deriving into a destination whose length differs from the tag length is rejected.
    /// </summary>
    [TestMethod]
    public void DeriveKey_WhenDestinationLengthWrong_ShouldThrowArgumentException()
    {
        var argon = new Argon2id(FastParameters());

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            Span<byte> destination = stackalloc byte[16];
            argon.DeriveKey("password"u8, "saltsalt"u8, destination);
        });
    }
}
