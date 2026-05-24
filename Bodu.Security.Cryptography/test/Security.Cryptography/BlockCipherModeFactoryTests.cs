// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockCipherModeFactoryTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for the <see cref="BlockCipherModeFactory" /> static helper which dispatches
/// classic confidentiality-only cipher modes (ECB / CBC / CFB / OFB / CTR) to their concrete
/// <see cref="IBlockCipherModeTransform" /> implementations.
/// </summary>
[TestClass]
public sealed class BlockCipherModeFactoryTests
{
    /// <summary>
    /// Verifies that <see cref="BlockCipherModeFactory.Create" /> with a <see langword="null" />
    /// cipher throws <see cref="ArgumentNullException" /> with the expected parameter name.
    /// </summary>
    [TestMethod]
    public void Create_WhenCipherIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = BlockCipherModeFactory.Create(CipherModeKind.ECB, null!);
        });

        Assert.AreEqual("cipher", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="BlockCipherModeFactory.Create" /> with an undefined
    /// <see cref="CipherModeKind" /> value throws <see cref="NotSupportedException" /> rather than
    /// dispatching to a default mode.
    /// </summary>
    [TestMethod]
    public void Create_WhenModeIsUndefinedEnumValue_ShouldThrowExactly()
    {
        var cipher = new SkipjackBlockCipher(new byte[10]);

        Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = BlockCipherModeFactory.Create((CipherModeKind)999, cipher, new byte[8]);
        });
    }

    /// <summary>
    /// Verifies that <see cref="BlockCipherModeFactory.Create" /> with <see cref="CipherModeKind.ECB" />
    /// does not require an IV and returns a non-null <see cref="IBlockCipherModeTransform" />.
    /// </summary>
    [TestMethod]
    public void Create_WhenModeIsEcbAndIvIsNull_ShouldNotThrow()
    {
        var cipher = new SkipjackBlockCipher(new byte[10]);

        IBlockCipherModeTransform transform = BlockCipherModeFactory.Create(CipherModeKind.ECB, cipher, iv: null);

        Assert.IsNotNull(transform);
        Assert.IsInstanceOfType<EcbModeTransform>(transform);
    }

    /// <summary>
    /// Verifies that <see cref="BlockCipherModeFactory.Create" /> with a feedback mode and a
    /// <see langword="null" /> IV throws <see cref="ArgumentException" /> with the expected
    /// parameter name.
    /// </summary>
    [TestMethod]
    [DataRow(CipherModeKind.CBC)]
    [DataRow(CipherModeKind.CFB)]
    [DataRow(CipherModeKind.OFB)]
    [DataRow(CipherModeKind.CTR)]
    public void Create_WhenIvRequiredAndIsNull_ShouldThrowExactly(CipherModeKind mode)
    {
        var cipher = new SkipjackBlockCipher(new byte[10]);

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = BlockCipherModeFactory.Create(mode, cipher, iv: null);
        });

        Assert.AreEqual("iv", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="BlockCipherModeFactory.Create" /> with a feedback mode and a
    /// wrong-sized IV throws <see cref="ArgumentException" /> rather than constructing a transform
    /// that would later fail with <see cref="IndexOutOfRangeException" /> on the first block.
    /// </summary>
    [TestMethod]
    [DataRow(CipherModeKind.CBC, 0)]
    [DataRow(CipherModeKind.CBC, 1)]
    [DataRow(CipherModeKind.CBC, 7)]
    [DataRow(CipherModeKind.CBC, 9)]
    [DataRow(CipherModeKind.CBC, 16)]
    [DataRow(CipherModeKind.CFB, 4)]
    [DataRow(CipherModeKind.OFB, 4)]
    [DataRow(CipherModeKind.CTR, 4)]
    public void Create_WhenIvLengthMismatchesBlockSize_ShouldThrowExactly(CipherModeKind mode, int ivLength)
    {
        var cipher = new SkipjackBlockCipher(new byte[10]);

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = BlockCipherModeFactory.Create(mode, cipher, new byte[ivLength]);
        });

        Assert.AreEqual("iv", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="BlockCipherModeFactory.Create" /> with the documented set of
    /// confidentiality-only modes returns the corresponding concrete transform type.
    /// </summary>
    [TestMethod]
    public void Create_ForEachSupportedMode_ShouldReturnExpectedTransformType()
    {
        var cipher = new SkipjackBlockCipher(new byte[10]);
        var iv = new byte[8];

        Assert.IsInstanceOfType<EcbModeTransform>(BlockCipherModeFactory.Create(CipherModeKind.ECB, cipher, iv));
        Assert.IsInstanceOfType<CbcModeTransform>(BlockCipherModeFactory.Create(CipherModeKind.CBC, cipher, iv));
        Assert.IsInstanceOfType<CfbModeTransform>(BlockCipherModeFactory.Create(CipherModeKind.CFB, cipher, iv));
        Assert.IsInstanceOfType<OfbModeTransform>(BlockCipherModeFactory.Create(CipherModeKind.OFB, cipher, iv));
        Assert.IsInstanceOfType<CtrModeTransform>(BlockCipherModeFactory.Create(CipherModeKind.CTR, cipher, iv));
    }

    /// <summary>
    /// Verifies that <see cref="BlockCipherModeFactory.Create" /> rejects every authenticated or
    /// disk-encryption mode that the factory documents as out-of-scope, with
    /// <see cref="NotSupportedException" />. Regression guard for any silent expansion of the
    /// factory's responsibility — those modes have a different contract and lifecycle and must be
    /// constructed directly.
    /// </summary>
    [TestMethod]
    [DataRow(CipherModeKind.XTS)]
    [DataRow(CipherModeKind.OCB)]
    [DataRow(CipherModeKind.EAX)]
    [DataRow(CipherModeKind.SIV)]
    public void Create_WhenModeIsOutOfScope_ShouldThrowExactly(CipherModeKind mode)
    {
        var cipher = new SkipjackBlockCipher(new byte[10]);

        Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = BlockCipherModeFactory.Create(mode, cipher, new byte[8]);
        });
    }
}
