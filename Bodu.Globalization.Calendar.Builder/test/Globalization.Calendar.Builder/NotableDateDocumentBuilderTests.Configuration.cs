// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateDocumentBuilderTests.Configuration.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Builder;

public partial class NotableDateDocumentBuilderTests
{
    /// <summary>
    /// Verifies that <see cref="NotableDateDocumentBuilder.WithResourceId(string)" /> replaces the resource identifier
    /// emitted on the root element.
    /// </summary>
    [TestMethod]
    public void WithResourceId_ShouldOverrideResourceIdInSerialization()
    {
        string xml = NotableDateDocumentBuilder.Create("initial")
            .WithResourceId("overridden")
            .AddNotableDate("nd", "ND", NotableDateCategory.Observance, d => d
                .AddRule("r", x => x.Fixed(1, 1)))
            .ToXml();

        Assert.Contains("resourceId=\"overridden\"", xml);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateDocumentBuilder.WithSchemaVersion(string)" /> replaces the schema version
    /// emitted on the root element.
    /// </summary>
    [TestMethod]
    public void WithSchemaVersion_ShouldOverrideSchemaVersionInSerialization()
    {
        string xml = NotableDateDocumentBuilder.Create("demo.schema")
            .WithSchemaVersion("9.9")
            .AddNotableDate("nd", "ND", NotableDateCategory.Observance, d => d
                .AddRule("r", x => x.Fixed(1, 1)))
            .ToXml();

        Assert.Contains("schemaVersion=\"9.9\"", xml);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateDocumentBuilder.ToProvider" /> materializes the document and exposes it as the
    /// provider's current resource.
    /// </summary>
    [TestMethod]
    public void ToProvider_ShouldExposeBuiltResourceAsCurrent()
    {
        INotableDateResourceProvider provider = SampleDocument().ToProvider();

        Assert.AreEqual("demo.sample", provider.Current.ResourceId);
    }
}
