// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CipherPaddingTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Verifies the bidirectional synchronisation between the extended <see cref="PaddingModeKind" /> and the
/// inherited <see cref="System.Security.Cryptography.SymmetricAlgorithm.Padding" /> on cipher classes that
/// expose both surfaces.
/// </summary>
/// <remarks>
/// The setters on <c>BlockPadding</c> and <c>Padding</c> mirror each other when the assigned value has a
/// matching member in the other enumeration, and leave the counterpart unchanged when there is no mapping
/// (for example <see cref="PaddingModeKind.ISO7816_4" /> has no <see cref="PaddingMode" /> equivalent).
/// </remarks>
[TestClass]
public sealed class CipherPaddingTests
{
    /// <summary>
    /// Verifies that setting <see cref="Blowfish.BlockPadding" /> to a value with a matching
    /// <see cref="PaddingMode" /> synchronises the inherited
    /// <see cref="System.Security.Cryptography.SymmetricAlgorithm.Padding" /> getter, and that reading
    /// <see cref="Blowfish.Padding" /> reflects the new value.
    /// </summary>
    [TestMethod]
    public void Blowfish_BlockPadding_WhenSetToMappedValue_ShouldSyncPaddingAndAllowRead()
    {
        using Blowfish algorithm = new();

        algorithm.BlockPadding = PaddingModeKind.PKCS7;

        Assert.AreEqual(PaddingModeKind.PKCS7, algorithm.BlockPadding);
        Assert.AreEqual(PaddingMode.PKCS7, algorithm.Padding);
    }

    /// <summary>
    /// Verifies that setting <see cref="Blowfish.BlockPadding" /> to an extended value without a
    /// <see cref="PaddingMode" /> counterpart (such as <see cref="PaddingModeKind.ISO7816_4" />) updates
    /// <see cref="Blowfish.BlockPadding" /> but leaves the inherited padding unchanged.
    /// </summary>
    [TestMethod]
    public void Blowfish_BlockPadding_WhenSetToUnmappedValue_ShouldNotChangeInheritedPadding()
    {
        using Blowfish algorithm = new();
        PaddingMode original = algorithm.Padding;

        algorithm.BlockPadding = PaddingModeKind.ISO7816_4;

        Assert.AreEqual(PaddingModeKind.ISO7816_4, algorithm.BlockPadding);
        Assert.AreEqual(original, algorithm.Padding);
    }

    /// <summary>
    /// Verifies that setting <see cref="Blowfish.Padding" /> to a value with a matching
    /// <see cref="PaddingModeKind" /> synchronises <see cref="Blowfish.BlockPadding" />.
    /// </summary>
    [TestMethod]
    public void Blowfish_Padding_WhenSetToMappedValue_ShouldSyncBlockPadding()
    {
        using Blowfish algorithm = new();

        algorithm.Padding = PaddingMode.Zeros;

        Assert.AreEqual(PaddingMode.Zeros, algorithm.Padding);
        Assert.AreEqual(PaddingModeKind.Zeros, algorithm.BlockPadding);
    }

    /// <summary>
    /// Verifies the same getter/setter sync behaviour on <see cref="Camellia" /> as on <see cref="Blowfish" />.
    /// </summary>
    [TestMethod]
    public void Camellia_BlockPadding_WhenSetToMappedValue_ShouldSyncPaddingAndAllowRead()
    {
        using Camellia algorithm = new();

        algorithm.BlockPadding = PaddingModeKind.PKCS7;

        Assert.AreEqual(PaddingModeKind.PKCS7, algorithm.BlockPadding);
        Assert.AreEqual(PaddingMode.PKCS7, algorithm.Padding);
    }

    /// <summary>
    /// Verifies that <see cref="Camellia.Padding" /> getter returns the inherited padding mode value.
    /// </summary>
    [TestMethod]
    public void Camellia_Padding_WhenRead_ShouldReturnInheritedPaddingMode()
    {
        using Camellia algorithm = new();

        algorithm.BlockPadding = PaddingModeKind.None;

        Assert.AreEqual(PaddingMode.None, algorithm.Padding);
    }

    /// <summary>
    /// Verifies that <see cref="Camellia.BlockPadding" /> set to <see cref="PaddingModeKind.ISO7816_4" />
    /// leaves the inherited <see cref="Camellia.Padding" /> unchanged.
    /// </summary>
    [TestMethod]
    public void Camellia_BlockPadding_WhenSetToUnmappedValue_ShouldNotChangeInheritedPadding()
    {
        using Camellia algorithm = new();
        PaddingMode original = algorithm.Padding;

        algorithm.BlockPadding = PaddingModeKind.ISO7816_4;

        Assert.AreEqual(PaddingModeKind.ISO7816_4, algorithm.BlockPadding);
        Assert.AreEqual(original, algorithm.Padding);
    }

    /// <summary>
    /// Verifies the same getter/setter sync behaviour on <see cref="Skipjack" /> as on <see cref="Blowfish" />.
    /// </summary>
    [TestMethod]
    public void Skipjack_BlockPadding_WhenSetToMappedValue_ShouldSyncPaddingAndAllowRead()
    {
        using Skipjack algorithm = new();

        algorithm.BlockPadding = PaddingModeKind.PKCS7;

        Assert.AreEqual(PaddingModeKind.PKCS7, algorithm.BlockPadding);
        Assert.AreEqual(PaddingMode.PKCS7, algorithm.Padding);
    }

    /// <summary>
    /// Verifies that <see cref="Skipjack.Padding" /> getter returns the inherited padding mode value.
    /// </summary>
    [TestMethod]
    public void Skipjack_Padding_WhenRead_ShouldReturnInheritedPaddingMode()
    {
        using Skipjack algorithm = new();

        algorithm.BlockPadding = PaddingModeKind.None;

        Assert.AreEqual(PaddingMode.None, algorithm.Padding);
    }

    /// <summary>
    /// Verifies that <see cref="Skipjack.BlockPadding" /> set to <see cref="PaddingModeKind.ISO7816_4" />
    /// leaves the inherited <see cref="Skipjack.Padding" /> unchanged.
    /// </summary>
    [TestMethod]
    public void Skipjack_BlockPadding_WhenSetToUnmappedValue_ShouldNotChangeInheritedPadding()
    {
        using Skipjack algorithm = new();
        PaddingMode original = algorithm.Padding;

        algorithm.BlockPadding = PaddingModeKind.ISO7816_4;

        Assert.AreEqual(PaddingModeKind.ISO7816_4, algorithm.BlockPadding);
        Assert.AreEqual(original, algorithm.Padding);
    }

    /// <summary>
    /// Verifies the same getter/setter sync behaviour on <see cref="Twofish" /> as on <see cref="Blowfish" />.
    /// </summary>
    [TestMethod]
    public void Twofish_BlockPadding_WhenSetToMappedValue_ShouldSyncPaddingAndAllowRead()
    {
        using Twofish algorithm = new();

        algorithm.BlockPadding = PaddingModeKind.PKCS7;

        Assert.AreEqual(PaddingModeKind.PKCS7, algorithm.BlockPadding);
        Assert.AreEqual(PaddingMode.PKCS7, algorithm.Padding);
    }

    /// <summary>
    /// Verifies that <see cref="Twofish.BlockPadding" /> set to <see cref="PaddingModeKind.ISO7816_4" />
    /// leaves the inherited <see cref="Twofish.Padding" /> unchanged.
    /// </summary>
    [TestMethod]
    public void Twofish_BlockPadding_WhenSetToUnmappedValue_ShouldNotChangeInheritedPadding()
    {
        using Twofish algorithm = new();
        PaddingMode original = algorithm.Padding;

        algorithm.BlockPadding = PaddingModeKind.ISO7816_4;

        Assert.AreEqual(PaddingModeKind.ISO7816_4, algorithm.BlockPadding);
        Assert.AreEqual(original, algorithm.Padding);
    }

    /// <summary>
    /// Verifies the same getter/setter sync behaviour on <see cref="Serpent128" /> as on <see cref="Blowfish" />.
    /// </summary>
    [TestMethod]
    public void Serpent128_Padding_WhenSetToMappedValue_ShouldSyncBlockPadding()
    {
        using Serpent128 algorithm = new();

        algorithm.Padding = PaddingMode.Zeros;

        Assert.AreEqual(PaddingMode.Zeros, algorithm.Padding);
    }
}
