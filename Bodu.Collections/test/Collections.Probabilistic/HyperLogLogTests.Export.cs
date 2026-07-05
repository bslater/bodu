// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HyperLogLogTests.Export.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Probabilistic;

public partial class HyperLogLogTests
{
    /// <summary>
    /// Verifies that <see cref="HyperLogLog{T}.GetExportByteCount" /> reports the documented layout size: a 2-byte
    /// header (version + precision) followed by one byte per register.
    /// </summary>
    [TestMethod]
    public void GetExportByteCount_WhenCalled_ShouldReturnHeaderPlusRegisterBytes()
    {
        var sketch = new HyperLogLog<int>(precision: 10);

        Assert.AreEqual(2 + sketch.RegisterCount, sketch.GetExportByteCount());
    }

    /// <summary>
    /// Verifies that <see cref="HyperLogLog{T}.TryExport" /> succeeds with an exactly sized buffer and reports the
    /// full export size as written.
    /// </summary>
    [TestMethod]
    public void TryExport_WhenDestinationIsExactSize_ShouldReturnTrueAndWriteAllBytes()
    {
        var sketch = new HyperLogLog<int>(precision: 10);
        sketch.Add(42);
        var destination = new byte[sketch.GetExportByteCount()];

        var result = sketch.TryExport(destination, out var bytesWritten);

        Assert.IsTrue(result);
        Assert.AreEqual(sketch.GetExportByteCount(), bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="HyperLogLog{T}.TryExport" /> returns <see langword="false" /> with zero bytes written
    /// when the destination is one byte too small.
    /// </summary>
    [TestMethod]
    public void TryExport_WhenDestinationTooSmall_ShouldReturnFalseAndWriteNothing()
    {
        var sketch = new HyperLogLog<int>(precision: 10);
        var destination = new byte[sketch.GetExportByteCount() - 1];

        var result = sketch.TryExport(destination, out var bytesWritten);

        Assert.IsFalse(result);
        Assert.AreEqual(0, bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="HyperLogLog{T}.Export" /> throws <see cref="ArgumentException" /> with the expected
    /// parameter name when the destination is too small.
    /// </summary>
    [TestMethod]
    public void Export_WhenDestinationTooSmall_ShouldThrowArgumentException()
    {
        var sketch = new HyperLogLog<int>(precision: 10);
        var destination = new byte[sketch.GetExportByteCount() - 1];

        ThrowsExactlyWithParamName<ArgumentException>(() =>
        {
            sketch.Export(destination);
        }, "destination");
    }

    /// <summary>
    /// Verifies that <see cref="HyperLogLog{T}.Export" /> and <see cref="HyperLogLog{T}.TryExport" /> produce
    /// identical bytes for the same sketch state.
    /// </summary>
    [TestMethod]
    public void Export_WhenComparedToTryExport_ShouldProduceIdenticalBytes()
    {
        var sketch = new HyperLogLog<int>(precision: 10);
        for (var i = 0; i < 500; i++)
            sketch.Add(i);

        var viaExport = new byte[sketch.GetExportByteCount()];
        var viaTryExport = new byte[sketch.GetExportByteCount()];

        sketch.Export(viaExport);
        Assert.IsTrue(sketch.TryExport(viaTryExport, out _));

        CollectionAssert.AreEqual(viaExport, viaTryExport);
    }

    /// <summary>
    /// Verifies that an export/import round trip preserves the estimate exactly — the registers are copied verbatim —
    /// along with the precision, register count, and standard error. Int keys are used because their hash codes are
    /// process-stable.
    /// </summary>
    [TestMethod]
    public void Import_WhenGivenExportedState_ShouldPreserveEstimateExactly()
    {
        var source = new HyperLogLog<int>(precision: 12);
        for (var i = 0; i < 10_000; i++)
            source.Add(i * 3);

        var exported = new byte[source.GetExportByteCount()];
        source.Export(exported);

        var imported = HyperLogLog<int>.Import(exported);

        Assert.AreEqual(source.Precision, imported.Precision);
        Assert.AreEqual(source.RegisterCount, imported.RegisterCount);
        Assert.AreEqual(source.StandardError, imported.StandardError);
        Assert.AreEqual(source.EstimateCardinality(), imported.EstimateCardinality());
    }

    /// <summary>
    /// Verifies that an imported sketch continues to track the source's stream: adding the same further elements to
    /// both produces identical estimates.
    /// </summary>
    [TestMethod]
    public void Import_WhenGivenExportedState_ShouldContinueTrackingIdentically()
    {
        var source = new HyperLogLog<int>(precision: 10);
        for (var i = 0; i < 1000; i++)
            source.Add(i);
        var exported = new byte[source.GetExportByteCount()];
        source.Export(exported);
        var imported = HyperLogLog<int>.Import(exported);

        for (var i = 1000; i < 2000; i++)
        {
            source.Add(i);
            imported.Add(i);
        }

        Assert.AreEqual(source.EstimateCardinality(), imported.EstimateCardinality());
    }

    /// <summary>
    /// Verifies that <see cref="HyperLogLog{T}.Import" /> associates the supplied comparer with the imported sketch.
    /// </summary>
    [TestMethod]
    public void Import_WhenComparerSupplied_ShouldExposeThatComparer()
    {
        var source = new HyperLogLog<string>(10, StringComparer.OrdinalIgnoreCase);
        var exported = new byte[source.GetExportByteCount()];
        source.Export(exported);

        var imported = HyperLogLog<string>.Import(exported, StringComparer.OrdinalIgnoreCase);

        Assert.AreSame(StringComparer.OrdinalIgnoreCase, imported.Comparer);
    }

    /// <summary>
    /// Verifies that <see cref="HyperLogLog{T}.Import" /> throws <see cref="ArgumentException" /> with the expected
    /// parameter name when the source is truncated, whether below the header size or mid-way through the register
    /// data.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(100)]
    public void Import_WhenSourceIsTruncated_ShouldThrowArgumentException(int truncatedLength)
    {
        var source = new HyperLogLog<int>(precision: 10);
        var exported = new byte[source.GetExportByteCount()];
        source.Export(exported);

        ThrowsExactlyWithParamName<ArgumentException>(() =>
        {
            _ = HyperLogLog<int>.Import(exported.AsSpan(0, truncatedLength));
        }, "source");
    }

    /// <summary>
    /// Verifies that <see cref="HyperLogLog{T}.Import" /> throws <see cref="ArgumentException" /> when the source
    /// carries trailing bytes beyond the declared register data.
    /// </summary>
    [TestMethod]
    public void Import_WhenSourceHasTrailingBytes_ShouldThrowArgumentException()
    {
        var source = new HyperLogLog<int>(precision: 10);
        var exported = new byte[source.GetExportByteCount() + 1];
        source.Export(exported.AsSpan(0, source.GetExportByteCount()));

        ThrowsExactlyWithParamName<ArgumentException>(() =>
        {
            _ = HyperLogLog<int>.Import(exported);
        }, "source");
    }

    /// <summary>
    /// Verifies that <see cref="HyperLogLog{T}.Import" /> throws <see cref="ArgumentException" /> when the version
    /// byte does not match the supported export format version.
    /// </summary>
    [TestMethod]
    public void Import_WhenVersionByteIsUnsupported_ShouldThrowArgumentException()
    {
        var source = new HyperLogLog<int>(precision: 10);
        var exported = new byte[source.GetExportByteCount()];
        source.Export(exported);
        exported[0] = 2;

        ThrowsExactlyWithParamName<ArgumentException>(() =>
        {
            _ = HyperLogLog<int>.Import(exported);
        }, "source");
    }

    /// <summary>
    /// Verifies that <see cref="HyperLogLog{T}.Import" /> throws <see cref="ArgumentException" /> when the precision
    /// byte lies outside the supported [4, 18] interval.
    /// </summary>
    [TestMethod]
    [DataRow((byte)3)]
    [DataRow((byte)19)]
    [DataRow((byte)255)]
    public void Import_WhenPrecisionByteIsCorrupted_ShouldThrowArgumentException(byte corruptedPrecision)
    {
        var source = new HyperLogLog<int>(precision: 10);
        var exported = new byte[source.GetExportByteCount()];
        source.Export(exported);
        exported[1] = corruptedPrecision;

        ThrowsExactlyWithParamName<ArgumentException>(() =>
        {
            _ = HyperLogLog<int>.Import(exported);
        }, "source");
    }

    /// <summary>
    /// Verifies that <see cref="HyperLogLog{T}.Import" /> throws <see cref="ArgumentException" /> when a register
    /// value exceeds the maximum attainable rank for the declared precision — 64 − b + 1 — indicating corruption that
    /// would silently skew subsequent estimates.
    /// </summary>
    [TestMethod]
    public void Import_WhenRegisterValueExceedsMaximumRank_ShouldThrowArgumentException()
    {
        var source = new HyperLogLog<int>(precision: 10);
        var exported = new byte[source.GetExportByteCount()];
        source.Export(exported);
        exported[2] = 56; // Max rank at precision 10 is 64 - 10 + 1 = 55.

        ThrowsExactlyWithParamName<ArgumentException>(() =>
        {
            _ = HyperLogLog<int>.Import(exported);
        }, "source");
    }
}
