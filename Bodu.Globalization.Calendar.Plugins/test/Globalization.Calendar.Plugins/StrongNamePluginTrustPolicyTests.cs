// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StrongNamePluginTrustPolicyTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Plugins;

/// <summary>
/// Verifies that <see cref="StrongNamePluginTrustPolicy" /> trusts an assembly only when its public-key token is in the
/// allowlist, matches tokens case-insensitively, and rejects unsigned assemblies.
/// </summary>
[TestClass]
public sealed partial class StrongNamePluginTrustPolicyTests
{
    /// <summary>
    /// A sample public-key token in lowercase hexadecimal.
    /// </summary>
    private const string Token = "b77a5c561934e089";
}
