// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateDocumentBuilderTests.SaveLoad.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Builder;

public partial class NotableDateDocumentBuilderTests
{
    /// <summary>
    /// Verifies that a document saved to a <c>.xml</c> file and loaded back resolves identically to the original.
    /// </summary>
    [TestMethod]
    public void SaveLoad_WhenXmlFile_ShouldRoundTripThroughDisk()
    {
        var path = Path.Combine(Path.GetTempPath(), $"bodu-builder-{Guid.NewGuid():N}.xml");
        try
        {
            SampleDocument().Save(path);

            // Load fails loudly if Save did not write the file, so the round-trip assertion alone is sufficient.
            var loaded = NotableDateDocumentBuilder.Load(path);

            CollectionAssert.AreEqual(ResolvedSample(SampleDocument().Build()), ResolvedSample(loaded.Build()));
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// Verifies that a document saved to a <c>.json</c> file and loaded back resolves identically to the original.
    /// </summary>
    [TestMethod]
    public void SaveLoad_WhenJsonFile_ShouldRoundTripThroughDisk()
    {
        var path = Path.Combine(Path.GetTempPath(), $"bodu-builder-{Guid.NewGuid():N}.json");
        try
        {
            SampleDocument().Save(path);

            // Load fails loudly if Save did not write the file, so the round-trip assertion alone is sufficient.
            var loaded = NotableDateDocumentBuilder.Load(path);

            CollectionAssert.AreEqual(ResolvedSample(SampleDocument().Build()), ResolvedSample(loaded.Build()));
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// Verifies that saving to a path with an unrecognized extension throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Save_WhenExtensionUnrecognized_ShouldThrowArgumentException()
    {
        var path = Path.Combine(Path.GetTempPath(), $"bodu-builder-{Guid.NewGuid():N}.txt");

        _ = Assert.ThrowsExactly<ArgumentException>(() => SampleDocument().Save(path));
    }
}
