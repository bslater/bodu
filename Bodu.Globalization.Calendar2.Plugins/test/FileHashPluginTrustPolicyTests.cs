// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FileHashPluginTrustPolicyTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2.Plugins;

/// <summary>
/// Verifies that <see cref="FileHashPluginTrustPolicy" /> trusts an assembly only when its file hash matches the digest
/// pinned under its name, and rejects mismatches, unpinned assemblies, and contexts without a hash.
/// </summary>
[TestClass]
public sealed class FileHashPluginTrustPolicyTests
{
    /// <summary>
    /// A sample pinned digest.
    /// </summary>
    private static readonly byte[] Hash = { 1, 2, 3, 4 };

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
        PluginTrustContext context = new("Plug", "/x/Plug.dll", new byte[] { 9, 9, 9, 9 });

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
    /// Verifies that a <see langword="null" /> allowlist throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenMapIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new FileHashPluginTrustPolicy(null!);
        });
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
