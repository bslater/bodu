// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8TomlReader.DateTimes.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Reader;

public ref partial struct Utf8TomlReader
{
    /// <summary>
    /// Scans a date or date-time value beginning with a four-digit year, caching the decoded value as a local date,
    /// local date-time, or offset date-time.
    /// </summary>
    /// <returns><see langword="true" /> when the value was scanned; <see langword="false" /> when more data is required.</returns>
    private bool TryScanDateTime()
    {
        if (!TryReadDateDigits(4, out var year) || !TryExpectDateByte((byte)'-')
            || !TryReadDateDigits(2, out var month) || !TryExpectDateByte((byte)'-')
            || !TryReadDateDigits(2, out var day))
        {
            return false;
        }

        // At the end of a non-final buffer the date may still grow into a date-time.
        if (Eof && !_isFinalBlock)
            return NeedMoreData();

        var hasTime = false;
        if (!Eof && (Current == (byte)'T' || Current == (byte)'t'))
        {
            hasTime = true;
            Advance();
        }
        else if (!Eof && Current == (byte)' ')
        {
            if (IsDigit(Peek(1)) && IsDigit(Peek(2)) && Peek(3) == (byte)':')
            {
                hasTime = true;
                Advance();
            }
            else if (!_isFinalBlock && _pos + 4 > _source.Length && MatchesSpaceTimePrefix())
            {
                return NeedMoreData();
            }
        }

        if (!hasTime)
        {
            _dateOnlyValue = MakeDate(year, month, day);
            _tokenType = TomlTokenType.LocalDate;
            SetValueSpan(_tokenStart, _pos - _tokenStart);
            return true;
        }

        if (!TryReadPartialTime(out var time))
            return false;

        // At the end of a non-final buffer the local date-time may still grow an offset.
        if (Eof && !_isFinalBlock)
            return NeedMoreData();

        if (!Eof && (Current == (byte)'Z' || Current == (byte)'z' || Current == (byte)'+' || Current == (byte)'-'))
        {
            if (!TryReadTimeOffset(out var offset))
                return false;

            var local = MakeDateTime(year, month, day, time.Hour, time.Minute, time.Second, time.FractionTicks);
            try
            {
                _dateTimeOffsetValue = new DateTimeOffset(local, offset);
            }
            catch (ArgumentException)
            {
                throw Error(TomlResourceStrings.Format_Invalid_TomlInvalidDateTime);
            }

            _tokenType = TomlTokenType.OffsetDateTime;
        }
        else
        {
            _dateTimeValue = MakeDateTime(year, month, day, time.Hour, time.Minute, time.Second, time.FractionTicks);
            _tokenType = TomlTokenType.LocalDateTime;
        }

        SetValueSpan(_tokenStart, _pos - _tokenStart);
        return true;
    }

    /// <summary>
    /// Indicates whether the bytes after the space at the cursor are a truncated prefix of the
    /// <c>DD:</c> time-of-day pattern, so the date/date-time decision needs more data.
    /// </summary>
    /// <returns><see langword="true" /> when every available byte matches the pattern.</returns>
    private readonly bool MatchesSpaceTimePrefix()
    {
        for (var i = 1; i <= 3 && _pos + i < _source.Length; i++)
        {
            var b = _source[_pos + i];
            if (i < 3 ? !IsDigit(b) : b != (byte)':')
                return false;
        }

        return true;
    }

    /// <summary>
    /// Scans a bare local-time value beginning with a two-digit hour, caching the decoded value.
    /// </summary>
    /// <returns><see langword="true" /> when the value was scanned; <see langword="false" /> when more data is required.</returns>
    private bool TryScanLocalTime()
    {
        if (!TryReadPartialTime(out var time))
            return false;

        _timeOnlyValue = MakeTime(time.Hour, time.Minute, time.Second, time.FractionTicks);
        _tokenType = TomlTokenType.LocalTime;
        SetValueSpan(_tokenStart, _pos - _tokenStart);
        return true;
    }

    /// <summary>
    /// Reads the <c>HH:MM[:SS[.fraction]]</c> portion of a time value.
    /// </summary>
    /// <param name="time">The hour, minute, second, and fractional ticks.</param>
    /// <returns><see langword="true" /> when the time was read; <see langword="false" /> when more data is required.</returns>
    private bool TryReadPartialTime(out (int Hour, int Minute, int Second, long FractionTicks) time)
    {
        time = default;
        if (!TryReadDateDigits(2, out var hour) || !TryExpectDateByte((byte)':') || !TryReadDateDigits(2, out var minute))
            return false;

        // At the end of a non-final buffer the seconds component may follow in the next block.
        if (Eof && !_isFinalBlock)
            return NeedMoreData();

        var second = 0;
        long fractionTicks = 0;
        if (!Eof && Current == (byte)':')
        {
            Advance();
            if (!TryReadDateDigits(2, out second))
                return false;

            // At the end of a non-final buffer a fractional part may follow in the next block.
            if (Eof && !_isFinalBlock)
                return NeedMoreData();

            if (!Eof && Current == (byte)'.')
            {
                Advance();
                if (!TryReadFractionTicks(out fractionTicks))
                    return false;
            }
        }
        else if (_specVersion != TomlSpecVersion.V1_1)
        {
            // Optional seconds were introduced in TOML v1.1.0; v1.0 requires HH:MM:SS.
            throw Error(TomlResourceStrings.Format_Invalid_TomlSecondsRequired);
        }

        time = (hour, minute, second, fractionTicks);
        return true;
    }

    /// <summary>
    /// Reads the fractional-seconds digits and converts them to ticks, truncating beyond 100-nanosecond precision.
    /// </summary>
    /// <param name="ticks">The fractional-second value in ticks.</param>
    /// <returns><see langword="true" /> when the digits were read; <see langword="false" /> when more data is required.</returns>
    private bool TryReadFractionTicks(out long ticks)
    {
        ticks = 0;
        var start = _pos;
        while (!Eof && IsDigit(Current))
            Advance();

        // A digit run reaching the end of a non-final buffer may still grow.
        if (Eof && !_isFinalBlock)
            return NeedMoreData();

        if (_pos == start)
            throw Error(TomlResourceStrings.Format_Invalid_TomlInvalidDateTime);

        ReadOnlySpan<byte> digits = _source[start.._pos];
        for (var i = 0; i < 7; i++)
            ticks = (ticks * 10) + (i < digits.Length ? digits[i] - (byte)'0' : 0);
        return true;
    }

    /// <summary>
    /// Reads a time-zone offset (<c>Z</c> or <c>±HH:MM</c>).
    /// </summary>
    /// <param name="offset">The offset.</param>
    /// <returns><see langword="true" /> when the offset was read; <see langword="false" /> when more data is required.</returns>
    private bool TryReadTimeOffset(out TimeSpan offset)
    {
        offset = TimeSpan.Zero;
        if (Current == (byte)'Z' || Current == (byte)'z')
        {
            Advance();
            return true;
        }

        var negative = Current == (byte)'-';
        Advance();
        if (!TryReadDateDigits(2, out var hours) || !TryExpectDateByte((byte)':') || !TryReadDateDigits(2, out var minutes))
            return false;

        if (hours > 23 || minutes > 59)
            throw Error(TomlResourceStrings.Format_Invalid_TomlInvalidDateTime);

        offset = new TimeSpan(hours, minutes, 0);
        if (negative)
            offset = -offset;
        return true;
    }

    /// <summary>
    /// Reads exactly <paramref name="count" /> decimal digits as part of a date-time value.
    /// </summary>
    /// <param name="count">The digit count.</param>
    /// <param name="value">The parsed integer.</param>
    /// <returns><see langword="true" /> when the digits were read; <see langword="false" /> when more data is required.</returns>
    private bool TryReadDateDigits(int count, out int value)
    {
        value = 0;
        if (_pos + count > _source.Length)
        {
            if (!_isFinalBlock)
                return NeedMoreData();
            throw Error(TomlResourceStrings.Format_Invalid_TomlInvalidDateTime);
        }

        for (var i = 0; i < count; i++)
        {
            var b = _source[_pos + i];
            if (!IsDigit(b))
                throw Error(TomlResourceStrings.Format_Invalid_TomlInvalidDateTime);
            value = (value * 10) + (b - (byte)'0');
        }

        _pos += count;
        return true;
    }

    /// <summary>
    /// Consumes the expected separator byte within a date-time value.
    /// </summary>
    /// <param name="expected">The expected byte.</param>
    /// <returns><see langword="true" /> when the byte was consumed; <see langword="false" /> when more data is required.</returns>
    private bool TryExpectDateByte(byte expected)
    {
        if (Eof)
        {
            if (!_isFinalBlock)
                return NeedMoreData();
            throw Error(TomlResourceStrings.Format_Invalid_TomlInvalidDateTime);
        }

        if (Current != expected)
            throw Error(TomlResourceStrings.Format_Invalid_TomlInvalidDateTime);
        Advance();
        return true;
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
