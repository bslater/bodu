// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CrcTests.Properties.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

public partial class CrcTests
{
    /// <summary>
    /// Verifies that each publicly exposed property on <see cref="Crc" /> mirrors the configured
    /// <see cref="CrcStandard" /> for every representative variant — the full parameter surface
    /// (<see cref="Crc.CrcStandard" />, <see cref="Crc.Name" />, <see cref="Crc.Size" />,
    /// <see cref="Crc.Polynomial" />, <see cref="Crc.InitialValue" />, <see cref="Crc.ReflectIn" />,
    /// <see cref="Crc.ReflectOut" />, <see cref="Crc.XOrOut" />).
    /// </summary>
    /// <param name="variant">The CRC variant under test.</param>
    /// <param name="expectedStandardPropertyName">
    /// The name of the static property on <see cref="CrcStandard" /> that returns the expected standard instance;
    /// used to retrieve the canonical <see cref="CrcStandard" /> for comparison.
    /// </param>
    [DataTestMethod]
    [DataRow(CrcTestVariant.Crc8_SMBUS)]
    [DataRow(CrcTestVariant.Crc16_ARC)]
    [DataRow(CrcTestVariant.Crc32_IsoHdlc)]
    [DataRow(CrcTestVariant.Crc64_Ecma182)]
    public void Properties_WhenConstructedWithStandard_ShouldMirrorStandardValues(CrcTestVariant variant)
    {
        CrcStandard standard = variant switch
        {
            CrcTestVariant.Crc8_SMBUS => CrcStandard.CRC8_SMBUS,
            CrcTestVariant.Crc16_ARC => CrcStandard.CRC16_ARC,
            CrcTestVariant.Crc32_IsoHdlc => CrcStandard.CRC32_ISOHDLC,
            CrcTestVariant.Crc64_Ecma182 => CrcStandard.CRC64_ECMA182,
            _ => throw new System.ArgumentOutOfRangeException(nameof(variant), variant, null),
        };

        Crc crc = new(standard);

        Assert.AreSame(standard, crc.CrcStandard);
        Assert.AreEqual(standard.Name, crc.Name);
        Assert.AreEqual(standard.Size, crc.Size);
        Assert.AreEqual(standard.Polynomial, crc.Polynomial);
        Assert.AreEqual(standard.InitialValue, crc.InitialValue);
        Assert.AreEqual(standard.ReflectIn, crc.ReflectIn);
        Assert.AreEqual(standard.ReflectOut, crc.ReflectOut);
        Assert.AreEqual(standard.XOrOut, crc.XOrOut);
    }
}
