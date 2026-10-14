// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CrcStandardTests.Get.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Verifies the exception <see cref="CrcStandard.Get(CrcStandards)" /> reports for a value outside the catalog.
/// </summary>
public partial class CrcStandardTests
{
    /// <summary>
    /// Verifies that <see cref="CrcStandard.Get(CrcStandards)" /> reports an undefined value with an
    /// <see cref="ArgumentOutOfRangeException" /> for <c>standard</c>, carrying the value, whose message begins with the
    /// project's resource text.
    /// </summary>
    [TestMethod]
    public void Get_WhenStandardIsUndefined_ShouldThrowWithTheResourceMessage()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = CrcStandard.Get((CrcStandards)(-1));
        });

        Assert.AreEqual("standard", ex.ParamName, "The parameter the exception names.");
        Assert.AreEqual((CrcStandards)(-1), ex.ActualValue, "The value the exception carries.");
        Assert.StartsWith(HashingResourceStrings.Arg_OutOfRange_CrcStandardUndefined, ex.Message, "The message.");
    }
}
