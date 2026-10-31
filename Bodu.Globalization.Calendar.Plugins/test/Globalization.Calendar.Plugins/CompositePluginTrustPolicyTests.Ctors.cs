// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompositePluginTrustPolicyTests.Ctors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Plugins;

public sealed partial class CompositePluginTrustPolicyTests
{
    /// <summary>
    /// Verifies that a <see langword="null" /> policy array throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenPoliciesIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new CompositePluginTrustPolicy(null!);
        });
    }

    /// <summary>
    /// Verifies that the composite copies the policies it is given, so a later change to the caller's array does not
    /// change which policies are consulted.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenPolicyArrayChangesLater_ShouldKeepTheOriginalPolicies()
    {
        IPluginTrustPolicy[] policies = [new AllowAllPluginTrustPolicy()];
        CompositePluginTrustPolicy policy = new(policies);
        policies[0] = new DelegatingPluginTrustPolicy(_ => PluginTrustResult.Rejected("replaced"));

        Assert.IsTrue(policy.Evaluate(Context).IsTrusted);
    }
}
