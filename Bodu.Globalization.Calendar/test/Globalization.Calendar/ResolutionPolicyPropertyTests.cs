// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResolutionPolicyPropertyTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar.RangeResolution;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that the default <see cref="ResolutionPolicy" /> exposes a defined value for each configured policy
/// component.
/// </summary>
[TestClass]
public class ResolutionPolicyPropertyTests
{
    /// <summary>
    /// Verifies that every policy-component getter on the default policy yields a defined enum value.
    /// </summary>
    [TestMethod]
    public void Default_ShouldExposeDefinedPolicyComponents()
    {
        ResolutionPolicy policy = ResolutionPolicy.Default;

        Assert.IsTrue(Enum.IsDefined(policy.DuplicatePolicy));
        Assert.IsTrue(Enum.IsDefined(policy.SameDayCollisionPolicy));
        Assert.IsTrue(Enum.IsDefined(policy.SpanCollisionPolicy));
        Assert.IsTrue(Enum.IsDefined(policy.PriorityDirection));
        Assert.IsTrue(Enum.IsDefined(policy.ObservedDateRangePolicy));
    }
}
