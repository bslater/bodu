// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateDocumentBuilderTests.SaveBinary.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Builder;

public partial class NotableDateDocumentBuilderTests
{
    /// <summary>
    /// Builds a small authored document for the binary compilation tests.
    /// </summary>
    /// <returns>The document builder.</returns>
    private static NotableDateDocumentBuilder BinarySampleDocument() =>
        NotableDateDocumentBuilder.Create("demo.binary")
            .AddNotableDate("new-years-day", "New Year's Day", NotableDateCategory.PublicHoliday, d => d
                .AddRule("fixed", r => r.Fixed(1, 1)))
            .AddNotableDate("easter", "Easter Sunday", NotableDateCategory.Religious, d => d
                .AddRule("computus", r => r.Algorithm("western-easter")));

    /// <summary>
    /// Verifies that a compiled pack loads through <see cref="NotableDateResourceLoader.LoadBinary(Stream)" /> and
    /// resolves identically to the directly built resource.
    /// </summary>
    [TestMethod]
    public void SaveBinary_WhenWrittenToStream_ShouldLoadWithResolutionParity()
    {
        NotableDateDocumentBuilder builder = BinarySampleDocument();
        NotableDateResource direct = builder.Build();

        using MemoryStream stream = new();
        builder.SaveBinary(stream);
        stream.Position = 0;
        NotableDateResource loaded = NotableDateResourceLoader.LoadBinary(stream);

        Assert.AreEqual(direct.ResourceId, loaded.ResourceId);
        CollectionAssert.AreEqual(ResolvedSample(direct), ResolvedSample(loaded));
    }

    /// <summary>
    /// Verifies that <c>Save</c> routes a <c>.bcal</c> path — by explicit format and by extension inference — to the
    /// binary compiler, producing a pack that loads from disk.
    /// </summary>
    [TestMethod]
    public void Save_WhenTargetIsBinary_ShouldCompileLoadablePack()
    {
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".bcal");

        try
        {
            BinarySampleDocument().Save(path);

            using FileStream stream = File.OpenRead(path);
            NotableDateResource loaded = NotableDateResourceLoader.LoadBinary(stream);

            Assert.AreEqual("demo.binary", loaded.ResourceId);
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// Verifies that an invalid document fails binary compilation with the same validation exception as
    /// <see cref="NotableDateDocumentBuilder.Build()" /> — nothing invalid can reach a pack.
    /// </summary>
    [TestMethod]
    public void SaveBinary_WhenDocumentFailsValidation_ShouldThrowNotableDateValidationException()
    {
        NotableDateDocumentBuilder builder = NotableDateDocumentBuilder.Create("demo.binary.invalid")
            .AddNotableDate("nd", "ND", NotableDateCategory.Observance, d => d
                .AddRule("r", x => x.Algorithm("no-such-algorithm")));

        using MemoryStream stream = new();

        _ = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            builder.SaveBinary(stream);
        });
    }

    /// <summary>
    /// Verifies that a binary pack cannot be re-opened for editing: it is compiled output, not an authoring source.
    /// </summary>
    [TestMethod]
    public void Load_WhenPathIsBinaryPack_ShouldThrowNotSupportedException()
    {
        _ = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = NotableDateDocumentBuilder.Load("holidays.bcal");
        });
    }
}
