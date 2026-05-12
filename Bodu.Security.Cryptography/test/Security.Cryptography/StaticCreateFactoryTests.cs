// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StaticCreateFactoryTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Verifies that the public static <c>Create()</c> factories declared on the algorithm types return non-null
/// instances. These factory methods are otherwise reached only through reflection-based discovery
/// (<see cref="System.Security.Cryptography.CryptoConfig" />), which is not exercised by the rest of the suite.
/// </summary>
[TestClass]
public sealed class StaticCreateFactoryTests
{
    /// <summary>
    /// Verifies that <see cref="Camellia.Create" /> returns a non-null instance.
    /// </summary>
    [TestMethod]
    public void Camellia_Create_ShouldReturnNonNullInstance()
    {
        using var algorithm = Camellia.Create();

        Assert.IsNotNull(algorithm);
    }

    /// <summary>
    /// Verifies that <see cref="Serpent128.Create" /> returns a non-null instance.
    /// </summary>
    [TestMethod]
    public void Serpent128_Create_ShouldReturnNonNullInstance()
    {
        using var algorithm = Serpent128.Create();

        Assert.IsNotNull(algorithm);
    }

    /// <summary>
    /// Verifies that <see cref="Serpent256.Create" /> returns a non-null instance.
    /// </summary>
    [TestMethod]
    public void Serpent256_Create_ShouldReturnNonNullInstance()
    {
        using var algorithm = Serpent256.Create();

        Assert.IsNotNull(algorithm);
    }

    /// <summary>
    /// Verifies that <see cref="Serpent512.Create" /> returns a non-null instance.
    /// </summary>
    [TestMethod]
    public void Serpent512_Create_ShouldReturnNonNullInstance()
    {
        using var algorithm = Serpent512.Create();

        Assert.IsNotNull(algorithm);
    }

    /// <summary>
    /// Verifies that <see cref="Serpent1024.Create" /> returns a non-null instance.
    /// </summary>
    [TestMethod]
    public void Serpent1024_Create_ShouldReturnNonNullInstance()
    {
        using var algorithm = Serpent1024.Create();

        Assert.IsNotNull(algorithm);
    }

    /// <summary>
    /// Verifies that <see cref="AsconXof128.Create" /> returns a non-null instance, exercising the inherited
    /// generic static factory.
    /// </summary>
    [TestMethod]
    public void AsconXof128_Create_ShouldReturnNonNullInstance()
    {
        using AsconXof128 algorithm = AsconXof<AsconXof128>.Create();

        Assert.IsNotNull(algorithm);
    }
}
