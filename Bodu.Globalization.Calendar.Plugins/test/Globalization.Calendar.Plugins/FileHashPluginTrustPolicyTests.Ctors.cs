// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FileHashPluginTrustPolicyTests.Ctors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Plugins;

public sealed partial class FileHashPluginTrustPolicyTests
{
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
    /// Verifies that entries with a blank assembly name or a <see langword="null" /> digest are ignored, so neither
    /// pins anything, while the well-formed entries beside them are kept.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenEntryHasBlankNameOrNullDigest_ShouldIgnoreIt()
    {
        FileHashPluginTrustPolicy policy = new(new Dictionary<string, byte[]>
        {
            ["   "] = Hash,
            ["Unpinned"] = null!,
            ["Plug"] = Hash,
        });

        Assert.IsTrue(policy.Evaluate(new PluginTrustContext("Plug", "/x/Plug.dll", (byte[])Hash.Clone())).IsTrusted);
        Assert.IsFalse(policy.Evaluate(new PluginTrustContext("   ", "/x/Blank.dll", (byte[])Hash.Clone())).IsTrusted);
        Assert.IsFalse(policy.Evaluate(new PluginTrustContext("Unpinned", "/x/Unpinned.dll", (byte[])Hash.Clone())).IsTrusted);
    }

    /// <summary>
    /// Verifies that an empty map rejects every assembly.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenMapIsEmpty_ShouldRejectEveryAssembly()
    {
        FileHashPluginTrustPolicy policy = new(new Dictionary<string, byte[]>());

        Assert.IsFalse(policy.Evaluate(new PluginTrustContext("Plug", "/x/Plug.dll", (byte[])Hash.Clone())).IsTrusted);
    }
}
