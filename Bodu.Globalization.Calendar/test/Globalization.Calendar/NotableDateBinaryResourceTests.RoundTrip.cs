// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateBinaryResourceTests.RoundTrip.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class NotableDateBinaryResourceTests
{
    /// <summary>
    /// Verifies that the comprehensive synthetic resource — every strategy, recurrence, and duration discriminator
    /// plus the full policy and applicability field surface — survives a write/read cycle with canonical-form
    /// equality: re-encoding the decoded resource reproduces the original bytes exactly.
    /// </summary>
    [TestMethod]
    public void WriteRead_WhenEveryDiscriminatorExercised_ShouldRoundTripCanonically()
    {
        NotableDateResource original = ComprehensiveResource();

        byte[] firstPack = WritePack(original);
        NotableDateResource reloaded = ReadPack(firstPack);
        byte[] secondPack = WritePack(reloaded);

        CollectionAssert.AreEqual(firstPack, secondPack, "Re-encoding the decoded resource must reproduce the original bytes.");
        Assert.AreEqual(original.ResourceId, reloaded.ResourceId);
        Assert.AreEqual(original.SchemaVersion, reloaded.SchemaVersion);
        Assert.AreEqual(original.AdjustmentPolicies.Count, reloaded.AdjustmentPolicies.Count);
        Assert.AreEqual(original.NotableDates.Count, reloaded.NotableDates.Count);
    }

    /// <summary>
    /// Verifies that the decoded comprehensive resource resolves identically to the original.
    /// </summary>
    [TestMethod]
    public void WriteRead_WhenEveryDiscriminatorExercised_ShouldPreserveResolution()
    {
        NotableDateResource original = ComprehensiveResource();

        NotableDateResource reloaded = ReadPack(WritePack(original));

        CollectionAssert.AreEqual(Fingerprint(original), Fingerprint(reloaded));
    }

    /// <summary>
    /// Verifies that writing the same resource twice produces identical bytes — the stability contract build systems
    /// rely on for up-to-date checks.
    /// </summary>
    [TestMethod]
    public void Write_WhenCalledTwiceForSameResource_ShouldProduceIdenticalBytes()
    {
        NotableDateResource resource = ComprehensiveResource();

        CollectionAssert.AreEqual(WritePack(resource), WritePack(resource));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateResourceLoader.LoadBinary(Stream)" /> loads a pack equivalently to reading
    /// it directly.
    /// </summary>
    [TestMethod]
    public void LoadBinary_WhenGivenPack_ShouldMatchDirectRead()
    {
        byte[] pack = WritePack(ComprehensiveResource());

        using MemoryStream stream = new(pack);
        NotableDateResource loaded = NotableDateResourceLoader.LoadBinary(stream);

        CollectionAssert.AreEqual(Fingerprint(ReadPack(pack)), Fingerprint(loaded));
    }

    /// <summary>
    /// Verifies that every bundled common catalogue round-trips through the binary format with resolved-occurrence
    /// parity and byte-stable output.
    /// </summary>
    /// <param name="catalog">The bundled catalogue to round-trip.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(BundledCatalogues))]
    public void WriteRead_WhenBundledCatalogue_ShouldRoundTripWithResolutionParity(CommonNotableDateCatalog catalog)
    {
        NotableDateResource direct = CommonNotableDateResources.Load(catalog);

        byte[] pack = WritePack(direct);
        NotableDateResource reloaded = ReadPack(pack);

        CollectionAssert.AreEqual(WritePack(direct), pack, $"{catalog}: pack output is not byte-stable");
        CollectionAssert.AreEqual(WritePack(reloaded), pack, $"{catalog}: re-encoding the decoded resource diverged");
        Assert.AreEqual(direct.ResourceId, reloaded.ResourceId);
        CollectionAssert.AreEqual(Fingerprint(direct), Fingerprint(reloaded), $"{catalog}: resolved occurrences differ");
    }

    /// <summary>
    /// Supplies every bundled catalogue as a test row.
    /// </summary>
    public static IEnumerable<object[]> BundledCatalogues =>
        Enum.GetValues<CommonNotableDateCatalog>().Select(catalog => new object[] { catalog });

    /// <summary>
    /// Verifies the argument guards on the write and read entry points.
    /// </summary>
    [TestMethod]
    public void WriteRead_WhenArgumentsAreNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            NotableDateBinaryResource.Write(null!, new MemoryStream());
        });
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            NotableDateBinaryResource.Write(ComprehensiveResource(), null!);
        });
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NotableDateBinaryResource.Read(null!);
        });
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NotableDateResourceLoader.LoadBinary(null!);
        });
    }
}
