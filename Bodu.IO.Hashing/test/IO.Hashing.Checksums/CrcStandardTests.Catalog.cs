// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CrcStandardTests.Catalog.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Checksums;

public partial class CrcStandardTests
{
    /// <summary>
    /// Verifies that <see cref="CrcStandard.Get(CrcStandards)" /> returns a non-null instance whose properties
    /// match the canonical <c>CRC-32/ISO-HDLC</c> definition.
    /// </summary>
    [TestMethod]
    public void Get_WhenCalledWithDefinedStandard_ShouldReturnPopulatedInstance()
    {
        CrcStandard standard = CrcStandard.Get(CrcStandards.CRC32_ISOHDLC);

        Assert.IsNotNull(standard);
        Assert.AreEqual("CRC-32/ISO-HDLC", standard.Name);
        Assert.AreEqual(32, standard.Size);
        Assert.AreEqual(0x04C11DB7UL, standard.Polynomial);
        Assert.AreEqual(0xFFFFFFFFUL, standard.InitialValue);
        Assert.IsTrue(standard.ReflectIn);
        Assert.IsTrue(standard.ReflectOut);
        Assert.AreEqual(0xFFFFFFFFUL, standard.XOrOut);
    }

    /// <summary>
    /// Verifies that <see cref="CrcStandard.Get(CrcStandards)" /> returns reference-equal instances on
    /// successive calls for the same enum value, demonstrating the per-entry cache.
    /// </summary>
    [TestMethod]
    public void Get_WhenCalledTwice_ShouldReturnSameInstance()
    {
        CrcStandard first = CrcStandard.Get(CrcStandards.CRC16_ARC);
        CrcStandard second = CrcStandard.Get(CrcStandards.CRC16_ARC);

        Assert.AreSame(first, second);
    }

    /// <summary>
    /// Verifies that <see cref="CrcStandard.Get(CrcStandards)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> with <c>ParamName</c> equal to <c>standard</c> when the supplied
    /// value is not a defined catalogue ordinal.
    /// </summary>
    [TestMethod]
    public void Get_WhenStandardIsUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = CrcStandard.Get((CrcStandards)int.MaxValue);
        });
        Assert.AreEqual("standard", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the static catalogue accessors expose the canonical instance for each documented
    /// standard, and that each accessor agrees with the corresponding <see cref="CrcStandards" /> ordinal.
    /// </summary>
    [TestMethod]
    public void StaticCatalogProperties_ShouldReturnCanonicalInstances()
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
    }

    /// <summary>
    /// Verifies that <see cref="CrcStandard.All" /> exposes one <see cref="CrcStandard" /> instance per defined
    /// catalogue ordinal, and that successive accesses return the same memoised list.
    /// </summary>
    [TestMethod]
    public void All_WhenAccessed_ShouldExposeAllCatalogueEntriesAndBeMemoised()
    {
        IReadOnlyList<CrcStandard> first = CrcStandard.All;
        IReadOnlyList<CrcStandard> second = CrcStandard.All;

        Assert.AreSame(first, second);

        int expectedCount = Enum.GetValues<CrcStandards>().Length;
        Assert.AreEqual(expectedCount, first.Count);

        // Every entry must be a non-null CrcStandard whose Name corresponds to a known canonical entry.
        foreach (CrcStandard entry in first)
        {
            Assert.IsNotNull(entry);
            Assert.IsFalse(string.IsNullOrEmpty(entry.Name));
        }
    }

    /// <summary>
    /// Verifies that every entry returned by <see cref="CrcStandard.All" /> matches the instance obtained from
    /// <see cref="CrcStandard.Get(CrcStandards)" /> at the same ordinal index.
    /// </summary>
    [TestMethod]
    public void All_WhenIndexed_ShouldMatchGetByOrdinal()
    {
        IReadOnlyList<CrcStandard> all = CrcStandard.All;

        for (int i = 0; i < all.Count; i++)
            Assert.AreSame(CrcStandard.Get((CrcStandards)i), all[i]);
    }

    /// <summary>
    /// Verifies that <see cref="CrcStandard.FromName(string)" /> resolves a canonical name to the same instance
    /// as <see cref="CrcStandard.Get(CrcStandards)" />.
    /// </summary>
    [TestMethod]
    public void FromName_WhenNameMatchesCanonical_ShouldReturnSameInstanceAsGet()
    {
        CrcStandard byName = CrcStandard.FromName("CRC-32/ISO-HDLC");
        CrcStandard byEnum = CrcStandard.Get(CrcStandards.CRC32_ISOHDLC);

        Assert.AreSame(byEnum, byName);
    }

    /// <summary>
    /// Verifies that <see cref="CrcStandard.FromName(string)" /> throws <see cref="ArgumentNullException" />
    /// with <c>ParamName</c> equal to <c>name</c> when invoked with <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void FromName_WhenNameIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = CrcStandard.FromName(null!);
        });
        Assert.AreEqual("name", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="CrcStandard.FromName(string)" /> throws
    /// <see cref="KeyNotFoundException" /> when no catalogue entry matches the supplied name.
    /// </summary>
    [TestMethod]
    public void FromName_WhenNameIsUnknown_ShouldThrowKeyNotFoundException()
    {
        Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            _ = CrcStandard.FromName("CRC-NOT-A-REAL-STANDARD");
        });
    }

    /// <summary>
    /// Verifies that <see cref="CrcStandard.TryFromName(string?, out CrcStandard?)" /> returns
    /// <see langword="true" /> and the canonical instance when the name matches a catalogue entry.
    /// </summary>
    [TestMethod]
    public void TryFromName_WhenNameMatches_ShouldReturnTrueAndCanonicalInstance()
    {
        bool found = CrcStandard.TryFromName("CRC-16/ARC", out CrcStandard? standard);

        Assert.IsTrue(found);
        Assert.IsNotNull(standard);
        Assert.AreSame(CrcStandard.Get(CrcStandards.CRC16_ARC), standard);
    }

    /// <summary>
    /// Verifies that <see cref="CrcStandard.TryFromName(string?, out CrcStandard?)" /> returns
    /// <see langword="false" /> and a <see langword="null" /> out value when the name is unknown.
    /// </summary>
    [TestMethod]
    public void TryFromName_WhenNameIsUnknown_ShouldReturnFalseAndNullStandard()
    {
        bool found = CrcStandard.TryFromName("not-a-known-standard", out CrcStandard? standard);

        Assert.IsFalse(found);
        Assert.IsNull(standard);
    }

    /// <summary>
    /// Verifies that <see cref="CrcStandard.TryFromName(string?, out CrcStandard?)" /> returns
    /// <see langword="false" /> and a <see langword="null" /> out value when the name is <see langword="null" />,
    /// rather than throwing.
    /// </summary>
    [TestMethod]
    public void TryFromName_WhenNameIsNull_ShouldReturnFalseAndNullStandard()
    {
        bool found = CrcStandard.TryFromName(null, out CrcStandard? standard);

        Assert.IsFalse(found);
        Assert.IsNull(standard);
    }
}
