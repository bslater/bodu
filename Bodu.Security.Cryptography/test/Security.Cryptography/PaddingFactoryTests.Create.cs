// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PaddingFactoryTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public partial class PaddingFactoryTests
{
    /// <summary>
    /// Verifies that <see cref="PaddingFactory.Create(PaddingMode)" /> rejects an undefined
    /// <see cref="PaddingMode" /> value with a clean exception message.
    /// </summary>
    [TestMethod]
    public void Create_WhenPaddingModeIsInvalid_ShouldThrowWithCleanMessage()
    {
        CryptographicException ex = Assert.ThrowsExactly<CryptographicException>(() =>
        {
            PaddingFactory.Create((PaddingMode)999);
        });
        Assert.IsFalse(ex.Message.Contains("this."), "Exception message must not contain 'this.' artifact.");
    }

    /// <summary>
    /// Verifies that <see cref="PaddingFactory.Create(BoduPaddingMode)" /> rejects an
    /// undefined <see cref="BoduPaddingMode" /> value with a clean exception message.
    /// </summary>
    [TestMethod]
    public void Create_WhenBoduPaddingModeIsInvalid_ShouldThrowWithCleanMessage()
    {
        CryptographicException ex = Assert.ThrowsExactly<CryptographicException>(() =>
        {
            PaddingFactory.Create((BlockPaddingMode)999);
        });
        Assert.IsFalse(ex.Message.Contains("this."), "Exception message must not contain 'this.' artifact.");
    }

    /// <summary>
    /// Verifies that <see cref="PaddingFactory.Create(PaddingMode)" /> returns a
    /// <see cref="Pkcs7Padding" /> for <see cref="PaddingMode.PKCS7" />.
    /// </summary>
    [TestMethod]
    public void Create_WhenPaddingModeIsPkcs7_ShouldReturnPkcs7Padding()
    {
        IPaddingStrategy strategy = PaddingFactory.Create(PaddingMode.PKCS7);
        Assert.IsInstanceOfType<Pkcs7Padding>(strategy);
    }

    /// <summary>
    /// Verifies that <see cref="PaddingFactory.Create(PaddingMode)" /> returns a
    /// <see cref="ZeroPadding" /> for <see cref="PaddingMode.Zeros" />.
    /// </summary>
    [TestMethod]
    public void Create_WhenPaddingModeIsZeros_ShouldReturnZeroPadding()
    {
        IPaddingStrategy strategy = PaddingFactory.Create(PaddingMode.Zeros);
        Assert.IsInstanceOfType<ZeroPadding>(strategy);
    }

    /// <summary>
    /// Verifies that <see cref="PaddingFactory.Create(PaddingMode)" /> returns a
    /// <see cref="NoPadding" /> for <see cref="PaddingMode.None" />.
    /// </summary>
    [TestMethod]
    public void Create_WhenPaddingModeIsNone_ShouldReturnNoPadding()
    {
        IPaddingStrategy strategy = PaddingFactory.Create(PaddingMode.None);
        Assert.IsInstanceOfType<NoPadding>(strategy);
    }

    /// <summary>
    /// Verifies that <see cref="PaddingFactory.Create(PaddingMode)" /> returns an
    /// <see cref="Ansix923Padding" /> for <see cref="PaddingMode.ANSIX923" />.
    /// </summary>
    [TestMethod]
    public void Create_WhenPaddingModeIsAnsix923_ShouldReturnAnsix923Padding()
    {
        IPaddingStrategy strategy = PaddingFactory.Create(PaddingMode.ANSIX923);
        Assert.IsInstanceOfType<Ansix923Padding>(strategy);
    }

    /// <summary>
    /// Verifies that <see cref="PaddingFactory.Create(PaddingMode)" /> returns an
    /// <see cref="Iso10126Padding" /> for <see cref="PaddingMode.ISO10126" />.
    /// </summary>
    [TestMethod]
    public void Create_WhenPaddingModeIsIso10126_ShouldReturnIso10126Padding()
    {
        IPaddingStrategy strategy = PaddingFactory.Create(PaddingMode.ISO10126);
        Assert.IsInstanceOfType<Iso10126Padding>(strategy);
    }

    /// <summary>
    /// Verifies that <see cref="PaddingFactory.Create(BoduPaddingMode)" /> returns an
    /// <see cref="Iso7816_4Padding" /> for <see cref="BoduPaddingMode.ISO7816_4" />.
    /// </summary>
    [TestMethod]
    public void Create_WhenBoduPaddingModeIsIso7816_4_ShouldReturnIso7816_4Padding()
    {
        IPaddingStrategy strategy = PaddingFactory.Create(BlockPaddingMode.ISO7816_4);
        Assert.IsInstanceOfType<Iso7816_4Padding>(strategy);
    }
}
