// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FileHashPluginTrustPolicyTests.Evaluate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Plugins;

public sealed partial class FileHashPluginTrustPolicyTests
{
    /// <summary>
    /// Verifies that a matching hash under the pinned name is trusted.
    /// </summary>
    [TestMethod]
    public void Evaluate_WhenHashMatches_ShouldTrust()
    {
        FileHashPluginTrustPolicy policy = new(new Dictionary<string, byte[]> { ["Plug"] = Hash });
        PluginTrustContext context = new("Plug", "/x/Plug.dll", (byte[])Hash.Clone());

        Assert.IsTrue(policy.Evaluate(context).IsTrusted);
    }

    /// <summary>
    /// Verifies that a differing hash is rejected.
    /// </summary>
    [TestMethod]
    public void Evaluate_WhenHashDiffers_ShouldReject()
    {
        FileHashPluginTrustPolicy policy = new(new Dictionary<string, byte[]> { ["Plug"] = Hash });
        PluginTrustContext context = new("Plug", "/x/Plug.dll", [9, 9, 9, 9]);

        Assert.IsFalse(policy.Evaluate(context).IsTrusted);
    }

    /// <summary>
    /// Verifies that an assembly whose name is not pinned is rejected.
    /// </summary>
    [TestMethod]
    public void Evaluate_WhenAssemblyNotPinned_ShouldReject()
    {
        FileHashPluginTrustPolicy policy = new(new Dictionary<string, byte[]> { ["Plug"] = Hash });
        PluginTrustContext context = new("Other", "/x/Other.dll", (byte[])Hash.Clone());

        Assert.IsFalse(policy.Evaluate(context).IsTrusted);
    }

    /// <summary>
    /// Verifies that a context without a file hash (an in-memory assembly) is rejected.
    /// </summary>
    [TestMethod]
    public void Evaluate_WhenNoFileHash_ShouldReject()
    {
        FileHashPluginTrustPolicy policy = new(new Dictionary<string, byte[]> { ["Plug"] = Hash });
        PluginTrustContext context = new("Plug", null, null);

        Assert.IsFalse(policy.Evaluate(context).IsTrusted);
    }

    /// <summary>
    /// Verifies that the assembly name is matched case-insensitively.
    /// </summary>
    [TestMethod]
    public void Evaluate_WhenNameMatchesCaseInsensitively_ShouldTrust()
    {
        FileHashPluginTrustPolicy policy = new(new Dictionary<string, byte[]> { ["Plug"] = Hash });
        PluginTrustContext context = new("PLUG", "/x/Plug.dll", (byte[])Hash.Clone());

        Assert.IsTrue(policy.Evaluate(context).IsTrusted);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> context throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Evaluate_WhenContextIsNull_ShouldThrowArgumentNullException()
    {
        FileHashPluginTrustPolicy policy = new(new Dictionary<string, byte[]>());

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = policy.Evaluate(null!);
        });
    }
}
