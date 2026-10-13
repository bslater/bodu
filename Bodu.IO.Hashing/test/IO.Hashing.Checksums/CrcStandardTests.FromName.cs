// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CrcStandardTests.FromName.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Test.Kat;

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Verifies <see cref="CrcStandard.FromName(string)" /> over the aliases the CRC RevEng catalogue publishes, and the
/// exception it reports for an unknown name.
/// </summary>
public partial class CrcStandardTests
{
    /// <summary>
    /// Verifies that <see cref="CrcStandard.FromName(string)" /> resolves every alias the CRC RevEng catalogue
    /// publishes to the shared instance of the standard it names.
    /// </summary>
    /// <param name="kat">The alias and the standard it names.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(PublishedAliases),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void FromName_WhenNameIsAPublishedAlias_ShouldReturnTheCanonicalInstance(ValidKat<string, CrcStandards> kat)
    {
        CrcStandard resolved = CrcStandard.FromName(kat.Input);

        Assert.AreSame(CrcStandard.Get(kat.Expected), resolved);
    }

    /// <summary>
    /// Verifies that <see cref="CrcStandard.FromName(string)" /> resolves a widely used alias to the shared instance of
    /// the standard it names, which keeps its canonical <see cref="CrcStandard.Name" />.
    /// </summary>
    /// <param name="alias">The published alias.</param>
    /// <param name="expected">The standard the alias names.</param>
    /// <param name="canonicalName">The canonical name of that standard.</param>
    [TestMethod]
    [DataRow("PKZIP", CrcStandards.CRC32_ISOHDLC, "CRC-32/ISO-HDLC")]
    [DataRow("CRC-32", CrcStandards.CRC32_ISOHDLC, "CRC-32/ISO-HDLC")]
    [DataRow("CRC-CCITT", CrcStandards.CRC16_KERMIT, "CRC-16/KERMIT")]
    [DataRow("CRC-16/CCITT-FALSE", CrcStandards.CRC16_IBM3740, "CRC-16/IBM-3740")]
    [DataRow("CRC-32C", CrcStandards.CRC32_ISCSI, "CRC-32/ISCSI")]
    [DataRow("CRC-8", CrcStandards.CRC8_SMBUS, "CRC-8/SMBUS")]
    [DataRow("DOW-CRC", CrcStandards.CRC8_MAXIMDOW, "CRC-8/MAXIM-DOW")]
    [DataRow("CRC-64", CrcStandards.CRC64_ECMA182, "CRC-64/ECMA-182")]
    public void FromName_WhenNameIsAWidelyUsedAlias_ShouldReturnTheCanonicalInstance(string alias, CrcStandards expected, string canonicalName)
    {
        CrcStandard resolved = CrcStandard.FromName(alias);

        Assert.AreSame(CrcStandard.Get(expected), resolved, "The instance the alias resolves to.");
        Assert.AreEqual(canonicalName, resolved.Name, "The name of the resolved standard.");
    }

    /// <summary>
    /// Verifies that <see cref="CrcStandard.FromName(string)" /> still resolves every canonical name to its own entry,
    /// so that no alias shadows a canonical name.
    /// </summary>
    [TestMethod]
    public void FromName_WhenNameIsAnyCanonicalName_ShouldReturnThatEntry()
    {
        foreach (CrcStandards standard in Enum.GetValues<CrcStandards>())
        {
            CrcStandard expected = CrcStandard.Get(standard);

            Assert.AreSame(expected, CrcStandard.FromName(expected.Name), expected.Name);
        }
    }

    /// <summary>
    /// Verifies that <see cref="CrcStandard.FromName(string)" /> compares aliases ordinally and case-sensitively, as it
    /// does canonical names, and throws <see cref="KeyNotFoundException" /> for an alias in another case.
    /// </summary>
    /// <param name="name">A published alias in another case.</param>
    [TestMethod]
    [DataRow("pkzip")]
    [DataRow("Crc-32")]
    [DataRow("crc-ccitt")]
    public void FromName_WhenAliasCaseDiffers_ShouldThrowKeyNotFoundException(string name)
    {
        Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            _ = CrcStandard.FromName(name);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CrcStandard.FromName(string)" /> does not trim a name, and throws
    /// <see cref="KeyNotFoundException" /> for an alias with surrounding white space.
    /// </summary>
    [TestMethod]
    public void FromName_WhenAliasHasSurroundingWhiteSpace_ShouldThrowKeyNotFoundException()
    {
        Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            _ = CrcStandard.FromName(" PKZIP ");
        });
    }

    /// <summary>
    /// Verifies that <see cref="CrcStandard.FromName(string)" /> reports an unknown name with a
    /// <see cref="KeyNotFoundException" /> whose message is the project's resource text formatted with the name.
    /// </summary>
    [TestMethod]
    public void FromName_WhenNameIsUnknown_ShouldThrowWithTheResourceMessage()
    {
        const string name = "no such crc";
        string? format = HashingResourceStrings.ResourceManager.GetString("IO_KeyNotFound_CrcStandardName", HashingResourceStrings.Culture);

        var ex = Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            _ = CrcStandard.FromName(name);
        });

        Assert.IsNotNull(format, "The resource that holds the message.");
        Assert.AreEqual(string.Format(CultureInfo.CurrentCulture, format, name), ex.Message);
    }
}
