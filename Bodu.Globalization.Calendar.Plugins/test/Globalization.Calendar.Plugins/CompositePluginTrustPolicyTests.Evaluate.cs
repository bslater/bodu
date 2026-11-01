// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompositePluginTrustPolicyTests.Evaluate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Plugins;

public sealed partial class CompositePluginTrustPolicyTests
{
    /// <summary>
    /// Verifies that a candidate every composed policy trusts is trusted, every policy having been consulted.
    /// </summary>
    [TestMethod]
    public void Evaluate_WhenEveryPolicyTrusts_ShouldTrust()
    {
        int consulted = 0;
        IPluginTrustPolicy counting = new DelegatingPluginTrustPolicy(_ =>
        {
            consulted++;
            return PluginTrustResult.Trusted();
        });
        CompositePluginTrustPolicy policy = new(new AllowAllPluginTrustPolicy(), counting, counting);

        PluginTrustResult result = policy.Evaluate(Context);

        Assert.IsTrue(result.IsTrusted);
        Assert.IsNull(result.Reason);
        Assert.AreEqual(2, consulted);
    }

    /// <summary>
    /// Verifies that a single trusting policy is enough for the composite to trust the candidate.
    /// </summary>
    [TestMethod]
    public void Evaluate_WhenTheOnlyPolicyTrusts_ShouldTrust()
    {
        CompositePluginTrustPolicy policy = new(new AllowAllPluginTrustPolicy());

        Assert.IsTrue(policy.Evaluate(Context).IsTrusted);
    }

    /// <summary>
    /// Verifies that the first rejection is returned as is and that the policies after it are not consulted.
    /// </summary>
    [TestMethod]
    public void Evaluate_WhenAPolicyRejects_ShouldReturnItsRejectionAndStopConsulting()
    {
        int consultedAfterRejection = 0;
        CompositePluginTrustPolicy policy = new(
            new AllowAllPluginTrustPolicy(),
            new DelegatingPluginTrustPolicy(_ => PluginTrustResult.Rejected("second policy")),
            new DelegatingPluginTrustPolicy(_ =>
            {
                consultedAfterRejection++;
                return PluginTrustResult.Trusted();
            }));

        PluginTrustResult result = policy.Evaluate(Context);

        Assert.IsFalse(result.IsTrusted);
        Assert.AreEqual("second policy", result.Reason);
        Assert.AreEqual(0, consultedAfterRejection);
    }

    /// <summary>
    /// Verifies that a composite with no constituent policies rejects with the no-policies reason rather than
    /// trusting vacuously.
    /// </summary>
    [TestMethod]
    public void Evaluate_WhenNoPolicies_ShouldRejectWithNoPoliciesReason()
    {
        CompositePluginTrustPolicy policy = new();

        PluginTrustResult result = policy.Evaluate(Context);

        Assert.IsFalse(result.IsTrusted);
        Assert.AreEqual(PluginsResourceStrings.Op_NotTrusted_PluginNoPolicies, result.Reason);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> context throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Evaluate_WhenContextIsNull_ShouldThrowArgumentNullException()
    {
        CompositePluginTrustPolicy policy = new(new AllowAllPluginTrustPolicy());

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = policy.Evaluate(null!);
        });
    }
}
