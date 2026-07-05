// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BloomFilterTests.Export.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Probabilistic;

public partial class BloomFilterTests
{
    /// <summary>
    /// Verifies that <see cref="BloomFilter{T}.GetExportByteCount" /> reports the documented layout size: a 21-byte
    /// header followed by 8 bytes per 64-bit word of the bit array.
    /// </summary>
    [TestMethod]
    public void GetExportByteCount_WhenCalled_ShouldReturnHeaderPlusWordBytes()
    {
        var filter = new BloomFilter<int>(1000, 0.01);
        var expectedWordCount = (filter.BitCount + 63) / 64;

        Assert.AreEqual(21 + (expectedWordCount * 8), filter.GetExportByteCount());
    }

    /// <summary>
    /// Verifies that <see cref="BloomFilter{T}.TryExport" /> succeeds with an exactly sized buffer and reports the
    /// full export size as written.
    /// </summary>
    [TestMethod]
    public void TryExport_WhenDestinationIsExactSize_ShouldReturnTrueAndWriteAllBytes()
    {
        var filter = new BloomFilter<int>(100, 0.01);
        filter.Add(42);
        var destination = new byte[filter.GetExportByteCount()];

        var result = filter.TryExport(destination, out var bytesWritten);

        Assert.IsTrue(result);
        Assert.AreEqual(filter.GetExportByteCount(), bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="BloomFilter{T}.TryExport" /> returns <see langword="false" /> with zero bytes written
    /// when the destination is one byte too small.
    /// </summary>
    [TestMethod]
    public void TryExport_WhenDestinationTooSmall_ShouldReturnFalseAndWriteNothing()
    {
        var filter = new BloomFilter<int>(100, 0.01);
        var destination = new byte[filter.GetExportByteCount() - 1];

        var result = filter.TryExport(destination, out var bytesWritten);

        Assert.IsFalse(result);
        Assert.AreEqual(0, bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="BloomFilter{T}.Export" /> throws <see cref="ArgumentException" /> with the expected
    /// parameter name when the destination is too small.
    /// </summary>
    [TestMethod]
    public void Export_WhenDestinationTooSmall_ShouldThrowArgumentException()
    {
        var filter = new BloomFilter<int>(100, 0.01);
        var destination = new byte[filter.GetExportByteCount() - 1];

        ThrowsExactlyWithParamName<ArgumentException>(() =>
        {
            filter.Export(destination);
        }, "destination");
    }

    /// <summary>
    /// Verifies that <see cref="BloomFilter{T}.Export" /> and <see cref="BloomFilter{T}.TryExport" /> produce
    /// identical bytes for the same filter state.
    /// </summary>
    [TestMethod]
    public void Export_WhenComparedToTryExport_ShouldProduceIdenticalBytes()
    {
        var filter = new BloomFilter<int>(100, 0.01);
        for (var i = 0; i < 50; i++)
            filter.Add(i);

        var viaExport = new byte[filter.GetExportByteCount()];
        var viaTryExport = new byte[filter.GetExportByteCount()];

        filter.Export(viaExport);
        Assert.IsTrue(filter.TryExport(viaTryExport, out _));

        CollectionAssert.AreEqual(viaExport, viaTryExport);
    }

    /// <summary>
    /// Verifies that an export/import round trip preserves membership and geometry: the imported filter answers
    /// <see cref="BloomFilter{T}.MightContain" /> identically to the source for both added and absent items. Int keys
    /// are used because their hash codes are process-stable.
    /// </summary>
    [TestMethod]
    public void Import_WhenGivenExportedState_ShouldPreserveMembershipAndGeometry()
    {
        var source = new BloomFilter<int>(1000, 0.01);
        for (var i = 0; i < 1000; i++)
            source.Add(i * 3);

        var exported = new byte[source.GetExportByteCount()];
        source.Export(exported);

        var imported = BloomFilter<int>.Import(exported);

        Assert.AreEqual(source.BitCount, imported.BitCount);
        Assert.AreEqual(source.HashCount, imported.HashCount);
        Assert.AreEqual(source.ExpectedItems, imported.ExpectedItems);
        Assert.AreEqual(source.DesignFalsePositiveRate, imported.DesignFalsePositiveRate);
        Assert.AreEqual(source.EstimatedFalsePositiveRate, imported.EstimatedFalsePositiveRate);

        for (var i = 0; i < 3000; i++)
            Assert.AreEqual(source.MightContain(i), imported.MightContain(i), $"Membership mismatch for {i}.");
    }

    /// <summary>
    /// Verifies that <see cref="BloomFilter{T}.Import" /> associates the supplied comparer with the imported filter.
    /// </summary>
    [TestMethod]
    public void Import_WhenComparerSupplied_ShouldExposeThatComparer()
    {
        var source = new BloomFilter<string>(100, 0.01, StringComparer.OrdinalIgnoreCase);
        var exported = new byte[source.GetExportByteCount()];
        source.Export(exported);

        var imported = BloomFilter<string>.Import(exported, StringComparer.OrdinalIgnoreCase);

        Assert.AreSame(StringComparer.OrdinalIgnoreCase, imported.Comparer);
    }

    /// <summary>
    /// Verifies that <see cref="BloomFilter{T}.Import" /> throws <see cref="ArgumentException" /> with the expected
    /// parameter name when the source is truncated, whether below the header size or mid-way through the word data.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(10)]
    [DataRow(20)]
    [DataRow(25)]
    public void Import_WhenSourceIsTruncated_ShouldThrowArgumentException(int truncatedLength)
    {
        var source = new BloomFilter<int>(100, 0.01);
        var exported = new byte[source.GetExportByteCount()];
        source.Export(exported);

        ThrowsExactlyWithParamName<ArgumentException>(() =>
        {
            _ = BloomFilter<int>.Import(exported.AsSpan(0, truncatedLength));
        }, "source");
    }

    /// <summary>
    /// Verifies that <see cref="BloomFilter{T}.Import" /> throws <see cref="ArgumentException" /> when the source
    /// carries trailing bytes beyond the declared word data.
    /// </summary>
    [TestMethod]
    public void Import_WhenSourceHasTrailingBytes_ShouldThrowArgumentException()
    {
        var source = new BloomFilter<int>(100, 0.01);
        var exported = new byte[source.GetExportByteCount() + 1];
        source.Export(exported.AsSpan(0, source.GetExportByteCount()));

        ThrowsExactlyWithParamName<ArgumentException>(() =>
        {
            _ = BloomFilter<int>.Import(exported);
        }, "source");
    }

    /// <summary>
    /// Verifies that <see cref="BloomFilter{T}.Import" /> throws <see cref="ArgumentException" /> when the version
    /// byte does not match the supported export format version.
    /// </summary>
    [TestMethod]
    public void Import_WhenVersionByteIsUnsupported_ShouldThrowArgumentException()
    {
        var source = new BloomFilter<int>(100, 0.01);
        var exported = new byte[source.GetExportByteCount()];
        source.Export(exported);
        exported[0] = 2;

        ThrowsExactlyWithParamName<ArgumentException>(() =>
        {
            _ = BloomFilter<int>.Import(exported);
        }, "source");
    }

    /// <summary>
    /// Verifies that <see cref="BloomFilter{T}.Import" /> throws <see cref="ArgumentException" /> when the header
    /// describes an invalid filter — a corrupted bit count of zero.
    /// </summary>
    [TestMethod]
    public void Import_WhenBitCountFieldIsCorrupted_ShouldThrowArgumentException()
    {
        var source = new BloomFilter<int>(100, 0.01);
        var exported = new byte[source.GetExportByteCount()];
        source.Export(exported);
        exported[1] = 0;
        exported[2] = 0;
        exported[3] = 0;
        exported[4] = 0;

        ThrowsExactlyWithParamName<ArgumentException>(() =>
        {
            _ = BloomFilter<int>.Import(exported);
        }, "source");
    }
}
