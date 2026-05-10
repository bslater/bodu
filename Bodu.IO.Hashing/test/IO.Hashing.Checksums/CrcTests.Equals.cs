// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CrcTests.Equals.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Checksums;

public partial class CrcTests
{
    /// <summary>
    /// Verifies that two <see cref="Crc" /> instances configured with identical <see cref="CrcStandard" /> values
    /// report equal via <see cref="Crc.Equals(object?)" /> and produce matching hash codes.
    /// </summary>
    [TestMethod]
    public void Equals_WhenStandardsMatch_ShouldReturnTrueAndEqualHashCode()
    {
        Crc a = new(CrcStandard.CRC32_ISOHDLC);
        Crc b = new(CrcStandard.CRC32_ISOHDLC);

        Assert.IsTrue(a.Equals(b));
    }

    /// <summary>
    /// Verifies that two <see cref="Crc" /> instances configured with different <see cref="CrcStandard" /> values
    /// report unequal via <see cref="Crc.Equals(object?)" />.
    /// </summary>
    [TestMethod]
    public void Equals_WhenStandardsDiffer_ShouldReturnFalse()
    {
        Crc a = new(CrcStandard.CRC32_ISOHDLC);
        Crc b = new(CrcStandard.CRC32_ISCSI);

        Assert.IsFalse(a.Equals(b));
    }

    /// <summary>
    /// Verifies that <see cref="Crc.Equals(object?)" /> returns <see langword="false" /> when the comparand is
    /// <see langword="null" /> or an object of an unrelated type.
    /// </summary>
    /// <param name="other">The comparand passed to <see cref="Crc.Equals(object?)" />.</param>
    [TestMethod]
    [DataRow(null)]
    [DataRow("not a Crc")]
    public void Equals_WhenObjIsNullOrDifferentType_ShouldReturnFalse(object? other)
    {
        Crc a = new(CrcStandard.CRC32_ISOHDLC);

        Assert.IsFalse(a.Equals(other));
    }
}
