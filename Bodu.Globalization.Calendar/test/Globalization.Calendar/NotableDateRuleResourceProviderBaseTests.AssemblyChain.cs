// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleResourceProviderBaseTests.AssemblyChain.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the diagnostic surface of <see cref="NotableDateRuleResourceProviderBase" /> when an embedded resource
/// cannot be found across a multi-assembly chain. This exercises the per-assembly lookup in
/// <c>OpenManifestResourceStream</c> and the <c>FormatAssemblyChain</c> projection lambda used to render the
/// not-found error message.
/// </summary>
[TestClass]
public sealed class NotableDateRuleResourceProviderBaseAssemblyChainTests
{
    /// <summary>
    /// Verifies that loading a resource that does not exist in any assembly in a two-assembly chain throws a
    /// <see cref="FileNotFoundException" /> whose message includes the full names of both assemblies (i.e. exercises
    /// the multi-assembly join path inside <c>FormatAssemblyChain</c>).
    /// </summary>
    [TestMethod]
    public void LoadRules_WhenResourceMissingFromMultipleAssemblies_ShouldThrowExactly()
    {
        // Two distinct loaded assemblies — neither contains the requested embedded resource.
        Assembly first = typeof(NotableDateRule).Assembly;
        Assembly second = typeof(NotableDateRuleResourceProviderBaseAssemblyChainTests).Assembly;

        XmlResourceNotableDateRuleProvider provider = new(
            "Definitely/Not/Real/missing.xml",
            new ResourcePathResolver(),
            new[] { first, second });

        FileNotFoundException ex = Assert.ThrowsExactly<FileNotFoundException>(() =>
        {
            _ = provider.LoadRules().ToList();
        });

        Assert.IsNotNull(first.FullName);
        Assert.IsNotNull(second.FullName);
        Assert.IsTrue(ex.Message.Contains(first.FullName!, StringComparison.Ordinal),
            $"Expected the error to mention assembly '{first.FullName}', got: {ex.Message}");
        Assert.IsTrue(ex.Message.Contains(second.FullName!, StringComparison.Ordinal),
            $"Expected the error to mention assembly '{second.FullName}', got: {ex.Message}");
    }

    /// <summary>
    /// Verifies that a single-assembly chain renders just the one assembly name in the not-found error message,
    /// exercising the count-one short-circuit branch of <c>FormatAssemblyChain</c>.
    /// </summary>
    [TestMethod]
    public void LoadRules_WhenResourceMissingFromSingleAssembly_ShouldThrowExactly()
    {
        Assembly sole = typeof(NotableDateRuleResourceProviderBaseAssemblyChainTests).Assembly;

        XmlResourceNotableDateRuleProvider provider = new(
            "Definitely/Not/Real/missing.xml",
            new ResourcePathResolver(),
            new[] { sole });

        FileNotFoundException ex = Assert.ThrowsExactly<FileNotFoundException>(() =>
        {
            _ = provider.LoadRules().ToList();
        });

        Assert.IsNotNull(sole.FullName);
        Assert.IsTrue(ex.Message.Contains(sole.FullName!, StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that constructing an XML provider with an empty assembly chain throws
    /// <see cref="ArgumentException" /> with <c>assemblies</c> as the offending parameter.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenAssemblyChainIsEmpty_ShouldThrowExactly()
    {
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new XmlResourceNotableDateRuleProvider(
                "any.xml",
                new ResourcePathResolver(),
                Array.Empty<Assembly>());
        });

        Assert.AreEqual("assemblies", ex.ParamName);
    }

    /// <summary>
    /// Verifies that constructing an XML provider with a chain that contains a <see langword="null" /> entry throws
    /// <see cref="ArgumentException" /> with <c>assemblies</c> as the offending parameter.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenAssemblyChainContainsNullEntry_ShouldThrowExactly()
    {
        Assembly real = typeof(NotableDateRuleResourceProviderBaseAssemblyChainTests).Assembly;

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new XmlResourceNotableDateRuleProvider(
                "any.xml",
                new ResourcePathResolver(),
                new Assembly[] { real, null! });
        });

        Assert.AreEqual("assemblies", ex.ParamName);
    }
}
