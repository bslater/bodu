// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CrcTests.Ctors.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Checksums;

public partial class CrcTests
{

    /// <summary>
    /// Verifies that calling the default constructor selects the CRC-32/ISO-HDLC standard.
    /// </summary>
    [TestMethod]
    public void Ctor_Default_ShouldUseCRC32_ISOHDLC()
    {
        Crc crc = new();
        Assert.AreEqual(CrcStandard.CRC32_ISOHDLC.Name, crc.CrcStandard.Name);
    }

    /// <summary>
    /// Verifies that passing a <see langword="null" /> <see cref="CrcStandard" /> to the constructor throws
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenStandardIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new Crc(null!);
        });
    }

}