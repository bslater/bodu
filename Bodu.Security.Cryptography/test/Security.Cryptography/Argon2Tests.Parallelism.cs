// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2Tests.Parallelism.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// The parallelism contract: whatever bound is placed on a derivation's threads, and whether or not its lanes are
/// divided among them, every variant derives the same tag, including when many derivations run at once.
/// </summary>
public partial class Argon2Tests
{
    /// <summary>
    /// The bounds the sweeps apply: the calling thread alone, two to four threads, the library's choice, and more
    /// threads than any shape has lanes.
    /// </summary>
    private static readonly int[] ThreadBounds = [1, 2, 3, 4, -1, 64];

    /// <summary>
    /// Verifies that each variant derives the same tag at every bound, for a shape whose lanes the library divides
    /// among threads by default.
    /// </summary>
    /// <param name="variant">The variant derived.</param>
    [TestMethod]
    [DataRow("d")]
    [DataRow("i")]
    [DataRow("id")]
    public void GetBytes_WhenBoundVaries_ShouldReturnTheSameTag(string variant)
    {
        Argon2Parameters parameters = ThreadedParameters() with { Iterations = 2 };
        byte[] password = Repeat(0x11, 16);
        byte[] salt = Repeat(0x22, 16);
        byte[] expected = Create(variant, parameters, 1).GetBytes(password, salt);

        foreach (int bound in ThreadBounds)
            CollectionAssert.AreEqual(expected, Create(variant, parameters, bound).GetBytes(password, salt), $"bound {bound}");
    }

    /// <summary>
    /// Verifies that a PHC string produced with the lanes divided among threads is the one produced on the calling
    /// thread alone, and verifies at every bound.
    /// </summary>
    [TestMethod]
    public void Hash_WhenLanesAreDividedAmongThreads_ShouldProduceTheSameEncodedString()
    {
        byte[] password = Repeat(0x33, 12);
        byte[] salt = Repeat(0x44, 16);
        string expected = new Argon2id(ThreadedParameters(), 1).Hash(password, salt);

        string encoded = new Argon2id(ThreadedParameters(), 4).Hash(password, salt);

        Assert.AreEqual(expected, encoded);
        foreach (int bound in ThreadBounds)
            Assert.IsTrue(Argon2.Verify(encoded, password, [], bound), $"bound {bound}");
    }

    /// <summary>
    /// Verifies that one instance shared by many threads, each derivation dividing its lanes among threads of its own,
    /// returns the same tag to every caller.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void GetBytes_WhenOneInstanceIsSharedAcrossThreads_ShouldReturnTheSameTagToEachCaller()
    {
        const int Derivations = 32;
        byte[] password = Repeat(0x55, 16);
        byte[] salt = Repeat(0x66, 16);
        byte[] expected = new Argon2id(ThreadedParameters(), 1).GetBytes(password, salt);
        var shared = new Argon2id(ThreadedParameters());
        byte[][] tags = new byte[Derivations][];

        Parallel.For(0, Derivations, index => tags[index] = shared.GetBytes(password, salt));

        for (int index = 0; index < Derivations; index++)
            CollectionAssert.AreEqual(expected, tags[index], $"derivation {index}");
    }
}
