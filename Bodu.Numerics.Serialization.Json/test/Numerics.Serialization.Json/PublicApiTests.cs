// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PublicApiTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.Numerics.Serialization.Json;

/// <summary>
/// Guards the public surface of the <c>Bodu.Numerics.Serialization.Json</c> companion assembly against unintended
/// change.
/// </summary>
[TestClass]
public class PublicApiTests
{
    /// <summary>
    /// Verifies that the public API of <c>Bodu.Numerics.Serialization.Json</c> matches its committed baseline, so a new
    /// or changed public or protected member fails the build until the baseline is deliberately regenerated.
    /// </summary>
    [TestMethod]
    public void PublicApi_ShouldMatchApprovedBaseline() =>
        PublicApiSnapshot.Verify(typeof(NumericsJsonPolicy).Assembly, "Bodu.Numerics.Serialization.Json.PublicApi.txt");
}
