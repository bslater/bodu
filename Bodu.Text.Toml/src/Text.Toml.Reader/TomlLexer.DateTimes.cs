// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlLexer.DateTimes.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Reader;

internal ref partial struct TomlLexer
{
    /// <summary>
    /// Scans a date or date-time value beginning with a four-digit year, caching the decoded value as a local date,
    /// local date-time, or offset date-time.
    /// </summary>
    private void ScanDateTime()
    {
        var year = ReadDateDigits(4);
        ExpectDateByte((byte)'-');
        var month = ReadDateDigits(2);
        ExpectDateByte((byte)'-');
        var day = ReadDateDigits(2);

        var hasTime = false;
        if (!Eof && (Current == (byte)'T' || Current == (byte)'t'))
        {
            hasTime = true;
            Advance();
        }
        else if (!Eof && Current == (byte)' ' && IsDigit(Peek(1)) && IsDigit(Peek(2)) && Peek(3) == (byte)':')
        {
            hasTime = true;
            Advance();
        }

        if (!hasTime)
        {
            _dateOnlyValue = MakeDate(year, month, day);
            _tokenType = TomlLexTokenType.LocalDate;
            SetValueSpan(_tokenStart, _pos - _tokenStart);
            return;
        }

        var (hour, minute, second, fractionTicks) = ReadPartialTime();

        if (!Eof && (Current == (byte)'Z' || Current == (byte)'z' || Current == (byte)'+' || Current == (byte)'-'))
        {
            var offset = ReadTimeOffset();
            var local = MakeDateTime(year, month, day, hour, minute, second, fractionTicks);
            try
            {
                _dateTimeOffsetValue = new DateTimeOffset(local, offset);
            }
            catch (ArgumentException)
            {
                throw Error(TomlResourceStrings.Format_Invalid_TomlInvalidDateTime);
            }

            _tokenType = TomlLexTokenType.OffsetDateTime;
        }
        else
        {
            _dateTimeValue = MakeDateTime(year, month, day, hour, minute, second, fractionTicks);
            _tokenType = TomlLexTokenType.LocalDateTime;
        }

        SetValueSpan(_tokenStart, _pos - _tokenStart);
    }

    /// <summary>
    /// Scans a bare local-time value beginning with a two-digit hour, caching the decoded value.
    /// </summary>
    private void ScanLocalTime()
    {
        var (hour, minute, second, fractionTicks) = ReadPartialTime();
        _timeOnlyValue = MakeTime(hour, minute, second, fractionTicks);
        _tokenType = TomlLexTokenType.LocalTime;
        SetValueSpan(_tokenStart, _pos - _tokenStart);
    }

    /// <summary>
    /// Reads the <c>HH:MM[:SS[.fraction]]</c> portion of a time value.
    /// </summary>
    /// <returns>The hour, minute, second, and fractional ticks.</returns>
    private (int Hour, int Minute, int Second, long FractionTicks) ReadPartialTime()
    {
        var hour = ReadDateDigits(2);
        ExpectDateByte((byte)':');
        var minute = ReadDateDigits(2);

        var second = 0;
        long fractionTicks = 0;
        if (!Eof && Current == (byte)':')
        {
            Advance();
            second = ReadDateDigits(2);
            if (!Eof && Current == (byte)'.')
            {
                Advance();
                fractionTicks = ReadFractionTicks();
            }
        }
        else if (_specVersion != TomlSpecVersion.V1_1)
        {
            // Optional seconds were introduced in TOML v1.1.0; v1.0 requires HH:MM:SS.
            throw Error(TomlResourceStrings.Format_Invalid_TomlSecondsRequired);
        }

        return (hour, minute, second, fractionTicks);
    }

    /// <summary>
    /// Reads the fractional-seconds digits and converts them to ticks, truncating beyond 100-nanosecond precision.
    /// </summary>
    /// <returns>The fractional-second value in ticks.</returns>
    private long ReadFractionTicks()
    {
        var start = _pos;
        while (!Eof && IsDigit(Current))
            Advance();
        if (_pos == start)
            throw Error(TomlResourceStrings.Format_Invalid_TomlInvalidDateTime);

        ReadOnlySpan<byte> digits = _source[start.._pos];
        long ticks = 0;
        for (var i = 0; i < 7; i++)
            ticks = (ticks * 10) + (i < digits.Length ? digits[i] - (byte)'0' : 0);
        return ticks;
    }

    /// <summary>
    /// Reads a time-zone offset (<c>Z</c> or <c>±HH:MM</c>).
    /// </summary>
    /// <returns>The offset.</returns>
    private TimeSpan ReadTimeOffset()
    {
        if (Current == (byte)'Z' || Current == (byte)'z')
        {
            Advance();
            return TimeSpan.Zero;
        }

        var negative = Current == (byte)'-';
        Advance();
        var hours = ReadDateDigits(2);
        ExpectDateByte((byte)':');
        var minutes = ReadDateDigits(2);
        if (hours > 23 || minutes > 59)
            throw Error(TomlResourceStrings.Format_Invalid_TomlInvalidDateTime);

        var offset = new TimeSpan(hours, minutes, 0);
        return negative ? -offset : offset;
    }

    /// <summary>
    /// Reads exactly <paramref name="count" /> decimal digits as part of a date-time value.
    /// </summary>
    /// <param name="count">The digit count.</param>
    /// <returns>The parsed integer.</returns>
    private int ReadDateDigits(int count)
    {
        if (_pos + count > _source.Length)
            throw Error(TomlResourceStrings.Format_Invalid_TomlInvalidDateTime);

        var value = 0;
        for (var i = 0; i < count; i++)
        {
            var b = _source[_pos + i];
            if (!IsDigit(b))
                throw Error(TomlResourceStrings.Format_Invalid_TomlInvalidDateTime);
            value = (value * 10) + (b - (byte)'0');
        }

        _pos += count;
        return value;
    }

    /// <summary>
    /// Consumes the expected separator byte within a date-time value.
    /// </summary>
    /// <param name="expected">The expected byte.</param>
    private void ExpectDateByte(byte expected)
    {
        if (Eof || Current != expected)
            throw Error(TomlResourceStrings.Format_Invalid_TomlInvalidDateTime);
        Advance();
    }

    /// <summary>
    /// Builds a <see cref="DateOnly" /> from validated components.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month.</param>
    /// <param name="day">The day.</param>
    /// <returns>The date.</returns>
    private readonly DateOnly MakeDate(int year, int month, int day)
    {
        try
        {
            return new DateOnly(year, month, day);
        }
        catch (ArgumentOutOfRangeException)
        {
            throw Error(TomlResourceStrings.Format_Invalid_TomlInvalidDateTime);
        }
    }

    /// <summary>
    /// Builds a <see cref="TimeOnly" /> from validated components.
    /// </summary>
    /// <param name="hour">The hour.</param>
    /// <param name="minute">The minute.</param>
    /// <param name="second">The second.</param>
    /// <param name="fractionTicks">The fractional-second ticks.</param>
    /// <returns>The time.</returns>
    private readonly TimeOnly MakeTime(int hour, int minute, int second, long fractionTicks)
    {
        // Leap seconds (:60) cannot be represented by TimeOnly/DateTime; reject rather than silently clamp to :59.
        if (second == 60)
            throw Error(TomlResourceStrings.Format_Invalid_TomlLeapSecond);
        if (hour > 23 || minute > 59 || second > 59)
            throw Error(TomlResourceStrings.Format_Invalid_TomlInvalidDateTime);

        var ticks = (((hour * 3600L) + (minute * 60L) + second) * TicksPerSecond) + fractionTicks;
        return new TimeOnly(ticks);
    }

    /// <summary>
    /// Builds a <see cref="DateTime" /> with <see cref="DateTimeKind.Unspecified" /> from validated components.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month.</param>
    /// <param name="day">The day.</param>
    /// <param name="hour">The hour.</param>
    /// <param name="minute">The minute.</param>
    /// <param name="second">The second.</param>
    /// <param name="fractionTicks">The fractional-second ticks.</param>
    /// <returns>The date-time.</returns>
    private readonly DateTime MakeDateTime(int year, int month, int day, int hour, int minute, int second, long fractionTicks)
    {
        // Leap seconds (:60) cannot be represented by TimeOnly/DateTime; reject rather than silently clamp to :59.
        if (second == 60)
            throw Error(TomlResourceStrings.Format_Invalid_TomlLeapSecond);
        if (hour > 23 || minute > 59 || second > 59)
            throw Error(TomlResourceStrings.Format_Invalid_TomlInvalidDateTime);

        try
        {
            return new DateTime(year, month, day, hour, minute, second, DateTimeKind.Unspecified).AddTicks(fractionTicks);
        }
        catch (ArgumentOutOfRangeException)
        {
            throw Error(TomlResourceStrings.Format_Invalid_TomlInvalidDateTime);
        }
    }
}
