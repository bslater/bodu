// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2Tests.MaxDegreeOfParallelism.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class Argon2Tests
{
    /// <summary>
    /// Verifies that an instance constructed without a bound reports <c>-1</c>, leaving the choice to the library.
    /// </summary>
    [TestMethod]
    public void MaxDegreeOfParallelism_WhenConstructedWithoutBound_ShouldBeMinusOne()
    {
        Argon2[] instances = [new Argon2id(FastParameters()), new Argon2i(FastParameters()), new Argon2d(FastParameters())];

        foreach (Argon2 instance in instances)
            Assert.AreEqual(-1, instance.MaxDegreeOfParallelism, instance.GetType().Name);
    }

    /// <summary>
    /// Verifies that each variant reports the bound it was constructed with.
    /// </summary>
    /// <param name="variant">The variant constructed.</param>
    /// <param name="maxDegreeOfParallelism">The bound supplied to the constructor.</param>
    [TestMethod]
    [DataRow("id", -1)]
    [DataRow("id", 1)]
    [DataRow("id", 2)]
    [DataRow("id", 64)]
    [DataRow("id", int.MaxValue)]
    [DataRow("i", 1)]
    [DataRow("i", 3)]
    [DataRow("d", 1)]
    [DataRow("d", 4)]
    public void MaxDegreeOfParallelism_WhenBoundIsGiven_ShouldReturnIt(string variant, int maxDegreeOfParallelism)
    {
        Argon2 instance = Create(variant, FastParameters(), maxDegreeOfParallelism);

        Assert.AreEqual(maxDegreeOfParallelism, instance.MaxDegreeOfParallelism);
    }
}
