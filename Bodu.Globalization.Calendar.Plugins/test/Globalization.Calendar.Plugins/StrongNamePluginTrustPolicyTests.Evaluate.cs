// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StrongNamePluginTrustPolicyTests.Evaluate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Plugins;

public sealed partial class StrongNamePluginTrustPolicyTests
{
    /// <summary>
    /// Verifies that a token in the allowlist is trusted.
    /// </summary>
    [TestMethod]
    public void Evaluate_WhenTokenInAllowlist_ShouldTrust()
    {
        StrongNamePluginTrustPolicy policy = new([Token]);
        PluginTrustContext context = new("Plug", null, null, Token);

        Assert.IsTrue(policy.Evaluate(context).IsTrusted);
    }

    /// <summary>
    /// Verifies that an allowlist token differing only by case still matches.
    /// </summary>
    [TestMethod]
    public void Evaluate_WhenTokenCaseDiffers_ShouldTrust()
    {
        StrongNamePluginTrustPolicy policy = new(["B77A5C561934E089"]);
        PluginTrustContext context = new("Plug", null, null, Token);

        Assert.IsTrue(policy.Evaluate(context).IsTrusted);
    }

    /// <summary>
    /// Verifies that a token absent from the allowlist is rejected.
    /// </summary>
    [TestMethod]
    public void Evaluate_WhenTokenNotInAllowlist_ShouldReject()
    {
        StrongNamePluginTrustPolicy policy = new(["0000000000000000"]);
        PluginTrustContext context = new("Plug", null, null, Token);

        Assert.IsFalse(policy.Evaluate(context).IsTrusted);
    }

    /// <summary>
    /// Verifies that an assembly with no public-key token is rejected.
    /// </summary>
    [TestMethod]
    public void Evaluate_WhenNotStrongNamed_ShouldReject()
    {
        StrongNamePluginTrustPolicy policy = new([Token]);
        PluginTrustContext context = new("Plug", null, null, null);

        Assert.IsFalse(policy.Evaluate(context).IsTrusted);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> context throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Evaluate_WhenContextIsNull_ShouldThrowArgumentNullException()
    {
        StrongNamePluginTrustPolicy policy = new([Token]);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = policy.Evaluate(null!);
        });
    }
}
