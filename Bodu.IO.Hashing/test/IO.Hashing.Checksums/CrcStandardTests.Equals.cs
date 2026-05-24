// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CrcStandardTests.Equals.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Checksums;

public partial class CrcStandardTests
{

    /// <summary>
    /// Verifies that two <see cref="CrcStandard" /> instances with identical parameters compare equal and
    /// produce the same hash code.
    /// </summary>
    [TestMethod]
    public void Equals_WhenAllFieldsMatch_ShouldReturnTrueAndEqualHashCode()
    {
        CrcStandard a = CreateReference();
        CrcStandard b = CreateReference();

        Assert.IsTrue(a.Equals(b));
        Assert.IsTrue(a.Equals((object)b));
        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }

    /// <summary>
    /// Verifies that changing any single identity-bearing parameter between two <see cref="CrcStandard" /> instances
    /// causes <see cref="CrcStandard.Equals(CrcStandard?)" /> to return <see langword="false" />. <see cref="CrcStandard.Name" />
    /// is informational and is exercised separately.
    /// </summary>
    [TestMethod]
    public void Equals_WhenAnySingleFieldDiffers_ShouldReturnFalse()
    {
        CrcStandard baseStandard = CreateReference();

        Assert.IsFalse(baseStandard.Equals(CreateReference(size: 16)));
        Assert.IsFalse(baseStandard.Equals(CreateReference(polynomial: 0x1021UL)));
        Assert.IsFalse(baseStandard.Equals(CreateReference(initialValue: 0UL)));
        Assert.IsFalse(baseStandard.Equals(CreateReference(reflectIn: false)));
        Assert.IsFalse(baseStandard.Equals(CreateReference(reflectOut: false)));
        Assert.IsFalse(baseStandard.Equals(CreateReference(xOrOut: 0UL)));
    }

    /// <summary>
    /// Verifies that two <see cref="CrcStandard" /> instances whose CRC parameters all match but whose
    /// <see cref="CrcStandard.Name" /> differs compare equal — the class remarks document <see cref="CrcStandard.Name" />
    /// as informational only.
    /// </summary>
    [TestMethod]
    public void Equals_WhenParametersMatchButNameDiffers_ShouldReturnTrue()
    {
        CrcStandard a = CreateReference(name: "Alpha");
        CrcStandard b = CreateReference(name: "Beta");

        Assert.IsTrue(a.Equals(b));
        Assert.IsTrue(a.Equals((object)b));
    }

    /// <summary>
    /// Verifies that two <see cref="CrcStandard" /> instances whose CRC parameters all match but whose
    /// <see cref="CrcStandard.Name" /> differs produce the same hash code, in keeping with the equality contract.
    /// </summary>
    [TestMethod]
    public void GetHashCode_WhenParametersMatchButNameDiffers_ShouldReturnSameValue()
    {
        CrcStandard a = CreateReference(name: "Alpha");
        CrcStandard b = CreateReference(name: "Beta");

        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }

    /// <summary>
    /// Verifies that inserting two <see cref="CrcStandard" /> instances whose CRC parameters all match but whose
    /// <see cref="CrcStandard.Name" /> differs into a <see cref="HashSet{T}" /> yields a single entry — the standard
    /// consumer-visible consequence of the equality contract.
    /// </summary>
    [TestMethod]
    public void HashSet_WhenInstancesShareParametersButDifferInName_ShouldHoldSingleEntry()
    {
        CrcStandard a = CreateReference(name: "Alpha");
        CrcStandard b = CreateReference(name: "Beta");

        HashSet<CrcStandard> set = new() { a };
        Assert.IsFalse(set.Add(b));
        Assert.AreEqual(1, set.Count);
    }

    /// <summary>
    /// Verifies that <see cref="CrcStandard.Equals(object?)" /> returns <see langword="false" /> when the
    /// comparand is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Equals_WhenObjectOtherIsNull_ShouldReturnFalse()
    {
        CrcStandard a = CreateReference();
        Assert.IsFalse(a.Equals((object?)null));
    }

    /// <summary>
    /// Verifies that <see cref="CrcStandard.Equals(object?)" /> returns <see langword="false" /> when the
    /// comparand is not a <see cref="CrcStandard" />.
    /// </summary>
    [TestMethod]
    public void Equals_WhenObjIsDifferentType_ShouldReturnFalse()
    {
        CrcStandard a = CreateReference();
        Assert.IsFalse(a.Equals("not a CrcStandard"));
    }
    /// <summary>
    /// Verifies that <see cref="CrcStandard.Equals(CrcStandard?)" /> returns <see langword="false" /> when the
    /// comparand is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Equals_WhenTypedOtherIsNull_ShouldReturnFalse()
    {
        CrcStandard a = CreateReference();
        Assert.IsFalse(a.Equals(null));
    }

}
