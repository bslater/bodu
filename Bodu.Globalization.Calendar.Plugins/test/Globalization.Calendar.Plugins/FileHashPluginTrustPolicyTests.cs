// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FileHashPluginTrustPolicyTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Plugins;

/// <summary>
/// Verifies that <see cref="FileHashPluginTrustPolicy" /> trusts an assembly only when its file hash matches the digest
/// pinned under its name, and rejects mismatches, unpinned assemblies, and contexts without a hash.
/// </summary>
[TestClass]
public sealed partial class FileHashPluginTrustPolicyTests
{
    /// <summary>
    /// A sample pinned digest.
    /// </summary>
    private static readonly byte[] Hash = [1, 2, 3, 4];
}
