// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlResourceNotableDateRuleProviderTests.Exceptions.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the argument-validation and load-time exception contracts of
/// <see cref="XmlResourceNotableDateRuleProvider" />.
/// </summary>
public sealed partial class XmlResourceNotableDateRuleProviderTests
{
    // -----------------------------------------------------------------------------------------
    // Constructor argument validation
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that the constructor rejects a <see langword="null" /> resource path resolver
    /// with <see cref="ArgumentNullException" /> and exposes the parameter name on the exception.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenResourcePathResolverIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new XmlResourceNotableDateRuleProvider(CommonResource, null!);
        });

        Assert.AreEqual("resourcePathResolver", ex.ParamName);
    }

    // -----------------------------------------------------------------------------------------
    // LoadRules surfaces validation lazily
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="XmlResourceNotableDateRuleProvider.LoadRules" /> surfaces a
    /// resource path that is empty or whitespace as <see cref="ArgumentException" /> via the
    /// internal <c>ToProviderPath</c> validation guard.
    /// </summary>
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("\t")]
    [TestMethod]
    public void LoadRules_WhenResourceNameIsEmptyOrWhitespace_ShouldThrowArgumentException(string resourceName)
    {
        var provider = new XmlResourceNotableDateRuleProvider(resourceName, new ResourcePathResolver());

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = provider.LoadRules().ToList();
        });
    }

    /// <summary>
    /// Verifies that <see cref="XmlResourceNotableDateRuleProvider.LoadRules" /> reports a
    /// missing resource via a <see cref="FileNotFoundException" /> whose message references the
    /// requested resource name to aid diagnosis.
    /// </summary>
    [TestMethod]
    public void LoadRules_WhenResourceMissing_ShouldThrowFileNotFoundExceptionWithMessage()
    {
        var provider = new XmlResourceNotableDateRuleProvider(
            "Bodu/Globalization/Calendar/Resources/Imaginary.xml",
            new ResourcePathResolver());

        var ex = Assert.ThrowsExactly<FileNotFoundException>(() =>
        {
            _ = provider.LoadRules().ToList();
        });

        Assert.IsTrue(
            ex.Message.Contains("Imaginary", StringComparison.Ordinal),
            "FileNotFoundException message should reference the missing resource name.");
    }

    /// <summary>
    /// Verifies that <see cref="XmlResourceNotableDateRuleProvider.LoadRules" /> throws
    /// <see cref="FileNotFoundException" /> when the resource is sought in an assembly that does
    /// not contain it.
    /// </summary>
    [TestMethod]
    public void LoadRules_WhenAssemblyDoesNotContainResource_ShouldThrowFileNotFoundException()
    {
        Assembly fxBaseAssembly = typeof(object).Assembly;
        var provider = new XmlResourceNotableDateRuleProvider(
            CommonResource,
            new ResourcePathResolver(),
            fxBaseAssembly);

        Assert.ThrowsExactly<FileNotFoundException>(() =>
        {
            _ = provider.LoadRules().ToList();
        });
    }
}
