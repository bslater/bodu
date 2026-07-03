// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PublicApiTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.Numerics;

/// <summary>
/// Guards the public surface of the <c>Bodu.Numerics</c> assembly against unintended change now that the package is a
/// stability candidate.
/// </summary>
[TestClass]
public class PublicApiTests
{
    /// <summary>
    /// Verifies that the public API of <c>Bodu.Numerics</c> matches its committed baseline, so a new or changed public
    /// or protected member fails the build until the baseline is deliberately regenerated.
    /// </summary>
    [TestMethod]
    public void PublicApi_ShouldMatchApprovedBaseline() =>
        PublicApiSnapshot.Verify(typeof(Fraction<int>).Assembly, "Bodu.Numerics.PublicApi.txt");
}
