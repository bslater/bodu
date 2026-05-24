// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CrcStandardTests.Catalog.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Checksums;

public partial class CrcStandardTests
{

    /// <summary>
    /// Verifies that <see cref="CrcStandard.All" /> returns a non-empty read-only list whose count matches the
    /// number of declared <see cref="CrcStandards" /> values.
    /// </summary>
    [TestMethod]
    public void All_WhenAccessed_ShouldReturnAllCatalogStandards()
    {
        IReadOnlyList<CrcStandard> all = CrcStandard.All;

        Assert.IsNotNull(all);
        Assert.AreEqual(Enum.GetValues<CrcStandards>().Length, all.Count);
    }

    /// <summary>
    /// Verifies that <see cref="CrcStandard.All" /> returns the same cached snapshot reference on repeated calls
    /// so that consumers can rely on stable identity.
    /// </summary>
    [TestMethod]
    public void All_WhenAccessedMultipleTimes_ShouldReturnSameInstance()
    {
        IReadOnlyList<CrcStandard> first = CrcStandard.All;
        IReadOnlyList<CrcStandard> second = CrcStandard.All;

        Assert.AreSame(first, second);
    }

    /// <summary>
    /// Verifies that every entry exposed by <see cref="CrcStandard.All" /> appears in declaration order and is the
    /// same shared instance returned by <see cref="CrcStandard.Get(CrcStandards)" />.
    /// </summary>
    [TestMethod]
    public void All_WhenEnumerated_ShouldMatchGetByOrdinalAndPreserveDeclarationOrder()
    {
        IReadOnlyList<CrcStandard> all = CrcStandard.All;
        CrcStandards[] values = Enum.GetValues<CrcStandards>();

        for (var i = 0; i < values.Length; i++)
        {
            Assert.AreSame(CrcStandard.Get(values[i]), all[i]);
        }
    }

    /// <summary>
    /// Verifies that <see cref="CrcStandard.FromName(string)" /> performs an ordinal case-sensitive comparison and
    /// therefore rejects a lowercased canonical name.
    /// </summary>
    [TestMethod]
    public void FromName_WhenNameCaseDiffers_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            _ = CrcStandard.FromName("crc-32/iso-hdlc");
        });
    }

    /// <summary>
    /// Verifies that <see cref="CrcStandard.FromName(string)" /> returns the canonical instance for a known
    /// catalogue name and that the returned reference is the same as <see cref="CrcStandard.Get(CrcStandards)" />.
    /// </summary>
    /// <param name="name">The canonical catalogue name to resolve.</param>
    /// <param name="expected">The enum value matched by <paramref name="name" />.</param>
    [DataRow("CRC-32/ISO-HDLC", CrcStandards.CRC32_ISOHDLC)]
    [DataRow("CRC-32/ISCSI", CrcStandards.CRC32_ISCSI)]
    [DataRow("CRC-32/BZIP2", CrcStandards.CRC32_BZIP2)]
    [DataRow("CRC-16/MODBUS", CrcStandards.CRC16_MODBUS)]
    [DataRow("CRC-16/KERMIT", CrcStandards.CRC16_KERMIT)]
    [DataRow("CRC-16/IBM-3740", CrcStandards.CRC16_IBM3740)]
    [DataRow("CRC-16/XMODEM", CrcStandards.CRC16_XMODEM)]
    [DataRow("CRC-8/SMBUS", CrcStandards.CRC8_SMBUS)]
    [DataRow("CRC-8/MAXIM-DOW", CrcStandards.CRC8_MAXIMDOW)]
    [DataRow("CRC-64/XZ", CrcStandards.CRC64_XZ)]
    [DataRow("CRC-64/ECMA-182", CrcStandards.CRC64_ECMA182)]
    [DataRow("CRC-3/GSM", CrcStandards.CRC3_GSM)]
    [TestMethod]
    public void FromName_WhenNameIsCanonical_ShouldReturnCachedInstance(string name, CrcStandards expected)
    {
        var resolved = CrcStandard.FromName(name);

        Assert.AreSame(CrcStandard.Get(expected), resolved);
        Assert.AreEqual(name, resolved.Name);
    }

    /// <summary>
    /// Verifies that <see cref="CrcStandard.FromName(string)" /> throws
    /// <see cref="ArgumentNullException" /> with <c>ParamName</c> equal to <c>name</c> when the supplied name is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void FromName_WhenNameIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = CrcStandard.FromName(null!);
        });
        Assert.AreEqual("name", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="CrcStandard.FromName(string)" /> throws
    /// <see cref="KeyNotFoundException" /> when the supplied name does not match any catalogue entry.
    /// </summary>
    [TestMethod]
    public void FromName_WhenNameIsUnknown_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            _ = CrcStandard.FromName("CRC-NOT-A-REAL-STANDARD");
        });
    }

    /// <summary>
    /// Verifies that successive calls to <see cref="CrcStandard.Get(CrcStandards)" /> for the same enum value
    /// return the same cached instance to guarantee reference-equality stability.
    /// </summary>
    [TestMethod]
    public void Get_WhenInvokedRepeatedly_ShouldReturnSameInstance()
    {
        var first = CrcStandard.Get(CrcStandards.CRC32_ISOHDLC);
        var second = CrcStandard.Get(CrcStandards.CRC32_ISOHDLC);

        Assert.AreSame(first, second);
    }

    /// <summary>
    /// Verifies that <see cref="CrcStandard.Get(CrcStandards)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> with <c>ParamName</c> equal to <c>standard</c> when the supplied
    /// enum value is outside the declared catalogue range.
    /// </summary>
    [TestMethod]
    public void Get_WhenStandardIsUndefined_ShouldThrowExactly()
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = CrcStandard.Get((CrcStandards)(-1));
        });
        Assert.AreEqual("standard", ex.ParamName);
    }

    /// <summary>
    /// Verifies that each named static catalogue property returns the canonical instance for its corresponding
    /// <see cref="CrcStandards" /> value, and that its <see cref="CrcStandard.Name" /> matches the expected
    /// catalogue label.
    /// </summary>
    [TestMethod]
    public void StaticProperties_WhenAccessed_ShouldReturnCanonicalCatalogInstances()
    {
        Assert.AreSame(CrcStandard.Get(CrcStandards.CRC8_SMBUS), CrcStandard.CRC8_SMBUS);
        Assert.AreSame(CrcStandard.Get(CrcStandards.CRC8_MAXIMDOW), CrcStandard.CRC8_MAXIMDOW);
        Assert.AreSame(CrcStandard.Get(CrcStandards.CRC16_ARC), CrcStandard.CRC16_ARC);
        Assert.AreSame(CrcStandard.Get(CrcStandards.CRC16_IBM3740), CrcStandard.CRC16_IBM3740);
        Assert.AreSame(CrcStandard.Get(CrcStandards.CRC16_KERMIT), CrcStandard.CRC16_KERMIT);
        Assert.AreSame(CrcStandard.Get(CrcStandards.CRC16_MODBUS), CrcStandard.CRC16_MODBUS);
        Assert.AreSame(CrcStandard.Get(CrcStandards.CRC16_XMODEM), CrcStandard.CRC16_XMODEM);
        Assert.AreSame(CrcStandard.Get(CrcStandards.CRC32_ISOHDLC), CrcStandard.CRC32_ISOHDLC);
        Assert.AreSame(CrcStandard.Get(CrcStandards.CRC32_ISCSI), CrcStandard.CRC32_ISCSI);
        Assert.AreSame(CrcStandard.Get(CrcStandards.CRC32_BZIP2), CrcStandard.CRC32_BZIP2);
        Assert.AreSame(CrcStandard.Get(CrcStandards.CRC64_ECMA182), CrcStandard.CRC64_ECMA182);
        Assert.AreSame(CrcStandard.Get(CrcStandards.CRC64_XZ), CrcStandard.CRC64_XZ);

        Assert.AreEqual("CRC-8/SMBUS", CrcStandard.CRC8_SMBUS.Name);
        Assert.AreEqual("CRC-8/MAXIM-DOW", CrcStandard.CRC8_MAXIMDOW.Name);
        Assert.AreEqual("CRC-16/ARC", CrcStandard.CRC16_ARC.Name);
        Assert.AreEqual("CRC-16/IBM-3740", CrcStandard.CRC16_IBM3740.Name);
        Assert.AreEqual("CRC-16/KERMIT", CrcStandard.CRC16_KERMIT.Name);
        Assert.AreEqual("CRC-16/MODBUS", CrcStandard.CRC16_MODBUS.Name);
        Assert.AreEqual("CRC-16/XMODEM", CrcStandard.CRC16_XMODEM.Name);
        Assert.AreEqual("CRC-32/ISO-HDLC", CrcStandard.CRC32_ISOHDLC.Name);
        Assert.AreEqual("CRC-32/ISCSI", CrcStandard.CRC32_ISCSI.Name);
        Assert.AreEqual("CRC-32/BZIP2", CrcStandard.CRC32_BZIP2.Name);
        Assert.AreEqual("CRC-64/ECMA-182", CrcStandard.CRC64_ECMA182.Name);
        Assert.AreEqual("CRC-64/XZ", CrcStandard.CRC64_XZ.Name);
    }

    /// <summary>
    /// Verifies that <see cref="CrcStandard.TryFromName(string, out CrcStandard)" /> is case-sensitive and rejects a
    /// lowercased canonical name.
    /// </summary>
    [TestMethod]
    public void TryFromName_WhenNameCaseDiffers_ShouldReturnFalse()
    {
        var ok = CrcStandard.TryFromName("crc-16/modbus", out CrcStandard? resolved);

        Assert.IsFalse(ok);
        Assert.IsNull(resolved);
    }

    /// <summary>
    /// Verifies that <see cref="CrcStandard.TryFromName(string, out CrcStandard)" /> returns <see langword="true" />
    /// and produces the canonical instance for a known catalogue name.
    /// </summary>
    [TestMethod]
    public void TryFromName_WhenNameIsCanonical_ShouldReturnTrueAndProduceCanonicalInstance()
    {
        var ok = CrcStandard.TryFromName("CRC-32/ISO-HDLC", out CrcStandard? resolved);

        Assert.IsTrue(ok);
        Assert.IsNotNull(resolved);
        Assert.AreSame(CrcStandard.Get(CrcStandards.CRC32_ISOHDLC), resolved);
    }

    /// <summary>
    /// Verifies that <see cref="CrcStandard.TryFromName(string, out CrcStandard)" /> returns <see langword="false" />
    /// rather than throwing when the supplied name is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void TryFromName_WhenNameIsNull_ShouldReturnFalseAndNull()
    {
        var ok = CrcStandard.TryFromName(null, out CrcStandard? resolved);

        Assert.IsFalse(ok);
        Assert.IsNull(resolved);
    }

    /// <summary>
    /// Verifies that <see cref="CrcStandard.TryFromName(string, out CrcStandard)" /> returns <see langword="false" />
    /// and a <see langword="null" /> standard when the supplied name does not match any catalogue entry.
    /// </summary>
    [TestMethod]
    public void TryFromName_WhenNameIsUnknown_ShouldReturnFalseAndNull()
    {
        var ok = CrcStandard.TryFromName("CRC-NOT-A-REAL-STANDARD", out CrcStandard? resolved);

        Assert.IsFalse(ok);
        Assert.IsNull(resolved);
    }

}
