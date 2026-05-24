// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CrcStandardTests.Equals.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
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
    /// Verifies that changing any single field between two <see cref="CrcStandard" /> instances causes
    /// <see cref="CrcStandard.Equals(CrcStandard?)" /> to return <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void Equals_WhenAnySingleFieldDiffers_ShouldReturnFalse()
    {
        CrcStandard baseStandard = CreateReference();

        Assert.IsFalse(baseStandard.Equals(CreateReference(name: "Other")));
        Assert.IsFalse(baseStandard.Equals(CreateReference(size: 16)));
        Assert.IsFalse(baseStandard.Equals(CreateReference(polynomial: 0x1021UL)));
        Assert.IsFalse(baseStandard.Equals(CreateReference(initialValue: 0UL)));
        Assert.IsFalse(baseStandard.Equals(CreateReference(reflectIn: false)));
        Assert.IsFalse(baseStandard.Equals(CreateReference(reflectOut: false)));
        Assert.IsFalse(baseStandard.Equals(CreateReference(xOrOut: 0UL)));
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
