// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffReader.Records.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

/// <content> The typed accessors that decode the current record. Each interprets the current record only and maintains
/// no state beyond it. </content>
public ref partial struct BiffReader
{
    /// <summary>
    /// Decodes the current <c>BOF</c> record.
    /// </summary>
    /// <returns>The decoded record.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current record is not a <c>BOF</c> record.
    /// </exception>
    /// <exception cref="BiffFormatException">Thrown when the payload is malformed.</exception>
    public readonly BiffBofRecord GetBof()
    {
        RequireRecord(BiffRecordType.Bof);

        return BiffBofRecord.Read(ValueSpan);
    }

    /// <summary>
    /// Decodes the current <c>BOUNDSHEET</c> record.
    /// </summary>
    /// <returns>The decoded record.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current record is not a <c>BOUNDSHEET</c> record, or the version is unknown.
    /// </exception>
    /// <exception cref="BiffFormatException">Thrown when the payload is malformed.</exception>
    public readonly BiffBoundSheetRecord GetBoundSheet()
    {
        RequireRecord(BiffRecordType.BoundSheet);
        RequireVersion();

        return BiffBoundSheetRecord.Read(ValueSpan, Version, CodePage);
    }

    /// <summary>
    /// Decodes the current <c>DIMENSIONS</c> record.
    /// </summary>
    /// <returns>The decoded record.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current record is not a <c>DIMENSIONS</c> record, or the version is unknown.
    /// </exception>
    /// <exception cref="BiffFormatException">Thrown when the payload is malformed.</exception>
    public readonly BiffDimensionsRecord GetDimensions()
    {
        RequireRecord(BiffRecordType.Dimensions);
        RequireVersion();

        return BiffDimensionsRecord.Read(ValueSpan, Version);
    }

    /// <summary>
    /// Decodes the current <c>ROW</c> record.
    /// </summary>
    /// <returns>The decoded record.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current record is not a <c>ROW</c> record.
    /// </exception>
    /// <exception cref="BiffFormatException">Thrown when the payload is malformed.</exception>
    public readonly BiffRowRecord GetRow()
    {
        RequireRecord(BiffRecordType.Row);

        return BiffRowRecord.Read(ValueSpan);
    }

    /// <summary>
    /// Decodes the current <c>NUMBER</c> record.
    /// </summary>
    /// <returns>The decoded record.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current record is not a <c>NUMBER</c> record.
    /// </exception>
    /// <exception cref="BiffFormatException">Thrown when the payload is malformed.</exception>
    public readonly BiffNumberRecord GetNumber()
    {
        RequireRecord(BiffRecordType.Number);

        return BiffNumberRecord.Read(ValueSpan);
    }

    /// <summary>
    /// Decodes the current <c>RK</c> record.
    /// </summary>
    /// <returns>The decoded record.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current record is not an <c>RK</c> record.
    /// </exception>
    /// <exception cref="BiffFormatException">Thrown when the payload is malformed.</exception>
    public readonly BiffRkRecord GetRk()
    {
        RequireRecord(BiffRecordType.Rk);

        return BiffRkRecord.Read(ValueSpan);
    }

    /// <summary>
    /// Decodes the current <c>MULRK</c> record.
    /// </summary>
    /// <returns>The decoded record.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current record is not a <c>MULRK</c> record.
    /// </exception>
    /// <exception cref="BiffFormatException">Thrown when the payload is malformed.</exception>
    public readonly BiffMulRkRecord GetMulRk()
    {
        RequireRecord(BiffRecordType.MulRk);

        return BiffMulRkRecord.Read(ValueSpan);
    }

    /// <summary>
    /// Decodes the current <c>LABEL</c> record.
    /// </summary>
    /// <returns>The decoded record.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current record is not a <c>LABEL</c> record, or the version is unknown.
    /// </exception>
    /// <exception cref="BiffFormatException">Thrown when the payload is malformed.</exception>
    public readonly BiffLabelRecord GetLabel()
    {
        RequireRecord(BiffRecordType.Label);
        RequireVersion();

        return BiffLabelRecord.Read(ValueSpan, Version, CodePage);
    }

    /// <summary>
    /// Decodes the current <c>LABELSST</c> record.
    /// </summary>
    /// <returns>The decoded record.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current record is not a <c>LABELSST</c> record.
    /// </exception>
    /// <exception cref="BiffFormatException">Thrown when the payload is malformed.</exception>
    public readonly BiffLabelSstRecord GetLabelSst()
    {
        RequireRecord(BiffRecordType.LabelSst);

        return BiffLabelSstRecord.Read(ValueSpan);
    }

    /// <summary>
    /// Decodes the current <c>BOOLERR</c> record.
    /// </summary>
    /// <returns>The decoded record.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current record is not a <c>BOOLERR</c> record.
    /// </exception>
    /// <exception cref="BiffFormatException">Thrown when the payload is malformed.</exception>
    public readonly BiffBoolErrRecord GetBoolErr()
    {
        RequireRecord(BiffRecordType.BoolErr);

        return BiffBoolErrRecord.Read(ValueSpan);
    }

    /// <summary>
    /// Decodes the current <c>BLANK</c> record.
    /// </summary>
    /// <returns>The decoded record.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current record is not a <c>BLANK</c> record.
    /// </exception>
    /// <exception cref="BiffFormatException">Thrown when the payload is malformed.</exception>
    public readonly BiffBlankRecord GetBlank()
    {
        RequireRecord(BiffRecordType.Blank);

        return BiffBlankRecord.Read(ValueSpan);
    }

    /// <summary>
    /// Decodes the current <c>MULBLANK</c> record.
    /// </summary>
    /// <returns>The decoded record.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current record is not a <c>MULBLANK</c> record.
    /// </exception>
    /// <exception cref="BiffFormatException">Thrown when the payload is malformed.</exception>
    public readonly BiffMulBlankRecord GetMulBlank()
    {
        RequireRecord(BiffRecordType.MulBlank);

        return BiffMulBlankRecord.Read(ValueSpan);
    }

    /// <summary>
    /// Decodes the current <c>FORMULA</c> record.
    /// </summary>
    /// <returns>The decoded record.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current record is not a <c>FORMULA</c> record.
    /// </exception>
    /// <exception cref="BiffFormatException">Thrown when the payload is malformed.</exception>
    public readonly BiffFormulaRecord GetFormula()
    {
        RequireRecord(BiffRecordType.Formula);

        return BiffFormulaRecord.Read(ValueSpan);
    }

    /// <summary>
    /// Decodes the current <c>STRING</c> record, the cached text result of the preceding formula.
    /// </summary>
    /// <returns>The decoded record.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current record is not a <c>STRING</c> record, or the version is unknown.
    /// </exception>
    /// <exception cref="BiffFormatException">Thrown when the payload is malformed.</exception>
    public readonly BiffStringRecord GetString()
    {
        RequireRecord(BiffRecordType.String);
        RequireVersion();

        return BiffStringRecord.Read(ValueSpan, Version, CodePage);
    }

    /// <summary>
    /// Decodes the current <c>XF</c> record.
    /// </summary>
    /// <returns>The decoded record.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current record is not an <c>XF</c> record.
    /// </exception>
    /// <exception cref="BiffFormatException">Thrown when the payload is malformed.</exception>
    public readonly BiffXfRecord GetXf()
    {
        RequireRecord(BiffRecordType.Xf);

        return BiffXfRecord.Read(ValueSpan);
    }

    /// <summary>
    /// Decodes the current <c>FORMAT</c> record.
    /// </summary>
    /// <returns>The decoded record.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current record is not a <c>FORMAT</c> record, or the version is unknown.
    /// </exception>
    /// <exception cref="BiffFormatException">Thrown when the payload is malformed.</exception>
    public readonly BiffFormatRecord GetFormat()
    {
        RequireRecord(BiffRecordType.Format);
        RequireVersion();

        return BiffFormatRecord.Read(ValueSpan, Version, CodePage);
    }

    /// <summary>
    /// Decodes the current <c>FONT</c> record.
    /// </summary>
    /// <returns>The decoded record.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current record is not a <c>FONT</c> record, or the version is unknown.
    /// </exception>
    /// <exception cref="BiffFormatException">Thrown when the payload is malformed.</exception>
    public readonly BiffFontRecord GetFont()
    {
        RequireRecord(BiffRecordType.Font);
        RequireVersion();

        return BiffFontRecord.Read(ValueSpan, Version, CodePage);
    }

    /// <summary>
    /// Decodes the current <c>CODEPAGE</c> record.
    /// </summary>
    /// <returns>The decoded record.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current record is not a <c>CODEPAGE</c> record.
    /// </exception>
    /// <exception cref="BiffFormatException">Thrown when the payload is malformed.</exception>
    public readonly BiffCodePageRecord GetCodePage()
    {
        RequireRecord(BiffRecordType.CodePage);

        return BiffCodePageRecord.Read(ValueSpan);
    }

    /// <summary>
    /// Decodes the current <c>DATEMODE</c> record.
    /// </summary>
    /// <returns>The decoded record.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current record is not a <c>DATEMODE</c> record.
    /// </exception>
    /// <exception cref="BiffFormatException">Thrown when the payload is malformed.</exception>
    public readonly BiffDateModeRecord GetDateMode()
    {
        RequireRecord(BiffRecordType.DateMode);

        return BiffDateModeRecord.Read(ValueSpan);
    }

    /// <summary>
    /// Decodes the current <c>FILEPASS</c> record.
    /// </summary>
    /// <returns>The decoded record.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current record is not a <c>FILEPASS</c> record, or the version is unknown.
    /// </exception>
    /// <exception cref="BiffFormatException">Thrown when the payload is malformed.</exception>
    public readonly BiffFilePassRecord GetFilePass()
    {
        RequireRecord(BiffRecordType.FilePass);
        RequireVersion();

        return BiffFilePassRecord.Read(ValueSpan, Version);
    }

    /// <summary>
    /// Decodes the counts at the head of the current <c>SST</c> record. The strings themselves are read through
    /// <see cref="BiffSstReader" />.
    /// </summary>
    /// <returns>The decoded header.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current record is not an <c>SST</c> record.
    /// </exception>
    /// <exception cref="BiffFormatException">Thrown when the payload is malformed.</exception>
    public readonly BiffSstHeader GetSstHeader()
    {
        RequireRecord(BiffRecordType.Sst);

        return BiffSstHeader.Read(ValueSpan);
    }
}
