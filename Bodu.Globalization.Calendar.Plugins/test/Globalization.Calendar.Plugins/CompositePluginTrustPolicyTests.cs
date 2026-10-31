// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompositePluginTrustPolicyTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Plugins;

/// <summary>
/// Verifies that <see cref="CompositePluginTrustPolicy" /> trusts a candidate only when every composed policy trusts
/// it, surfaces the first rejection without consulting the policies after it, and fails closed when it composes
/// nothing.
/// </summary>
[TestClass]
public sealed partial class CompositePluginTrustPolicyTests
{
    /// <summary>
    /// A context the composed policies decide on.
    /// </summary>
    private static readonly PluginTrustContext Context = new("plugin.asm", "/plugins/plugin.dll", [1, 2, 3, 4]);
}
