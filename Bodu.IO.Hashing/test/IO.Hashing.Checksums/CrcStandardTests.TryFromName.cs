// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CrcStandardTests.TryFromName.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Verifies <see cref="CrcStandard.TryFromName(string, out CrcStandard)" /> over the aliases the CRC RevEng catalog
/// publishes.
/// </summary>
public partial class CrcStandardTests
{
    /// <summary>
    /// Verifies that <see cref="CrcStandard.TryFromName(string, out CrcStandard)" /> resolves every alias the CRC RevEng
    /// catalog publishes to the shared instance of the standard it names.
    /// </summary>
    /// <param name="kat">The alias and the standard it names.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(PublishedAliases),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void TryFromName_WhenNameIsAPublishedAlias_ShouldReturnTrueAndTheCanonicalInstance(ValidKat<string, CrcStandards> kat)
    {
        bool found = CrcStandard.TryFromName(kat.Input, out CrcStandard? resolved);

        Assert.IsTrue(found, "The result of resolving the alias.");
        Assert.AreSame(CrcStandard.Get(kat.Expected), resolved, "The instance the alias resolves to.");
    }

    /// <summary>
    /// Verifies that <see cref="CrcStandard.TryFromName(string, out CrcStandard)" /> resolves a widely used alias to the
    /// shared instance of the standard it names.
    /// </summary>
    /// <param name="alias">The published alias.</param>
    /// <param name="expected">The standard the alias names.</param>
    [TestMethod]
    [DataRow("PKZIP", CrcStandards.CRC32_ISOHDLC)]
    [DataRow("CRC-32", CrcStandards.CRC32_ISOHDLC)]
    [DataRow("CRC-CCITT", CrcStandards.CRC16_KERMIT)]
    [DataRow("CRC-32C", CrcStandards.CRC32_ISCSI)]
    public void TryFromName_WhenNameIsAWidelyUsedAlias_ShouldReturnTrueAndTheCanonicalInstance(string alias, CrcStandards expected)
    {
        bool found = CrcStandard.TryFromName(alias, out CrcStandard? resolved);

        Assert.IsTrue(found, "The result of resolving the alias.");
        Assert.AreSame(CrcStandard.Get(expected), resolved, "The instance the alias resolves to.");
    }

    /// <summary>
    /// Verifies that <see cref="CrcStandard.TryFromName(string, out CrcStandard)" /> compares aliases ordinally and
    /// case-sensitively, and returns <see langword="false" /> and <see langword="null" /> for an alias in another case.
    /// </summary>
    [TestMethod]
    public void TryFromName_WhenAliasCaseDiffers_ShouldReturnFalseAndNull()
    {
        bool found = CrcStandard.TryFromName("pkzip", out CrcStandard? resolved);

        Assert.IsFalse(found, "The result of resolving the alias in another case.");
        Assert.IsNull(resolved, "The standard produced for the alias in another case.");
    }
}
