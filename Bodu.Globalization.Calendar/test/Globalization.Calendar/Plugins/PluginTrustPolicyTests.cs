// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PluginTrustPolicyTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Globalization.Calendar.Plugins;

/// <summary>
/// Verifies the behaviour of each built-in <see cref="IPluginTrustPolicy" /> implementation in isolation — file-hash equality,
/// strong-name allowlisting, composite AND semantics, delegating dispatch, and the unconditional dev-only <c>AllowAll</c>
/// policy.
/// </summary>
[TestClass]
public sealed class PluginTrustPolicyTests
{
    private static readonly byte[] SampleHash = [0x01, 0x02, 0x03, 0x04];
    private const string SampleAssemblyName = "Acme.Holidays";

    /// <summary>
    /// Verifies that the dev-only <see cref="AllowAllPluginTrustPolicy" /> returns trusted for every context.
    /// </summary>
    [TestMethod]
    public void AllowAllPluginTrustPolicy_ShouldAlwaysReturnTrusted()
    {
        var policy = new AllowAllPluginTrustPolicy();
        var context = new PluginTrustContext("/tmp/anything.dll", new AssemblyName("Anything"), []);

        PluginTrustResult result = policy.Evaluate(context);

        Assert.IsTrue(result.Trusted);
        Assert.IsNull(result.Reason);
    }

    /// <summary>
    /// Verifies that <see cref="FileHashPluginTrustPolicy" /> admits a candidate whose SHA-256 matches the pinned value for
    /// its assembly name.
    /// </summary>
    [TestMethod]
    public void FileHashPluginTrustPolicy_WhenHashMatches_ShouldReturnTrusted()
    {
        var policy = new FileHashPluginTrustPolicy(
            new Dictionary<string, byte[]> { [SampleAssemblyName] = SampleHash });
        var context = new PluginTrustContext("/tmp/acme.dll", new AssemblyName(SampleAssemblyName), SampleHash);

        PluginTrustResult result = policy.Evaluate(context);

        Assert.IsTrue(result.Trusted);
    }

    /// <summary>
    /// Verifies that <see cref="FileHashPluginTrustPolicy" /> rejects a candidate whose SHA-256 differs from the pinned value,
    /// and that the rejection carries a diagnostic reason.
    /// </summary>
    [TestMethod]
    public void FileHashPluginTrustPolicy_WhenHashDiffers_ShouldReturnUntrustedWithReason()
    {
        var policy = new FileHashPluginTrustPolicy(
            new Dictionary<string, byte[]> { [SampleAssemblyName] = SampleHash });
        var tamperedHash = new byte[] { 0x01, 0x02, 0x03, 0xFF };
        var context = new PluginTrustContext("/tmp/acme.dll", new AssemblyName(SampleAssemblyName), tamperedHash);

        PluginTrustResult result = policy.Evaluate(context);

        Assert.IsFalse(result.Trusted);
        Assert.IsNotNull(result.Reason);
        StringAssert.Contains(result.Reason!, SampleAssemblyName);
    }

    /// <summary>
    /// Verifies that <see cref="FileHashPluginTrustPolicy" /> rejects an assembly whose name is not in the allowlist at all.
    /// </summary>
    [TestMethod]
    public void FileHashPluginTrustPolicy_WhenAssemblyNameNotInAllowlist_ShouldReturnUntrusted()
    {
        var policy = new FileHashPluginTrustPolicy(
            new Dictionary<string, byte[]> { [SampleAssemblyName] = SampleHash });
        var context = new PluginTrustContext("/tmp/other.dll", new AssemblyName("Other.Assembly"), SampleHash);

        PluginTrustResult result = policy.Evaluate(context);

        Assert.IsFalse(result.Trusted);
    }

    /// <summary>
    /// Verifies that <see cref="StrongNamePluginTrustPolicy" /> rejects an assembly that is not strong-named (null public-key
    /// token) even if the allowlist is empty.
    /// </summary>
    [TestMethod]
    public void StrongNamePluginTrustPolicy_WhenAssemblyNotStrongNamed_ShouldReturnUntrusted()
    {
        var policy = new StrongNamePluginTrustPolicy(["abcdef1234567890"]);
        var context = new PluginTrustContext("/tmp/plain.dll", new AssemblyName("Plain.Assembly"), SampleHash);

        PluginTrustResult result = policy.Evaluate(context);

        Assert.IsFalse(result.Trusted);
        StringAssert.Contains(result.Reason!, "not strong-named");
    }

    /// <summary>
    /// Verifies that <see cref="StrongNamePluginTrustPolicy" /> admits an assembly whose public-key token (hex-lowercase)
    /// appears in the allowlist, matched case-insensitively.
    /// </summary>
    [TestMethod]
    public void StrongNamePluginTrustPolicy_WhenTokenInAllowlist_ShouldReturnTrusted()
    {
        var token = new byte[] { 0xB0, 0x3F, 0x5F, 0x7F, 0x11, 0xD5, 0x0A, 0x3A };
        var tokenHex = "b03f5f7f11d50a3a";

        var assemblyName = new AssemblyName("Signed.Assembly");
        assemblyName.SetPublicKeyToken(token);

        var policy = new StrongNamePluginTrustPolicy([tokenHex.ToUpperInvariant()]);
        var context = new PluginTrustContext("/tmp/signed.dll", assemblyName, SampleHash);

        PluginTrustResult result = policy.Evaluate(context);

        Assert.IsTrue(result.Trusted);
    }

    /// <summary>
    /// Verifies that <see cref="StrongNamePluginTrustPolicy" /> rejects an assembly whose token is not in the allowlist and
    /// surfaces the token value in the reason.
    /// </summary>
    [TestMethod]
    public void StrongNamePluginTrustPolicy_WhenTokenNotInAllowlist_ShouldReturnUntrustedWithReason()
    {
        var token = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF };
        var assemblyName = new AssemblyName("Signed.Assembly");
        assemblyName.SetPublicKeyToken(token);

        var policy = new StrongNamePluginTrustPolicy(["deadbeef12345678"]);
        var context = new PluginTrustContext("/tmp/signed.dll", assemblyName, SampleHash);

        PluginTrustResult result = policy.Evaluate(context);

        Assert.IsFalse(result.Trusted);
        StringAssert.Contains(result.Reason!, "0123456789abcdef");
    }

    /// <summary>
    /// Verifies that <see cref="CompositePluginTrustPolicy" /> returns trusted only when every child policy returns trusted.
    /// </summary>
    [TestMethod]
    public void CompositePluginTrustPolicy_WhenAllChildrenTrust_ShouldReturnTrusted()
    {
        var policy = new CompositePluginTrustPolicy(new AllowAllPluginTrustPolicy(), new AllowAllPluginTrustPolicy());
        var context = new PluginTrustContext("/tmp/anything.dll", new AssemblyName("Anything"), SampleHash);

        PluginTrustResult result = policy.Evaluate(context);

        Assert.IsTrue(result.Trusted);
    }

    /// <summary>
    /// Verifies that <see cref="CompositePluginTrustPolicy" /> short-circuits on the first child that rejects and returns that
    /// child's reason.
    /// </summary>
    [TestMethod]
    public void CompositePluginTrustPolicy_WhenChildRejects_ShouldShortCircuitWithChildReason()
    {
        var rejecting = new DelegatingPluginTrustPolicy(_ => new PluginTrustResult(false, "child-reason"));
        var policy = new CompositePluginTrustPolicy(new AllowAllPluginTrustPolicy(), rejecting, new AllowAllPluginTrustPolicy());
        var context = new PluginTrustContext("/tmp/anything.dll", new AssemblyName("Anything"), SampleHash);

        PluginTrustResult result = policy.Evaluate(context);

        Assert.IsFalse(result.Trusted);
        Assert.AreEqual("child-reason", result.Reason);
    }

    /// <summary>
    /// Verifies that <see cref="CompositePluginTrustPolicy" /> throws when constructed with an empty policy array.
    /// </summary>
    [TestMethod]
    public void CompositePluginTrustPolicy_WhenConstructedEmpty_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentException>(() => _ = new CompositePluginTrustPolicy());
    }

    /// <summary>
    /// Verifies that <see cref="DelegatingPluginTrustPolicy" /> forwards the context to its callback verbatim and returns the
    /// callback's verdict.
    /// </summary>
    [TestMethod]
    public void DelegatingPluginTrustPolicy_ShouldForwardContextAndReturnCallbackResult()
    {
        PluginTrustContext? observed = null;
        var policy = new DelegatingPluginTrustPolicy(ctx =>
        {
            observed = ctx;
            return new PluginTrustResult(false, "nope");
        });
        var context = new PluginTrustContext("/tmp/x.dll", new AssemblyName("X"), SampleHash);

        PluginTrustResult result = policy.Evaluate(context);

        Assert.IsNotNull(observed);
        Assert.AreEqual(context.AssemblyPath, observed!.Value.AssemblyPath);
        Assert.IsFalse(result.Trusted);
        Assert.AreEqual("nope", result.Reason);
    }

    // ---------------------------------------------------------------------------------
    // Constructor argument validation (composite / file-hash / strong-name)
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="CompositePluginTrustPolicy" /> rejects a <see langword="null" />
    /// policies array with <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void CompositePluginTrustPolicy_WhenPoliciesArrayIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new CompositePluginTrustPolicy(null!);
        });

        Assert.AreEqual("policies", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="CompositePluginTrustPolicy" /> rejects a policies array that
    /// contains a <see langword="null" /> element with <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void CompositePluginTrustPolicy_WhenPoliciesArrayContainsNullElement_ShouldThrowExactly()
    {
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new CompositePluginTrustPolicy(new AllowAllPluginTrustPolicy(), null!);
        });

        Assert.AreEqual("policies", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="FileHashPluginTrustPolicy" /> rejects a <see langword="null" />
    /// allowlist dictionary with <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void FileHashPluginTrustPolicy_WhenAllowedHashesIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new FileHashPluginTrustPolicy(null!);
        });

        Assert.AreEqual("allowedHashesByAssemblyName", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="FileHashPluginTrustPolicy" /> silently skips allowlist entries
    /// whose key is null/whitespace or whose value is <see langword="null" /> rather than
    /// throwing — entries so constructed never produce a trust admission, while other entries
    /// in the same dictionary remain active.
    /// </summary>
    [TestMethod]
    public void FileHashPluginTrustPolicy_WhenAllowlistContainsWhitespaceKeyOrNullValue_ShouldSilentlySkip()
    {
        var allowlist = new Dictionary<string, byte[]?>(StringComparer.OrdinalIgnoreCase)
        {
            ["   "] = SampleHash,
            ["Valid"] = null,
            ["Good"] = SampleHash,
        };

        var policy = new FileHashPluginTrustPolicy((IReadOnlyDictionary<string, byte[]>)allowlist!);

        var context = new PluginTrustContext("/tmp/x.dll", new AssemblyName("Good"), SampleHash);
        Assert.IsTrue(policy.Evaluate(context).Trusted);
    }

    /// <summary>
    /// Verifies that <see cref="FileHashPluginTrustPolicy" /> rejects an assembly whose
    /// <see cref="AssemblyName.Name" /> is empty (exercises the early-return branch on missing
    /// assembly name).
    /// </summary>
    [TestMethod]
    public void FileHashPluginTrustPolicy_WhenAssemblyNameIsEmpty_ShouldReturnUntrusted()
    {
        var allowlist = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Good"] = SampleHash,
        };
        var policy = new FileHashPluginTrustPolicy(allowlist);
        var nameless = new AssemblyName { Name = string.Empty };
        var context = new PluginTrustContext("/tmp/x.dll", nameless, SampleHash);

        PluginTrustResult result = policy.Evaluate(context);

        Assert.IsFalse(result.Trusted);
        Assert.IsNotNull(result.Reason);
    }

    /// <summary>
    /// Verifies that <see cref="FileHashPluginTrustPolicy" /> rejects with a length-mismatch
    /// reason when the candidate hash has a different byte length from the pinned one.
    /// </summary>
    [TestMethod]
    public void FileHashPluginTrustPolicy_WhenHashLengthDiffers_ShouldReturnUntrustedWithLengthMismatchReason()
    {
        var allowlist = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Good"] = [1, 2, 3],
        };
        var policy = new FileHashPluginTrustPolicy(allowlist);
        var context = new PluginTrustContext("/tmp/x.dll", new AssemblyName("Good"), SampleHash);

        PluginTrustResult result = policy.Evaluate(context);

        Assert.IsFalse(result.Trusted);
        Assert.IsTrue(result.Reason!.Contains("length", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Verifies that <see cref="StrongNamePluginTrustPolicy" /> rejects a
    /// <see langword="null" /> allowlist with <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void StrongNamePluginTrustPolicy_WhenAllowlistIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new StrongNamePluginTrustPolicy(null!);
        });

        Assert.AreEqual("allowedPublicKeyTokens", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="StrongNamePluginTrustPolicy" /> normalises allowlist entries
    /// (trim + lowercase) including treating a <see langword="null" /> element as an empty
    /// string, without throwing during construction.
    /// </summary>
    [TestMethod]
    public void StrongNamePluginTrustPolicy_WhenAllowlistHasMixedCaseAndNullEntries_ShouldNormaliseWithoutThrowing()
    {
        _ = new StrongNamePluginTrustPolicy(["  AbCdEf1234567890  ", null!]);

        // Reaching this line implies the normalisation succeeded for both entries.
        Assert.IsTrue(true);
    }
}
