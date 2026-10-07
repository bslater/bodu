// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeSerializerOptionsTests.Trimming.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies that <see cref="BencodeSerializerOptions.GetConverter(Type)" />, which resolves and constructs a converter
/// for an arbitrary type, declares <see cref="RequiresUnreferencedCodeAttribute" /> and
/// <see cref="RequiresDynamicCodeAttribute" />, as the serializer entry points that reach it do.
/// </summary>
public partial class BencodeSerializerOptionsTests
{
    /// <summary>
    /// Verifies that <see cref="BencodeSerializerOptions.GetConverter(Type)" /> declares
    /// <see cref="RequiresUnreferencedCodeAttribute" />, so trimming analysis warns where an application calls it.
    /// </summary>
    [TestMethod]
    public void GetConverter_WhenTrimmed_ShouldRequireUnreferencedCode()
    {
        MethodInfo method = typeof(BencodeSerializerOptions).GetMethod(nameof(BencodeSerializerOptions.GetConverter), [typeof(Type)])!;

        Assert.IsTrue(method.IsDefined(typeof(RequiresUnreferencedCodeAttribute), inherit: false));
    }

    /// <summary>
    /// Verifies that <see cref="BencodeSerializerOptions.GetConverter(Type)" /> declares
    /// <see cref="RequiresDynamicCodeAttribute" />, so native AOT analysis warns where an application calls it.
    /// </summary>
    [TestMethod]
    public void GetConverter_WhenCompiledAheadOfTime_ShouldRequireDynamicCode()
    {
        MethodInfo method = typeof(BencodeSerializerOptions).GetMethod(nameof(BencodeSerializerOptions.GetConverter), [typeof(Type)])!;

        Assert.IsTrue(method.IsDefined(typeof(RequiresDynamicCodeAttribute), inherit: false));
    }
}
