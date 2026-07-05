// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CountMinSketchTests.Export.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Probabilistic;

public partial class CountMinSketchTests
{
    /// <summary>
    /// Verifies that <see cref="CountMinSketch{T}.GetExportByteCount" /> reports the documented layout size: a
    /// 33-byte header followed by 8 bytes per counter cell.
    /// </summary>
    [TestMethod]
    public void GetExportByteCount_WhenCalled_ShouldReturnHeaderPlusCellBytes()
    {
        var sketch = new CountMinSketch<int>(0.01, 0.01);

        Assert.AreEqual(33 + (sketch.Width * sketch.Depth * 8), sketch.GetExportByteCount());
    }

    /// <summary>
    /// Verifies that <see cref="CountMinSketch{T}.TryExport" /> succeeds with an exactly sized buffer and reports the
    /// full export size as written.
    /// </summary>
    [TestMethod]
    public void TryExport_WhenDestinationIsExactSize_ShouldReturnTrueAndWriteAllBytes()
    {
        var sketch = new CountMinSketch<int>(0.01, 0.01);
        sketch.Add(42, 7);
        var destination = new byte[sketch.GetExportByteCount()];

        var result = sketch.TryExport(destination, out var bytesWritten);

        Assert.IsTrue(result);
        Assert.AreEqual(sketch.GetExportByteCount(), bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="CountMinSketch{T}.TryExport" /> returns <see langword="false" /> with zero bytes
    /// written when the destination is one byte too small.
    /// </summary>
    [TestMethod]
    public void TryExport_WhenDestinationTooSmall_ShouldReturnFalseAndWriteNothing()
    {
        var sketch = new CountMinSketch<int>(0.01, 0.01);
        var destination = new byte[sketch.GetExportByteCount() - 1];

        var result = sketch.TryExport(destination, out var bytesWritten);

        Assert.IsFalse(result);
        Assert.AreEqual(0, bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="CountMinSketch{T}.Export" /> throws <see cref="ArgumentException" /> with the
    /// expected parameter name when the destination is too small.
    /// </summary>
    [TestMethod]
    public void Export_WhenDestinationTooSmall_ShouldThrowArgumentException()
    {
        var sketch = new CountMinSketch<int>(0.01, 0.01);
        var destination = new byte[sketch.GetExportByteCount() - 1];

        ThrowsExactlyWithParamName<ArgumentException>(() =>
        {
            sketch.Export(destination);
        }, "destination");
    }

    /// <summary>
    /// Verifies that <see cref="CountMinSketch{T}.Export" /> and <see cref="CountMinSketch{T}.TryExport" /> produce
    /// identical bytes for the same sketch state.
    /// </summary>
    [TestMethod]
    public void Export_WhenComparedToTryExport_ShouldProduceIdenticalBytes()
    {
        var sketch = new CountMinSketch<int>(0.01, 0.01);
        for (var i = 0; i < 50; i++)
            sketch.Add(i, i + 1);

        var viaExport = new byte[sketch.GetExportByteCount()];
        var viaTryExport = new byte[sketch.GetExportByteCount()];

        sketch.Export(viaExport);
        Assert.IsTrue(sketch.TryExport(viaTryExport, out _));

        CollectionAssert.AreEqual(viaExport, viaTryExport);
    }

    /// <summary>
    /// Verifies that an export/import round trip preserves estimates, geometry, error parameters, and the running
    /// total: the imported sketch answers <see cref="CountMinSketch{T}.EstimateCount" /> identically to the source
    /// for both counted and unseen items. Int keys are used because their hash codes are process-stable.
    /// </summary>
    [TestMethod]
    public void Import_WhenGivenExportedState_ShouldPreserveEstimatesAndGeometry()
    {
        var source = new CountMinSketch<int>(0.01, 0.01);
        for (var i = 0; i < 1000; i++)
            source.Add(i * 3, 1 + (i % 25));

        var exported = new byte[source.GetExportByteCount()];
        source.Export(exported);

        var imported = CountMinSketch<int>.Import(exported);

        Assert.AreEqual(source.Width, imported.Width);
        Assert.AreEqual(source.Depth, imported.Depth);
        Assert.AreEqual(source.Epsilon, imported.Epsilon);
        Assert.AreEqual(source.Delta, imported.Delta);
        Assert.AreEqual(source.TotalCount, imported.TotalCount);

        for (var i = 0; i < 3000; i++)
            Assert.AreEqual(source.EstimateCount(i), imported.EstimateCount(i), $"Estimate mismatch for {i}.");
    }

    /// <summary>
    /// Verifies that <see cref="CountMinSketch{T}.Import" /> associates the supplied comparer with the imported
    /// sketch.
    /// </summary>
    [TestMethod]
    public void Import_WhenComparerSupplied_ShouldExposeThatComparer()
    {
        var source = new CountMinSketch<string>(0.01, 0.01, StringComparer.OrdinalIgnoreCase);
        var exported = new byte[source.GetExportByteCount()];
        source.Export(exported);

        var imported = CountMinSketch<string>.Import(exported, StringComparer.OrdinalIgnoreCase);

        Assert.AreSame(StringComparer.OrdinalIgnoreCase, imported.Comparer);
    }

    /// <summary>
    /// Verifies that <see cref="CountMinSketch{T}.Import" /> throws <see cref="ArgumentException" /> with the
    /// expected parameter name when the source is truncated, whether below the header size or mid-way through the
    /// cell data.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(10)]
    [DataRow(32)]
    [DataRow(40)]
    public void Import_WhenSourceIsTruncated_ShouldThrowArgumentException(int truncatedLength)
    {
        var source = new CountMinSketch<int>(0.01, 0.01);
        var exported = new byte[source.GetExportByteCount()];
        source.Export(exported);

        ThrowsExactlyWithParamName<ArgumentException>(() =>
        {
            _ = CountMinSketch<int>.Import(exported.AsSpan(0, truncatedLength));
        }, "source");
    }

    /// <summary>
    /// Verifies that <see cref="CountMinSketch{T}.Import" /> throws <see cref="ArgumentException" /> when the source
    /// carries trailing bytes beyond the declared cell data.
    /// </summary>
    [TestMethod]
    public void Import_WhenSourceHasTrailingBytes_ShouldThrowArgumentException()
    {
        var source = new CountMinSketch<int>(0.01, 0.01);
        var exported = new byte[source.GetExportByteCount() + 1];
        source.Export(exported.AsSpan(0, source.GetExportByteCount()));

        ThrowsExactlyWithParamName<ArgumentException>(() =>
        {
            _ = CountMinSketch<int>.Import(exported);
        }, "source");
    }

    /// <summary>
    /// Verifies that <see cref="CountMinSketch{T}.Import" /> throws <see cref="ArgumentException" /> when the version
    /// byte does not match the supported export format version.
    /// </summary>
    [TestMethod]
    public void Import_WhenVersionByteIsUnsupported_ShouldThrowArgumentException()
    {
        var source = new CountMinSketch<int>(0.01, 0.01);
        var exported = new byte[source.GetExportByteCount()];
        source.Export(exported);
        exported[0] = 2;

        ThrowsExactlyWithParamName<ArgumentException>(() =>
        {
            _ = CountMinSketch<int>.Import(exported);
        }, "source");
    }

    /// <summary>
    /// Verifies that <see cref="CountMinSketch{T}.Import" /> throws <see cref="ArgumentException" /> when the header
    /// describes an invalid sketch — a corrupted width field of zero.
    /// </summary>
    [TestMethod]
    public void Import_WhenWidthFieldIsCorrupted_ShouldThrowArgumentException()
    {
        var source = new CountMinSketch<int>(0.01, 0.01);
        var exported = new byte[source.GetExportByteCount()];
        source.Export(exported);
        exported[1] = 0;
        exported[2] = 0;
        exported[3] = 0;
        exported[4] = 0;

        ThrowsExactlyWithParamName<ArgumentException>(() =>
        {
            _ = CountMinSketch<int>.Import(exported);
        }, "source");
    }

    /// <summary>
    /// Verifies that <see cref="CountMinSketch{T}.Import" /> throws <see cref="ArgumentException" /> when the header
    /// carries a corrupted, negative total count.
    /// </summary>
    [TestMethod]
    public void Import_WhenTotalCountFieldIsNegative_ShouldThrowArgumentException()
    {
        var source = new CountMinSketch<int>(0.01, 0.01);
        var exported = new byte[source.GetExportByteCount()];
        source.Export(exported);
        exported[32] = 0x80;

        ThrowsExactlyWithParamName<ArgumentException>(() =>
        {
            _ = CountMinSketch<int>.Import(exported);
        }, "source");
    }
}
