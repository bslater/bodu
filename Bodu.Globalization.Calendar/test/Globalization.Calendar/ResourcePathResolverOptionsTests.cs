// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResourcePathResolverOptionsTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the default state and customisation surface of <see cref="ResourcePathResolverOptions" />.
/// </summary>
[TestClass]
public sealed class ResourcePathResolverOptionsTests
{
    /// <summary>
    /// Verifies that the default instance contains the expected fully qualified resource prefix used by the
    /// embedded calendar payload set.
    /// </summary>
    [TestMethod]
    public void FullyQualifiedResourcePrefixes_WhenDefault_ShouldContainCalendarResourcePrefix()
    {
        ResourcePathResolverOptions options = new();

        Assert.IsTrue(options.FullyQualifiedResourcePrefixes.Contains("Bodu.Globalization.Calendar.Resources."));
    }

    /// <summary>
    /// Verifies that the default <see cref="ResourcePathResolverOptions.FullyQualifiedResourcePrefixes" /> set is
    /// case-sensitive (ordinal comparison) — looking up a lower-cased variant returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void FullyQualifiedResourcePrefixes_WhenDefault_ShouldBeOrdinalCaseSensitive()
    {
        ResourcePathResolverOptions options = new();

        Assert.IsFalse(options.FullyQualifiedResourcePrefixes.Contains("bodu.globalization.calendar.resources."));
    }

    /// <summary>
    /// Verifies that authoring an explicit prefix set replaces the default and is reflected on the property.
    /// </summary>
    [TestMethod]
    public void FullyQualifiedResourcePrefixes_WhenInitialised_ShouldReplaceDefault()
    {
        HashSet<string> custom = new(StringComparer.Ordinal) { "MyCompany.Calendar." };
        ResourcePathResolverOptions options = new()
        {
            FullyQualifiedResourcePrefixes = custom,
        };

        Assert.IsTrue(options.FullyQualifiedResourcePrefixes.Contains("MyCompany.Calendar."));
        Assert.IsFalse(options.FullyQualifiedResourcePrefixes.Contains("Bodu.Globalization.Calendar.Resources."));
    }
}
