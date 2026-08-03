// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstCryptMethodTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst;

/// <summary>
/// Verifies that <see cref="PstCryptMethod" /> pins the header's <c>bCryptMethod</c> values.
/// </summary>
[TestClass]
public class PstCryptMethodTests
{
    /// <summary>
    /// Verifies the numeric value of each member against the specification.
    /// </summary>
    /// <param name="testName">The specification constant the row pins.</param>
    /// <param name="method">The enum member.</param>
    /// <param name="expected">The specification value.</param>
    [TestMethod]
    [DataRow("NDB_CRYPT_NONE", PstCryptMethod.None, 0x00)]
    [DataRow("NDB_CRYPT_PERMUTE", PstCryptMethod.Permute, 0x01)]
    [DataRow("NDB_CRYPT_CYCLIC", PstCryptMethod.Cyclic, 0x02)]
    [DataRow("NDB_CRYPT_EDPCRYPTED", PstCryptMethod.WindowsInformationProtection, 0x10)]
    public void Values_WhenComparedToSpecification_ShouldMatch(string testName, PstCryptMethod method, int expected)
    {
        Assert.AreEqual(expected, (int)method, testName);
    }
}
