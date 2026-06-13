// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SignatureValueTests.FromBytes.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class SignatureValueTests
{
    /// <summary>
    /// Verifies that <see cref="SignatureValue.FromBytes" /> records the supplied format and copies the bytes.
    /// </summary>
    /// <param name="format">The wire encoding under test.</param>
    [TestMethod]
    [DataRow(SignatureFormat.Unknown)]
    [DataRow(SignatureFormat.Raw)]
    [DataRow(SignatureFormat.Der)]
    [DataRow(SignatureFormat.P1363)]
    public void FromBytes_WhenFormatIsDefined_ShouldRecordFormatAndBytes(SignatureFormat format)
    {
        var source = new byte[] { 0x30, 0x06, 0x02, 0x01, 0x01, 0x02 };

        var signature = SignatureValue.FromBytes(source, format);

        Assert.AreEqual(format, signature.Format);
        Assert.AreEqual(source.Length, signature.Length);
        CollectionAssert.AreEqual(source, signature.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="SignatureValue.FromBytes" /> copies the input so later mutation of the source
    /// buffer does not change the value.
    /// </summary>
    [TestMethod]
    public void FromBytes_WhenSourceMutatedAfterwards_ShouldNotAffectValue()
    {
        var source = new byte[] { 0x01, 0x02, 0x03 };
        var signature = SignatureValue.FromBytes(source, SignatureFormat.Raw);

        source[0] = 0xFF;

        Assert.AreEqual("010203", signature.ToHexString());
    }

    /// <summary>
    /// Verifies that <see cref="SignatureValue.FromBytes" /> over an empty span produces an empty value that still
    /// carries the supplied format.
    /// </summary>
    [TestMethod]
    public void FromBytes_WhenInputIsEmpty_ShouldProduceEmptyValueWithFormat()
    {
        var signature = SignatureValue.FromBytes([], SignatureFormat.Der);

        Assert.IsTrue(signature.IsEmpty);
        Assert.AreEqual(SignatureFormat.Der, signature.Format);
    }

    /// <summary>
    /// Verifies that <see cref="SignatureValue.FromBytes" /> throws <see cref="ArgumentOutOfRangeException" />
    /// for an undefined <see cref="SignatureFormat" /> value.
    /// </summary>
    [TestMethod]
    public void FromBytes_WhenFormatIsUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = SignatureValue.FromBytes([0x01], (SignatureFormat)99);
        });

        Assert.AreEqual("format", ex.ParamName);
    }
}
