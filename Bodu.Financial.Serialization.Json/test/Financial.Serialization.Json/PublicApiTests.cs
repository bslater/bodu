// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PublicApiTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.Financial.Serialization.Json;

/// <summary>
/// Guards the public surface of the <c>Bodu.Financial.Serialization.Json</c> companion assembly against unintended
/// change.
/// </summary>
[TestClass]
public class PublicApiTests
{
    /// <summary>
    /// Verifies that the public API of <c>Bodu.Financial.Serialization.Json</c> matches its committed baseline, so a
    /// new or changed public or protected member fails the build until the baseline is deliberately regenerated.
    /// </summary>
    [TestMethod]
    public void PublicApi_ShouldMatchApprovedBaseline() =>
        PublicApiSnapshot.Verify(typeof(FinancialJsonPolicy).Assembly, "Bodu.Financial.Serialization.Json.PublicApi.txt");
}
